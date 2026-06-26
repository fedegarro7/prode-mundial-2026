using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prode.Api.BackgroundServices;
using Prode.Api.Data;
using Prode.Api.Services;
using System.Text;
using Prode.Api.Middlewares;
using FluentValidation;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddEndpointsApiExplorer();

var connectionString = GetRequiredConfigurationValue(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    "ConnectionStrings:DefaultConnection"
);

var jwtSettings = builder.Configuration.GetSection("Jwt");

var jwtKey = GetRequiredConfigurationValue(jwtSettings["Key"], "Jwt:Key");
var jwtIssuer = GetRequiredConfigurationValue(jwtSettings["Issuer"], "Jwt:Issuer");
var jwtAudience = GetRequiredConfigurationValue(jwtSettings["Audience"], "Jwt:Audience");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
  throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");
}

builder.Services.AddSwaggerGen(options =>
{
  options.SwaggerDoc(
      "v1",
      new OpenApiInfo
      {
        Title = "Prode API",
        Version = "v1"
      }
  );

  options.AddSecurityDefinition(
      "Bearer",
      new OpenApiSecurityScheme
      {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description =
              "Ingresá el token JWT"
      }
  );

  options.AddSecurityRequirement(
      new OpenApiSecurityRequirement
      {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
      }
  );
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
  // Append pool settings: release idle connections quickly so Neon can scale to zero.
  // Connection Idle Lifetime=30  → prune connections idle for 30 s
  // Connection Pruning Interval=10 → pruning check every 10 s
  // Keepalive=0                  → no TCP keepalive pings to Neon
  var pooledConnectionString = connectionString.TrimEnd(';')
      + ";Connection Idle Lifetime=30;Connection Pruning Interval=10;Keepalive=0";
  options.UseNpgsql(pooledConnectionString);
});

var allowedOrigins =
  builder.Configuration
      .GetSection("Cors:AllowedOrigins")
      .Get<string[]>()
  ?? new[]
  {
    "http://localhost:4200",
    "http://127.0.0.1:4200"
  };

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAngular", policy =>
  {
    policy
          .WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
  });
});

builder.Services.AddScoped<JwtService>();
builder.Services.Configure<SmtpEmailOptions>(
    builder.Configuration.GetSection("Email:Smtp")
);
builder.Services.Configure<PasswordResetOptions>(
    builder.Configuration.GetSection("PasswordReset")
);
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<BombMatchService>();
builder.Services.AddScoped<ScoreRecalculationService>();
builder.Services.AddScoped<MechanicsService>();
builder.Services.AddScoped<PredictionService>();
builder.Services.AddHttpClient<FifaFixtureSyncService>(client =>
{
  client.DefaultRequestHeaders.UserAgent.ParseAdd(
      "Mozilla/5.0 ProdeMundial2026/1.0"
  );

  client.DefaultRequestHeaders.Accept.ParseAdd(
      "application/json"
  );
});

if (
  builder.Configuration.GetValue<bool>(
      "FixtureSync:AutoSyncOnStartup",
      builder.Environment.IsDevelopment()
  )
)
{
  builder.Services.AddHostedService<FixtureStartupSyncService>();
}

builder.Services.AddHostedService<FifaScoreSyncBackgroundService>();

builder.Services.AddRateLimiter(options =>
{
  options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

  options.AddPolicy("AuthSensitive", context =>
      RateLimitPartition.GetFixedWindowLimiter(
          GetRemoteIpPartitionKey(context, "auth"),
          _ => new FixedWindowRateLimiterOptions
          {
            AutoReplenishment = true,
            PermitLimit = 5,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1)
          }
      )
  );

  options.AddPolicy("PasswordRecovery", context =>
      RateLimitPartition.GetFixedWindowLimiter(
          GetRemoteIpPartitionKey(context, "password-recovery"),
          _ => new FixedWindowRateLimiterOptions
          {
            AutoReplenishment = true,
            PermitLimit = 3,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(15)
          }
      )
  );

  options.AddPolicy("Predictions", context =>
      RateLimitPartition.GetFixedWindowLimiter(
          GetUserOrRemoteIpPartitionKey(context, "predictions"),
          _ => new FixedWindowRateLimiterOptions
          {
            AutoReplenishment = true,
            PermitLimit = 30,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1)
          }
      )
  );

  options.AddPolicy("Groups", context =>
      RateLimitPartition.GetFixedWindowLimiter(
          GetUserOrRemoteIpPartitionKey(context, "groups"),
          _ => new FixedWindowRateLimiterOptions
          {
            AutoReplenishment = true,
            PermitLimit = 40,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1)
          }
      )
  );
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

      options.SaveToken = false;

      options.TokenValidationParameters =
          new TokenValidationParameters
          {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey =
                  new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateIssuer = true,

            ValidIssuer = jwtIssuer,

            ValidateAudience = true,

            ValidAudience = jwtAudience,

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
          };

      options.Events = new JwtBearerEvents
      {
        OnMessageReceived = context =>
        {
          var cookieToken =
              context.Request.Cookies[AuthCookieDefaults.CookieName];

          if (!string.IsNullOrWhiteSpace(cookieToken))
          {
            context.Token = cookieToken;
          }

          return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
          var logger = context.HttpContext.RequestServices
              .GetRequiredService<ILogger<Program>>();

          logger.LogWarning(
              context.Exception,
              "JWT authentication failed."
          );

          return Task.CompletedTask;
        }
      };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
  using var scope = app.Services.CreateScope();
  var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  await context.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();

  app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAngular");

app.Use(async (context, next) =>
{
  context.Response.Headers["X-Content-Type-Options"] = "nosniff";
  context.Response.Headers["X-Frame-Options"] = "DENY";
  context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
  context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
  await next();
});

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

if (
  args.Contains(
      "--sync-fifa-fixture",
      StringComparer.OrdinalIgnoreCase
  )
)
{
  using var scope = app.Services.CreateScope();

  var context =
      scope.ServiceProvider.GetRequiredService<AppDbContext>();

  await context.Database.MigrateAsync();

  var syncService =
      scope.ServiceProvider
          .GetRequiredService<FifaFixtureSyncService>();

  var result =
      await syncService.SyncWorldCup2026Async();

  Console.WriteLine(
      $"FIFA fixture synced: {result.Teams} teams, " +
      $"{result.Stadiums} stadiums, {result.Matches} matches"
  );

  return;
}

app.Run();

static string GetRequiredConfigurationValue(string? value, string key)
{
  if (string.IsNullOrWhiteSpace(value))
  {
    throw new InvalidOperationException(
        $"Missing required configuration value: {key}."
    );
  }

  return value;
}

static string GetRemoteIpPartitionKey(HttpContext context, string prefix)
{
  var ipAddress =
      context.Connection.RemoteIpAddress?.ToString()
      ?? "unknown";

  return $"{prefix}:ip:{ipAddress}";
}

static string GetUserOrRemoteIpPartitionKey(HttpContext context, string prefix)
{
  var userId =
      context.User.FindFirstValue(ClaimTypes.NameIdentifier);

  return string.IsNullOrWhiteSpace(userId)
      ? GetRemoteIpPartitionKey(context, prefix)
      : $"{prefix}:user:{userId}";
}
