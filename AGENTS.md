# sel-api-archivos

.NET 8.0 ASP.NET Core Web API for file upload/download/delete. Part of AGROIDEAS SEL (Sistema de Elegibilidad).

## Architecture

5 projects in solution (`sel-api-archivos.slnx`), all net8.0 via `Directory.Build.props`:

| Project | Role |
|---|---|
| `sel-api-archivos.Api` | Entrypoint — `Program.cs`, `ArchivosController`, `MidagriResponseFilter` |
| `sel-api-archivos.Negocio` | Business logic — `ArchivoServicio`, storage providers (`IStorageProvider`) |
| `sel-api-archivos.Datos` | Dapper + SQL Server via stored procedures (`IArchivoRepositorio`) |
| `sel-api-archivos.Entidad` | POCOs (`ArchivoEntity`, `ProveedorEntity`, `AuditoriaEntity`, `RespuestaEstandar`) |
| `sel-api-archivos.Pruebas` | xUnit + NSubstitute + FluentAssertions (unit tests only, no integration) |

`sel-api-archivos.Utils/` exists on disk but is **not in the solution** and not referenced by any project. Do not add code here.

Shared properties (`TargetFramework=net8.0`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=12`, `TreatWarningsAsErrors=true`, `GenerateDocumentationFile=true`) are centralized in `Directory.Build.props`. Pruebas overrides `GenerateDocumentationFile=false`.

## API Endpoints

All under `[Authorize]` → `/archivos`:

| Method | Route | Notes |
|---|---|---|
| POST | `/archivos` | Multipart form: `archivo` (IFormFile), `codSistema`, `codProceso` (optional) |
| GET | `/archivos/{id:guid}` | Metadata only |
| GET | `/archivos/{id:guid}/descarga` | Returns raw `FileResult` (not wrapped in `RespuestaEstandar`) |
| GET | `/archivos/{id:guid}/contenido` | Reads text content |
| DELETE | `/archivos/{id:guid}` | Soft delete (logical) |

## Response Wrapper

`MidagriResponseFilter` wraps all JSON responses in `RespuestaEstandar` (`{ respuesta: "OK"|"ERROR", mensaje, datos }`). Two exceptions:
- `FileResult` (downloads) → raw binary
- If the controller already returns `RespuestaEstandar` → no double-wrap

## Storage Providers (Strategy Pattern)

Pluggable via `IStorageProvider` / `IStorageProviderResolver`. Registered in DI as multiple `IStorageProvider` instances and resolved by `ProviderCode`.

- **`LOCAL`** (`LocalStorageProvider`) — file system, configured via `localStoragePath` in provider's `JsnConfiguracion` JSON
- **`FTP`** (`FtpStorageProvider`) — **stub, not production-ready**. Emits `Console.Error.WriteLine` on every call; returns fake URLs and simulated content. Documented with `<remarks>` in XML docs

Active provider selected at runtime from DB table `ARC.API_FILE_TG_PROVEEDOR` (first with `flgActivo = 1`).

## Database

SQL Server, schema `ARC`, database `BD_API_FILE`. Setup script at `data/database_setup.sql`.

### Tables
- `ARC.API_FILE_TG_PROVEEDOR` — storage providers
- `ARC.API_FILE_TMM_ARCHIVO` — file metadata
- `ARC.API_FILE_TC_AUDITORIA` — access audit log
- `ARC.API_FILE_TG_PARAMETRO` — dynamic config parameters
- `ARC.API_FILE_TC_MIGRACIONES` — migration version tracking

### Stored Procedures (9)
- `ARC.API_FILE_SP_C_ARCHIVO` — register new file
- `ARC.API_FILE_SP_R_ARCHIVO` — get metadata by ID
- `ARC.API_FILE_SP_U_ARCHIVO` — update metadata
- `ARC.API_FILE_SP_D_ARCHIVO` — logical delete
- `ARC.API_FILE_SP_C_AUDITORIA` — register audit action
- `ARC.API_FILE_SP_R_ARCHIVOS_POR_SISTEMA` — list files with pagination
- `ARC.API_FILE_SP_R_ARCHIVO_POR_CHECKSUM` — find file by SHA256
- `ARC.API_FILE_SP_R_PROVEEDORES` — list active providers
- `ARC.API_FILE_SP_R_PROVEEDOR_POR_ID` — get provider by ID

### Naming mismatch (legacy vs migrations)

`database_setup.sql` uses `SEL_ARC_*` naming (`SEL_ARC_SP_C_ARCHIVO`, `SEL_ARC_TG_PROVEEDOR`, etc.) while the migrations under `data/migrations/` and C# code use `API_FILE_*` naming. The migrations are the source of truth for production; `database_setup.sql` is the legacy monolithic script.

### Migrations

Flyway-style `V{NNN}__{description}.sql` files in `data/migrations/`. Applied via `scripts/deploy.ps1` (PowerShell, uses `sqlcmd`). Rollback script: `scripts/rollback.sql`.

## Authentication

JWT Bearer. Configurable via `JwtSettings` section in `appsettings.json`. Swagger UI configured with Bearer token input (Development environment only). CORS configured per-environment in `Cors:Origenes`.

## Logging (Structured JSON — Dlozze/Kibana)

- **Provider**: Serilog with `CompactJsonFormatter`, console sink
- **EventIds**: Defined in `Api/Logging/LogEventIds.cs` (20 EventIds, range 1001–3002)
  - 1001–1010: File operations (upload, download, read, delete, metadata)
  - 1101–1104: Validation errors (empty, size exceeded, type not allowed, not found)
  - 1201–1202: Provider errors
  - 2001–2002: Filter lifecycle
  - 3001–3002: HTTP request lifecycle
- **Structured fields**: `EventId`, `CorrelationId`, `User`, `IpOrigen`, `CodSistema`, `FileId`, `DurationMs`, `StatusCode`, `ErrorCode`
- **CorrelationId**: Injected via `BeginScope()` in `ArchivoServicio`; carried via `HttpContext.Items` in `ArchivosController`
- **DurationMs**: Measured via `Stopwatch` in both `ArchivoServicio` and `ArchivosController`
- **Controller request lifecycle**: `IActionFilter` explicit implementation (not override) — `ControllerBase` does not support `OnActionExecuting`/`OnActionExecuted` overrides
- **Serilog bootstrap**: `CompactJsonFormatter`, `WithProperty("Application","sel-api-archivos")`, graceful shutdown via `try/finally` in `Program.cs`
- **`ConfigureAwait(false)`**: All `await` in `ArchivoRepositorio.cs` and all storage providers

## Commands

```bash
dotnet restore && dotnet build    # Build all projects
dotnet test sel-api-archivos.Pruebas  # Run unit tests (xUnit, 14 tests, no DB needed)
dotnet run --project sel-api-archivos.Api  # Development server (requires SQL Server)
```

No CI, no pre-commit hooks, no codegen, no formatter/lint config in repo.

## Conventions & Quirks

- All entity property names use Hungarian-like prefixes (`TxtNombreOriginal`, `CanTamanioBytes`, `FlgActivo`, `IdeArchivo`, `FecCreacion`)
- Audit trail on every action: UPLOAD, DOWNLOAD, READ, DELETE logged to `ARC.API_FILE_TC_AUDITORIA`
- SHA256 checksum computed on upload (stream must be seekable or buffered)
- `.gitignore` present — `bin/`, `obj/`, `storage/`, `.idea/`, `*.user`, `*.log` ignored
- `sel-api-archivos.Utils/` exists but is not in the solution and not referenced — do not add code here without a clear purpose
- `Program.cs` uses top-level statements with explicit service registration (no reflection/scrutor)
- `FtpStorageProvider` is a documented stub; do not use in production
- `TreatWarningsAsErrors=true` is on globally — fix all warnings before build
