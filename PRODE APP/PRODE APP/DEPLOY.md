# Deploy del MVP

## 1. API en Render

Usa el `render.yaml` del repo como blueprint o crea un Web Service manual.

Configuracion clave:

- Runtime: Docker
- Root directory: `Prode.Api`
- Health check path: `/api/health`
- Plan: Free

Variables necesarias:

- `ConnectionStrings__DefaultConnection`: connection string de PostgreSQL.
- `Jwt__Key`: una clave larga generada al azar.
- `ASPNETCORE_ENVIRONMENT`: `Production`.
- `Database__AutoMigrate`: `true` para aplicar migraciones al iniciar.
- `Cors__AllowedOrigins__0`: URL final de Vercel, por ejemplo `https://prode-mundial.vercel.app`.

Cuando Render termine, proba:

```text
https://TU-API.onrender.com/api/health
```

## 2. Web en Vercel

En Vercel crea el proyecto apuntando a `prode-web`.

Configuracion:

- Framework: Angular
- Build command: `npm run build`
- Output directory: `dist/prode-web/browser`

Antes de publicar, cambia en `prode-web/src/environments/environment.prod.ts`:

```ts
apiUrl: 'https://TU-API.onrender.com/api'
```

## 3. CORS final

Despues de tener la URL de Vercel, volve a Render y deja:

```text
Cors__AllowedOrigins__0=https://TU-WEB.vercel.app
```

Despues redeploya la API.

## 4. Primer chequeo

1. Abrir la web de Vercel.
2. Registrarse.
3. Entrar a `Partidos`.
4. Guardar un pronostico.
5. Crear un grupo.
6. Copiar el link directo de invitacion.
7. Verificar `/api/health` en Render.
