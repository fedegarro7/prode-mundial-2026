using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prode.Api.BackgroundServices;
using Prode.Api.Data;
using Prode.Api.Services;
using System.Text;
using Prode.Api.Middlewares;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddEndpointsApiExplorer();

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
  options.UseNpgsql(
      builder.Configuration.GetConnectionString("DefaultConnection")
  );
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
          .AllowAnyMethod();
  });
});

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ScoringService>();
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

var jwtSettings = builder.Configuration.GetSection("Jwt");

var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      options.RequireHttpsMetadata = false;

      options.SaveToken = true;

      options.TokenValidationParameters =
          new TokenValidationParameters
          {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey =
                  new SymmetricSecurityKey(key),

            ValidateIssuer = false,

            ValidateAudience = false,

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
          };

      options.Events = new JwtBearerEvents
      {
        OnAuthenticationFailed = context =>
        {
          Console.WriteLine("JWT ERROR:");
          Console.WriteLine(context.Exception);

          return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
          Console.WriteLine("TOKEN VALIDADO");

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

app.UseSwagger();

app.UseSwaggerUI();

app.UseCors("AllowAngular");

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();

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
