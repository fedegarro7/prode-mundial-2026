# Prode Mundial 2026

## 🇪🇸 Español

### Descripción del proyecto
Prode Mundial 2026 es una aplicación web para pronosticar partidos del Mundial FIFA 2026, crear y administrar grupos privados, comparar puntajes y seguir rankings, noticias y mecánicas especiales del juego. El repositorio contiene una solución con un backend ASP.NET Core y un frontend Angular.

### Características principales
- Autenticación con JWT almacenado en cookie.
- Registro, inicio de sesión, recuperación y cambio de contraseña.
- Pronósticos de partidos y consulta de pronósticos propios.
- Grupos privados con creación, invitación por código y ranking interno.
- Tablas de posiciones, partidos, selecciones, estadios y rankings.
- Sección de noticias y centro de reglas.
- Panel de administración y soporte para actualización de resultados.
- Mecánicas especiales en fases eliminatorias, incluyendo Capitán, Partido Bomba, Gol de Oro, Francotirador y Oráculo.
- Health check expuesto por la API.
- Rate limiting para autenticación, recuperación de contraseña, pronósticos y grupos.
- Sincronización de fixture y puntajes mediante servicios en segundo plano.

### Tecnologías utilizadas
- Backend: ASP.NET Core `net10.0`.
- Entity Framework Core 10.
- PostgreSQL con `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Autenticación JWT con `Microsoft.AspNetCore.Authentication.JwtBearer`.
- Validación con FluentValidation.
- Hash de contraseñas con BCrypt.Net-Next.
- Documentación OpenAPI con Swashbuckle.
- Pruebas unitarias con xUnit, Moq y EF Core InMemory.
- Frontend: Angular 21.
- Angular SSR.
- Angular Material y CDK.
- TypeScript 5.9.
- RxJS, SCSS, `ngx-toastr`, `jwt-decode` y `@microsoft/signalr` declarados en el frontend.

### Arquitectura
- La solución principal está en `PRODE APP/PRODE APP/prode-mundial-2026.sln`.
- El backend vive en `PRODE APP/PRODE APP/Prode.Api` y expone una API REST con controladores para autenticación, partidos, grupos, rankings, standings, noticias, mecánicas, dashboard, estadios, selecciones, health check y sincronización de fixture.
- El backend usa `AppDbContext`, migraciones de EF Core, middlewares propios, validadores y servicios de dominio.
- El frontend vive en `PRODE APP/PRODE APP/prode-web` y está organizado como aplicación standalone con rutas, guard de autenticación, interceptor HTTP y páginas por dominio.
- El frontend consume la API vía `environment.ts` y `proxy.conf.json` en desarrollo.
- El despliegue está pensado con backend en Render y frontend en Vercel.

### Requisitos
- .NET 10 SDK.
- Node.js compatible con Angular CLI 21.2.10.
- npm `10.9.2` declarado en el proyecto frontend.
- Una base PostgreSQL accesible para la API.
- SMTP configurado si se usa recuperación de contraseña por correo.

### Instalación
1. Clonar el repositorio.
2. Ir a `PRODE APP/PRODE APP`.
3. Instalar dependencias del frontend:

```bash
cd "PRODE APP/PRODE APP/prode-web"
npm install
```

4. Restaurar paquetes y compilar la solución backend:

```bash
dotnet restore "PRODE APP/PRODE APP/prode-mundial-2026.sln"
dotnet build "PRODE APP/PRODE APP/prode-mundial-2026.sln"
```

### Configuración
- La API requiere `ConnectionStrings:DefaultConnection`, `Jwt:Key`, `Jwt:Issuer` y `Jwt:Audience`.
- `Jwt:ExpireMinutes` está configurado en `appsettings.json`.
- `PasswordReset:ExpireMinutes`, `PasswordReset:RequestCooldownMinutes` y `PasswordReset:ResetUrl` están definidos en configuración.
- `Email:Smtp:*` contiene host, puerto, usuario, contraseña, remitente, nombre y SSL.
- `Cors:AllowedOrigins` incluye localhost y la URL de Vercel configurada en el repositorio.
- `FixtureSync:ScoreSyncIntervalMinutes` controla la frecuencia de sincronización de puntajes.
- `Database:AutoMigrate` habilita la migración automática al iniciar.
- `FixtureSync:AutoSyncOnStartup` existe en configuración de desarrollo.
- En desarrollo, el frontend apunta a `http://localhost:5000/api`.
- En producción, el frontend apunta a `https://prode-mundial-2026-duyj.onrender.com/api`.
- La cookie de autenticación usa el nombre `prode_auth`.

### Ejecución en desarrollo
- Backend:

```bash
cd "PRODE APP/PRODE APP/Prode.Api"
dotnet run
```

- Frontend:

```bash
cd "PRODE APP/PRODE APP/prode-web"
npm start
```

- La configuración de desarrollo del frontend usa proxy para `/api` hacia `http://localhost:5000`.
- La API expone Swagger únicamente en desarrollo.

### Build y despliegue
- Frontend:

