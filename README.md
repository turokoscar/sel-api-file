# sel-api-archivos

.NET 8.0 ASP.NET Core Web API for file upload, download, delete, and metadata management. Part of AGROIDEAS SEL (Sistema de Elegibilidad).

## Architecture

5 projects (`sel-api-archivos.slnx`), all targeting `net8.0`:

| Project | Role |
|---|---|
| `sel-api-archivos.Api` | Entry point — `Program.cs`, `ArchivosController`, `MidagriResponseFilter`, `LogEventIds` |
| `sel-api-archivos.Negocio` | Business logic — `ArchivoServicio`, storage providers (`IStorageProvider`) |
| `sel-api-archivos.Datos` | Dapper + SQL Server via stored procedures (`IArchivoRepositorio`) |
| `sel-api-archivos.Entidad` | POCO entities (`ArchivoEntity`, `ProveedorEntity`, `AuditoriaEntity`, `RespuestaEstandar`) |
| `sel-api-archivos.Pruebas` | xUnit + NSubstitute + FluentAssertions (unit tests only) |
| `sel-api-archivos.Utils` | Referenced but empty — placeholder only |

## API Endpoints

All under `[Authorize]` → `/archivos`:

| Method | Route | Notes |
|---|---|---|
| POST | `/archivos` | Multipart form: `archivo` (IFormFile), `codSistema`, `codProceso` (optional) |
| GET | `/archivos/{id:guid}` | Metadata only |
| GET | `/archivos/{id:guid}/descarga` | Raw `FileResult` (not wrapped in `RespuestaEstandar`) |
| GET | `/archivos/{id:guid}/contenido` | Reads text content |
| DELETE | `/archivos/{id:guid}` | Soft delete (logical) |

## Response Wrapper

`MidagriResponseFilter` wraps all JSON responses in `RespuestaEstandar` (`{ respuesta: "OK"|"ERROR", mensaje, datos }`). Two exceptions:
- `FileResult` (downloads) → raw binary
- If the controller already returns `RespuestaEstandar` → no double-wrap

## Storage Providers (Strategy Pattern)

Pluggable via `IStorageProvider` / `IStorageProviderResolver`. Resolved by `ProviderCode` from DB.

- **`LOCAL`** (`LocalStorageProvider`) — file system, configured via `localStoragePath` in provider's `JsnConfiguracion` JSON
- **`FTP`** (`FtpStorageProvider`) — **stub, not production-ready**. Documented with `<remarks>` and runtime warning

Active provider selected from `ARC.SEL_ARC_TG_PROVEEDOR` (first with `flgActivo = 1`).

Provider config (`JsnConfiguracion`):
```json
{
  "localStoragePath": "/ruta/base",
  "maxFileSizeBytes": 52428800,
  "allowedContentTypes": ["application/pdf", "image/png"]
}
```

## Authentication

JWT Bearer. Configurable via `JwtSettings` section in `appsettings.json`. Swagger UI configured with Bearer token input.

## Logging (Structured JSON)

Serilog with `CompactJsonFormatter` → console sink → Dlozze/Kibana.

16 EventIds defined in `Api/Logging/LogEventIds.cs` (range 1001–3002):
- 1001–1010: File operations (upload, download, read, delete, metadata)
- 1101–1104: Validation errors (empty, size exceeded, type not allowed, not found)
- 1201–1202: Provider errors
- 2001–2002: Filter lifecycle
- 3001–3002: HTTP request lifecycle

Structured fields: `EventId`, `CorrelationId`, `User`, `IpOrigen`, `CodSistema`, `FileId`, `DurationMs`, `StatusCode`.

## Database

SQL Server, schema `ARC`, 3 tables + 5 stored procedures. Setup script at `data/database_setup.sql`.

Stored procedures: `ARC.SEL_ARC_SP_{C,R,D}_ARCHIVO`, `ARC.SEL_ARC_SP_C_AUDITORIA`, `ARC.SEL_ARC_SP_R_PROVEEDORES`.

## Docker

Multistage: `mcr.microsoft.com/dotnet/sdk:8.0` → `mcr.microsoft.com/dotnet/aspnet:8.0`.
- Timezone: `America/Lima`
- Non-root user `appuser`
- `storage/` directory with proper ownership
- Exposes port `8080`

## Commands

```bash
dotnet restore && dotnet build    # Build all projects
dotnet test sel-api-archivos.Pruebas  # Run unit tests (14 tests)
dotnet run --project sel-api-archivos.Api  # Dev server (requires SQL Server)
```

## Error Codes

| Code | HTTP Status | Cause |
|------|-------------|-------|
| `ARCHIVO_VACIO` | 400 | Empty file stream on upload |
| `ARCHIVO_TAMANIO_EXCEDIDO` | 400 | File exceeds provider's `maxFileSizeBytes` |
| `ARCHIVO_TIPO_NO_PERMITIDO` | 400 | Content-Type not in provider's `allowedContentTypes` |
| `ARCHIVO_NO_ENCONTRADO` | 404 | File ID not found in DB |
| `PROVEEDOR_NO_DISPONIBLE` | 503 | No active storage provider |
| `ERROR_INTERNO_0001` | 500 | Unhandled exception |

## Conventions

- Entity properties use Hungarian-like prefixes: `TxtNombreOriginal`, `CanTamanioBytes`, `FlgActivo`, `IdeArchivo`, `FecCreacion`
- Audit trail on every action logged to `ARC.SEL_ARC_TC_AUDITORIA`
- SHA256 checksum computed on upload
- `ConfigureAwait(false)` on all `await` in `Datos` and `Negocio` layers
- `FtpStorageProvider` is a documented stub; do not use in production
