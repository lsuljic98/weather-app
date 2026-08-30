# Weather App

A full-stack weather app: sign in, see the weather where you are, search any city for a
5-day forecast (grid + chart with filters), and look back over your search history and
statistics. Weather data comes from [OpenWeather](https://openweathermap.org/api).

**Stack:** .NET 10 Web API · PostgreSQL 17 · React 19 (Vite, TypeScript, Tailwind) · Docker Compose

---

## Run it locally

You need Docker and an OpenWeather API key (free tier is enough; a new key takes up to
~2 hours to activate).

```bash
cp .env.example .env          # then fill in OPENWEATHER_API_KEY, POSTGRES_PASSWORD, JWT_KEY
docker compose up --build
```

Then open **http://localhost:5173** and register an account.

| What | URL |
| --- | --- |
| App | http://localhost:5173 |
| API + Swagger UI | http://localhost:8080/swagger |
| pgAdmin (dev only, no login) | http://localhost:5050 |
| Postgres (dev only) | `localhost:5432` |

`docker-compose.override.yml` is picked up automatically and gives you hot reload for both
the API (`dotnet watch`) and the frontend (Vite), plus pgAdmin. To run the production images
instead: `docker compose -f docker-compose.yml up --build`.

### Environment variables (`.env`)

| Variable | Required | Notes |
| --- | --- | --- |
| `OPENWEATHER_API_KEY` | yes | From your OpenWeather account |
| `POSTGRES_PASSWORD` | yes | Anything; the DB is local |
| `JWT_KEY` | yes | ≥ 32 bytes for HS256 — `openssl rand -base64 48` |
| `JWT_ACCESS_MINUTES` / `JWT_REFRESH_DAYS` | no | Defaults 15 / 7 |
| `WEB_PORT` / `API_PORT` / `DB_PORT` / `PGADMIN_PORT` | no | Host ports, defaults above |

Database migrations run automatically when the API starts (`ApplyMigrationsOnStartup=true`
in compose).

---

## Architecture

```
 browser ──▶ web (nginx / Vite) ──/api──▶ api (ASP.NET) ──▶ db (Postgres)
                     :5173                    :8080              :5432
                                                 │
                                                 └──▶ api.openweathermap.org
```

The browser talks to **one origin only**. nginx (prod) or the Vite dev server (dev) proxies
`/api/*` to the backend, so there is no CORS and the auth cookie is first-party. See
[DOCKER.md](DOCKER.md) for the container setup in depth.

### Backend — `weather-be/`

Clean-architecture layout, one project per layer:

| Project | Holds |
| --- | --- |
| `WeatherApp.Domain` | Entities: `User`, `RefreshToken`, `Search` |
| `WeatherApp.Application` | Use cases (`AuthService`, `SearchService`, `StatisticsService`), DTOs, repository/service interfaces |
| `WeatherApp.Infrastructure` | EF Core + Npgsql, migrations, repositories, the OpenWeather HTTP client with in-memory caching (10 min weather, 12 h geocoding) |
| `WeatherApp.API` | MVC controllers, JWT auth, ProblemDetails error handling, OpenAPI |

**Auth.** Passwords are hashed with ASP.NET's `PasswordHasher` (PBKDF2). Login returns a
15-minute JWT in the body and a 7-day refresh token in an `HttpOnly` cookie scoped to
`/api/auth`. Refresh tokens rotate on every use and are stored hashed; presenting an
already-used token revokes the whole chain.

**Searches.** Fetching a forecast *is* a search: `GET /api/weather/forecast` records a row
with a snapshot of the conditions at that moment. History and statistics are then plain
per-user queries on that table — always read from the database, never a cache — and every
read path is covered by a `(user_id, …)` composite index.

### Frontend — `weather-fe/src/`

```
api/          fetch client (bearer injection, single-flight refresh + retry) and typed endpoint calls
auth/         AuthProvider (token in memory, session restored from the cookie), route guard
hooks/        TanStack Query hooks, geolocation, debounce
features/     auth · current-widget · forecast · history · statistics
components/   Button, Field, WeatherIcon, ChartTooltip, …
layouts/      AppShell: header tabs + the always-visible current-weather widget
```

The forecast page keeps its city and filters in the URL. Grid and chart both render one
memoised `applyFilters()` result, so a filter change moves both by construction.

### API

All routes are under `/api`; everything except `/auth/*` needs `Authorization: Bearer <jwt>`
and is scoped to the caller.

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/auth/register` · `/auth/login` · `/auth/refresh` · `/auth/logout` | Session lifecycle |
| GET | `/auth/me` | Current user |
| GET | `/weather/current/coordinates?lat&lon` | Widget (not recorded) |
| GET | `/weather/cities?q` | City autocomplete |
| GET | `/weather/forecast?city&countryCode` | 5-day forecast; **records a search** |
| GET | `/searches?page&pageSize` | Paged history |
| GET | `/statistics/top-cities` · `/statistics/recent` · `/statistics/conditions` | Statistics cards |

Errors are RFC 7807 `ProblemDetails`.

---

## Development

```bash
# Backend (needs the .NET 10 SDK)
cd weather-be
dotnet test                                  # unit + integration tests
dotnet ef migrations add <Name> -p WeatherApp.Infrastructure -s WeatherApp.API

# Frontend (needs Node 24)
cd weather-fe
npm ci
npm run dev                                  # expects the API on http://api:8080 — use Docker, or edit vite.config.ts
npm test                                     # Vitest
npm run lint && npm run build                # what CI would run
```

Secrets for running the API outside Docker go in user secrets, not `appsettings.json`:

```bash
dotnet user-secrets set "WeatherServiceConfiguration:ApiKey" "<key>" --project weather-be/WeatherApp.API
dotnet user-secrets set "Auth:Key" "<key>" --project weather-be/WeatherApp.API
```

---

## Decisions worth knowing

- **Single origin, no CORS** — the proxy is the whole cross-origin story.
- **Access token in memory, refresh token in an `HttpOnly` cookie** — nothing auth-related
  is readable from JavaScript; a page reload silently restores the session.
- **`GET /weather/forecast` has a side effect** — it writes the search row. Doing it
  server-side means a client can't fetch a forecast and "forget" to record it.
- **Condition snapshot on the `searches` row** — history and all three statistics are
  single-table queries with no second call to OpenWeather.
- **Forecast is aggregated on the backend** — OpenWeather returns 3-hour steps; the API
  returns those plus daily min/max summaries so grid and chart agree by construction.
- **Own SVG weather icons** — OpenWeather's PNG set renders "clear night" as a grey disc.