```bash
cd "PRODE APP/PRODE APP/prode-web"
npm run build
```

- El build del frontend genera `dist/prode-web/browser`.
- El despliegue del frontend está configurado para Vercel mediante `vercel.json`.
- El backend está preparado para Render mediante `render.yaml`.
- El backend usa Dockerfile con imagen base `mcr.microsoft.com/dotnet/sdk:10.0` para build y `mcr.microsoft.com/dotnet/aspnet:10.0` para runtime.
- El contenedor del backend escucha en el puerto `8080`.
- El health check configurado para Render es `/api/health`.
- La configuración de Render habilita `Database__AutoMigrate=true`.
- El origen de CORS configurado para producción es la URL de Vercel definida en `render.yaml`.

### Estructura del proyecto
- `PRODE APP/DB`: archivo de datos en la raíz del repositorio.
- `PRODE APP/PRODE APP/prode-mundial-2026.sln`: solución de Visual Studio.
- `PRODE APP/PRODE APP/Prode.Api`: backend ASP.NET Core.
- `PRODE APP/PRODE APP/Prode.Api/Controllers`: controladores de la API.
- `PRODE APP/PRODE APP/Prode.Api/Services`: lógica de negocio, sincronización, scoring, JWT y correo.
- `PRODE APP/PRODE APP/Prode.Api/Entities`: entidades de dominio y EF Core.
- `PRODE APP/PRODE APP/Prode.Api/DTOs`: contratos de entrada y salida.
- `PRODE APP/PRODE APP/Prode.Api/Validators`: validadores FluentValidation.
- `PRODE APP/PRODE APP/Prode.Api/BackgroundServices`: servicios en segundo plano.
- `PRODE APP/PRODE APP/Prode.Api/Migrations`: migraciones de base de datos.
- `PRODE APP/PRODE APP/Prode.Api.Tests`: proyecto de pruebas.
- `PRODE APP/PRODE APP/prode-web`: frontend Angular.
- `PRODE APP/PRODE APP/prode-web/src/app/pages`: páginas de la aplicación.
- `PRODE APP/PRODE APP/prode-web/src/app/services`: servicios del frontend.
- `PRODE APP/PRODE APP/prode-web/src/app/guards`: guardas de navegación.
- `PRODE APP/PRODE APP/prode-web/src/app/interceptors`: interceptor HTTP.
- `PRODE APP/PRODE APP/render.yaml`: despliegue de Render.
- `PRODE APP/PRODE APP/prode-web/vercel.json`: despliegue y headers de Vercel.
- `PRODE APP/PRODE APP/DEPLOY.md`: guía de despliegue.
- `PRODE APP/PRODE APP/MECHANICS_CHANGELOG.md`: changelog de mecánicas.

### Cómo contribuir
No hay un `CONTRIBUTING.md` ni reglas de contribución adicionales en el repositorio. El proyecto sí incluye una solución formal y pruebas unitarias en `Prode.Api.Tests`, por lo que cualquier aporte debería mantener esa estructura y validar el cambio con el build y los tests correspondientes.

### Licencia
No se encontró un archivo de licencia en el repositorio.

## 🇺🇸 English

### Project Description
Prode Mundial 2026 is a web application for predicting FIFA World Cup 2026 matches, creating and managing private groups, comparing scores, and following rankings, news, and special gameplay mechanics. The repository contains an ASP.NET Core backend and an Angular frontend.

### Main Features
- JWT-based authentication stored in a cookie.
- Registration, login, password recovery, and password change.
- Match predictions and access to the user’s own predictions.
- Private groups with creation, invite-code join flow, and internal ranking.
- Standings, matches, teams, stadiums, and rankings views.
- News section and rules center.
- Admin panel and match-result update support.
- Special knockout-stage mechanics, including Captain, Bomb Match, Golden Goal, Sharpshooter, and Oracle.
- API health check endpoint.
- Rate limiting for authentication, password recovery, predictions, and groups.
- Fixture and score synchronization through background services.

