# sel-api-archivos

.NET 8.0 ASP.NET Core Web API for file upload/download/delete. Part of AGROIDEAS SEL (Sistema de Elegibilidad).

## Architecture

5 projects in solution (`sel-api-archivos.slnx`), all net8.0 via `Directory.Build.props`:

| Project | Role |
|---|---|
| `sel-api-archivos.Api` | Entrypoint — `Program.cs`, `ArchivosController`, `MidagriResponseFilter` |
| `sel-api-archivos.Negocio` | Business logic — `ArchivoServicio`, storage providers (`IStorageProvider`) |
| `sel-api-archivos.Datos` | Dapper + SQL Server via stored procedures (`IArchivoRepositorio`) |
| `sel-api-archivos.Entidad` | POCO entities (`ArchivoEntity`, `ProveedorEntity`, `AuditoriaEntity`, `RespuestaEstandar`) |
| `sel-api-archivos.Pruebas` | xUnit + NSubstitute + FluentAssertions (unit tests only, no integration) |
| `sel-api-archivos.Utils` | Referenced by Negocio but unused — placeholder only, no utility code yet |

Shared properties (`TargetFramework=net8.0`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=12`) are centralized in `Directory.Build.props`. No `Directory.Build.props` = not part of the solution.

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

- **`LOCAL`** (`LocalStorageProvider`) — file system, configured via `LocalStoragePath` in provider's `JsnConfiguracion` JSON
- **`FTP`** (`FtpStorageProvider`) — **stub, not production-ready**. Emits `Console.Error.WriteLine` on every call and is documented with `<remarks>` in XML docs

Active provider selected at runtime from DB table `ARC.SEL_ARC_TG_PROVEEDOR` (first with `flgActivo = 1`).

## Database

SQL Server, schema `ARC`, 3 tables + 5 stored procedures. Setup script at `data/database_setup.sql`.

Stored procedures used: `ARC.SEL_ARC_SP_{C,R,D}_ARCHIVO`, `ARC.SEL_ARC_SP_C_AUDITORIA`, `ARC.SEL_ARC_SP_R_PROVEEDORES`.

## Authentication

JWT Bearer. Configurable via `JwtSettings:Secret`, `JwtSettings:Issuer`, `JwtSettings:Audience`. Currently using Dev secrets from `appsettings.json`. Swagger UI configured with Bearer token input.

## Docker

Multistage: `mcr.microsoft.com/dotnet/sdk:8.0` build → `mcr.microsoft.com/dotnet/aspnet:8.0` runtime.
- Timezone: `America/Lima`
- Non-root user `appuser`
- `storage/` directory created with proper ownership
- Exposes `8080`

## Commands

```bash
dotnet restore && dotnet build    # Build all projects
dotnet test sel-api-archivos.Pruebas  # Run unit tests (xUnit, no DB needed)
dotnet run --project sel-api-archivos.Api  # Development server (requires SQL Server)
```

No CI, no pre-commit hooks, no codegen, no formatter/lint config in repo.

## Conventions & Quirks

- All entity property names use Hungarian-like prefixes (`TxtNombreOriginal`, `CanTamanioBytes`, `FlgActivo`, `IdeArchivo`, `FecCreacion`)
- Audit trail on every action: UPLOAD, DOWNLOAD, READ, DELETE logged to `ARC.SEL_ARC_TC_AUDITORIA`
- SHA256 checksum computed on upload (stream must be seekable or buffered)
- `.gitignore` present — `bin/`, `obj/`, `storage/`, `.idea/`, `*.user`, `*.log` ignored
- `sel-api-archivos.Utils/` — referenced but empty; do not add code here without a clear purpose
- `Program.cs` uses top-level statements with explicit service registration (no reflection/scrutor)
- `FtpStorageProvider` is a documented stub; do not use in production