### Technologies
- Backend: ASP.NET Core `net10.0`.
- Entity Framework Core 10.
- PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`.
- JWT auth via `Microsoft.AspNetCore.Authentication.JwtBearer`.
- Validation with FluentValidation.
- Password hashing with BCrypt.Net-Next.
- OpenAPI documentation with Swashbuckle.
- Unit tests with xUnit, Moq, and EF Core InMemory.
- Frontend: Angular 21.
- Angular SSR.
- Angular Material and CDK.
- TypeScript 5.9.
- RxJS, SCSS, `ngx-toastr`, `jwt-decode`, and `@microsoft/signalr` are declared in the frontend project.

### Architecture
- The main solution is located at `PRODE APP/PRODE APP/prode-mundial-2026.sln`.
- The backend lives in `PRODE APP/PRODE APP/Prode.Api` and exposes a REST API with controllers for auth, matches, groups, rankings, standings, news, mechanics, dashboard, stadiums, teams, health, and fixture sync.
- The backend uses `AppDbContext`, EF Core migrations, custom middlewares, validators, and domain services.
- The frontend lives in `PRODE APP/PRODE APP/prode-web` and is organized as a standalone Angular app with routes, an auth guard, an HTTP interceptor, and page-level components.
- The frontend consumes the API through `environment.ts` and the local `proxy.conf.json`.
- Deployment is set up for Render on the backend and Vercel on the frontend.

### Requirements
- .NET 10 SDK.
- Node.js compatible with Angular CLI 21.2.10.
- npm `10.9.2` as declared by the frontend project.
- A PostgreSQL database reachable by the API.
- SMTP settings if email-based password recovery is used.

### Installation
1. Clone the repository.
2. Change into `PRODE APP/PRODE APP`.
3. Install frontend dependencies:

```bash
cd "PRODE APP/PRODE APP/prode-web"
npm install
```

4. Restore and build the backend solution:

```bash
dotnet restore "PRODE APP/PRODE APP/prode-mundial-2026.sln"
dotnet build "PRODE APP/PRODE APP/prode-mundial-2026.sln"
```

### Configuration
- The API requires `ConnectionStrings:DefaultConnection`, `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience`.
- `Jwt:ExpireMinutes` is defined in `appsettings.json`.
- `PasswordReset:ExpireMinutes`, `PasswordReset:RequestCooldownMinutes`, and `PasswordReset:ResetUrl` are configured.
- `Email:Smtp:*` contains host, port, username, password, from email, from name, and SSL settings.
- `Cors:AllowedOrigins` includes localhost and the Vercel URL configured in the repo.
- `FixtureSync:ScoreSyncIntervalMinutes` controls score-sync frequency.
- `Database:AutoMigrate` enables automatic migrations on startup.
- `FixtureSync:AutoSyncOnStartup` exists in development configuration.
- In development, the frontend points to `http://localhost:5000/api`.
- In production, the frontend points to `https://prode-mundial-2026-duyj.onrender.com/api`.
- The authentication cookie is named `prode_auth`.

### Development
- Backend:

```bash
cd "PRODE APP/PRODE APP/Prode.Api"
dotnet run
```

- Frontend:

```bash
cd "PRODE APP/PRODE APP/prode-web"
npm start
```

- The frontend development setup proxies `/api` requests to `http://localhost:5000`.
- Swagger is enabled only in development on the API.

### Build and Deployment
- Frontend build:

```bash
cd "PRODE APP/PRODE APP/prode-web"
npm run build
```

- The frontend build output is `dist/prode-web/browser`.
- Frontend deployment is configured for Vercel through `vercel.json`.
- Backend deployment is configured for Render through `render.yaml`.
- The backend Dockerfile uses `mcr.microsoft.com/dotnet/sdk:10.0` for build and `mcr.microsoft.com/dotnet/aspnet:10.0` for runtime.
- The backend container listens on port `8080`.
- The Render health check path is `/api/health`.
- Render is configured with `Database__AutoMigrate=true`.
- The production CORS origin matches the Vercel URL defined in `render.yaml`.

### Project Structure
- `PRODE APP/DB`: data file in the repository root.
- `PRODE APP/PRODE APP/prode-mundial-2026.sln`: Visual Studio solution.
- `PRODE APP/PRODE APP/Prode.Api`: ASP.NET Core backend.
- `PRODE APP/PRODE APP/Prode.Api/Controllers`: API controllers.
- `PRODE APP/PRODE APP/Prode.Api/Services`: business logic, sync, scoring, JWT, and email services.
- `PRODE APP/PRODE APP/Prode.Api/Entities`: domain and EF Core entities.
- `PRODE APP/PRODE APP/Prode.Api/DTOs`: input and output contracts.
- `PRODE APP/PRODE APP/Prode.Api/Validators`: FluentValidation validators.
- `PRODE APP/PRODE APP/Prode.Api/BackgroundServices`: background workers.
- `PRODE APP/PRODE APP/Prode.Api/Migrations`: database migrations.
- `PRODE APP/PRODE APP/Prode.Api.Tests`: test project.
- `PRODE APP/PRODE APP/prode-web`: Angular frontend.
- `PRODE APP/PRODE APP/prode-web/src/app/pages`: application pages.
- `PRODE APP/PRODE APP/prode-web/src/app/services`: frontend services.
- `PRODE APP/PRODE APP/prode-web/src/app/guards`: navigation guards.
- `PRODE APP/PRODE APP/prode-web/src/app/interceptors`: HTTP interceptor.
- `PRODE APP/PRODE APP/render.yaml`: Render deployment spec.
- `PRODE APP/PRODE APP/prode-web/vercel.json`: Vercel deployment and headers.
- `PRODE APP/PRODE APP/DEPLOY.md`: deployment guide.
- `PRODE APP/PRODE APP/MECHANICS_CHANGELOG.md`: mechanics changelog.

### Contributing
There is no `CONTRIBUTING.md` or additional contribution policy in the repository. The project does include a formal solution and unit tests in `Prode.Api.Tests`, so contributions should preserve that structure and be validated with the relevant build and test commands.

### License
No license file was found in the repository.
