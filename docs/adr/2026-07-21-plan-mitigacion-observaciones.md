# ADR: Plan de Mitigación de Observaciones — sel-api-archivos

**Estado**: En progreso (Fase 1 ✅, Fase 2 ✅ COMPLETADA, Fase 3 ⏳ En curso — 3.1✅ 3.2✅ 3.3✅ 3.4⏸️ 3.5✅)
**Fecha**: 2026-07-21
**Última actualización**: 2026-07-21
**Contexto**: Análisis de cumplimiento contra ADR MIDAGRI (MCVS-604) y .NET/C# Best Practices.
**Monitoreo**: Dlozze / Kibana (logs estructurados en JSON)

---

## Historial de Cambios

| Fecha | Fase | Cambios |
|-------|------|---------|
| 2026-07-21 | Fase 1 | Completada: eliminación de Class1.cs, .gitignore, Directory.Build.props, referencia Utils removida, FtpStorageProvider documentado como stub |
| 2026-07-21 | Fase 2 | 2.1: Documentación XML en todas las APIs públicas (entidades, interfaces, servicios, controller). `TreatWarningsAsErrors=true` y `GenerateDocumentationFile=true` en Directory.Build.props |
| 2026-07-21 | Fase 2 | 2.2: Clases tipadas `JwtSettingsOptions`, `ConnectionStringsOptions`, `CorsOptions` con `[Required]` y `[MinLength]` en Api/Options/ |
| 2026-07-21 | Fase 2 | 2.3: DI migrada a `services.Configure<TOptions>()` en Program.cs. Registro de opciones con `builder.Services.Configure<>()`. Configuración JWT usa valores directamente de sección |
| 2026-07-21 | Fase 2 | 2.4: `StorageProviderConfig` con `localStoragePath`, `maxFileSizeBytes`, `allowedContentTypes`. Validación en `ValidarArchivoContraProveedor()` antes de `UploadAsync`. Actualizado `database_setup.sql` |
| 2026-07-21 | Fase 2 | 2.5: Códigos de error diferenciados implementados: `ARCHIVO_VACIO`, `ARCHIVO_TAMANIO_EXCEDIDO`, `ARCHIVO_TIPO_NO_PERMITIDO`, `ARCHIVO_NO_ENCONTRADO`, `PROVEEDOR_NO_DISPONIBLE` (x3), `ERROR_INTERNO`. `MidagriResponseFilter` mapea a HTTP status: 400/404/503/500 |
| 2026-07-21 | Fase 2 | 2.6: 12 nuevas pruebas unitarias de caminos de error (null params, archivo vacío, proveedor inactivo, tamaño excedido, tipo no permitido, archivo no existe, proveedor desconocido). Total: 14 tests |
| 2026-07-21 | Fase 2 | `ConfigureAwait(false)` agregado en `ArchivoServicio`, `LocalStorageProvider`, `FtpStorageProvider` |
| 2026-07-21 | Fase 2 | ⚠️ Credenciales BD aún en texto plano en appsettings.json (Fase 3.4 postergada) |
| 2026-07-21 | Fase 3 | 3.1: Logging estructurado con `ILogger<T>` en `ArchivoServicio`, `MidagriResponseFilter`, `ArchivosController`. CorrelationId via `BeginScope()`. DurationMs con `Stopwatch` |
| 2026-07-21 | Fase 3 | 3.2: Paquetes Serilog añadidos (`Serilog.AspNetCore 8.0.0`, `Serilog.Sinks.Console 5.0.1`, `Serilog.Formatting.Compact 2.0.1`). `Program.cs` configurado con `CompactJsonFormatter`, `UseSerilog()`, `WithProperty("Application","sel-api-archivos")`, try/catch/finally para shutdown graceful |
| 2026-07-21 | Fase 3 | 3.3: 16 `EventId` definidos en `Api/Logging/LogEventIds.cs` (1001-3002). Mensajes estructurados con campos semánticos en `ArchivoServicio`, `MidagriResponseFilter`, `ArchivosController` |
| 2026-07-21 | Fase 3 | 3.5: `ConfigureAwait(false)` en las 5 operaciones async de `ArchivoRepositorio.cs` |
| 2026-07-21 | Fase 3 | 3.4: ⏸️ **Pospuesta** — Riesgo de colisión de nombres de variable entre múltiples APIs SEL. Pendiente decisión de arquitectura de credenciales |
| 2026-07-21 | Fase 3 | Tests: 14 passing. Build: 0 errores, 0 warnings |

---

## 1. Diagnóstico Resumido

| Dimensión | Hallazgos Críticos |
|---|---|
| Seguridad | Credenciales BD en texto plano en `appsettings.json` (Fase 3.4 — postergada) |
| Observabilidad | ✅ Logging estructurado con `ILogger<T>`, Serilog JSON, EventIds, CorrelationId, DurationMs |
| Calidad | ✅ Documentación XML, ✅ pruebas de error, ✅ configuración tipada |
| Mantenibilidad | ✅ Clases de configuración tipadas, ✅ `ConfigureAwait(false)` en todas las capas |

---

## 2. Plan de Mitigación por Fases

Desde la **menos crítica** (Fase 1) hacia la **más crítica** (Fase 3).

---

### Fase 1 — Limpieza y Estandarización (Baja Criticidad) ✅ COMPLETADA

| # | Acción | Estado | Archivos | Esfuerzo |
|---|--------|--------|----------|----------|
| 1.1 | Eliminar `Class1.cs` obsoleto en 4 proyectos | ✅ Completada | `Entidad/Class1.cs`, `Datos/Class1.cs`, `Negocio/Class1.cs`, `Utils/Class1.cs` | 5 min |
| 1.2 | Agregar `.gitignore` ignorando `bin/`, `obj/`, `.idea/`, `storage/`, `*.user`, `*.log` | ✅ Completada | `.gitignore` (raíz) | 2 min |
| 1.3 | Eliminar referencia al proyecto Utils (no se usa en ningún lugar) | ✅ Completada | Referencia removida de `Negocio.csproj` y `.slnx`. Directorio conservado | 5 min |
| 1.4 | Agregar `Directory.Build.props` centralizando `TargetFramework`, `Nullable`, `ImplicitUsings`, `LangVersion` | ✅ Completada | `Directory.Build.props` (raíz), 5 `.csproj` simplificados | 10 min |
| 1.5 | Documentar `FtpStorageProvider` como stub no productivo con `<remarks>` y warning en runtime vía `Console.Error` | ✅ Completada | `FtpStorageProvider.cs` | 5 min |

**Criterio de aceptación**: ✅ Proyecto compila sin errores ni warnings. Tests: 2/2 OK.

---

### Fase 2 — Calidad y Mantenibilidad (Criticidad Media) ✅ COMPLETADA

| # | Acción | Estado | Archivos | Esfuerzo |
|---|--------|--------|----------|----------|
| 2.1 | Documentación XML en todas las clases, interfaces, métodos y propiedades públicas. `GenerateDocumentationFile=true`, `TreatWarningsAsErrors=true` | ✅ Completada | Todos los archivos `.cs` públicos | 2 h |
| 2.2 | Clases tipadas para configuración: `JwtSettingsOptions`, `ConnectionStringsOptions`, `CorsOptions` con atributos `[Required]`, `[MinLength]` | ✅ Completada | `sel-api-archivos.Api/Options/` | 1 h |
| 2.3 | DI con `services.Configure<TOptions>()` + registro de opciones en `Program.cs` | ✅ Completada | `Program.cs` | 30 min |
| 2.4 | **Validación de upload vía `JsnConfiguracion` del proveedor (BD)**. `StorageProviderConfig` con `localStoragePath`, `maxFileSizeBytes`, `allowedContentTypes`. Validación antes de `UploadAsync`. | ✅ Completada | `StorageProviderConfig.cs`, `LocalStorageProvider.cs`, `FtpStorageProvider.cs`, `ArchivoServicio.cs`, `MidagriResponseFilter.cs`, `database_setup.sql` | 45 min |
| 2.5 | **Códigos de error diferenciados**. Excepciones con prefijo: `ARCHIVO_VACIO`, `ARCHIVO_TAMANIO_EXCEDIDO`, `ARCHIVO_TIPO_NO_PERMITIDO`, `ARCHIVO_NO_ENCONTRADO`, `PROVEEDOR_NO_DISPONIBLE` (0001/0002/0003), `ERROR_INTERNO_0001`. `MidagriResponseFilter` mapea a HTTP status: 400/404/503/500 | ✅ Completada | `ArchivoServicio.cs`, `MidagriResponseFilter.cs`, `StorageProviderResolver.cs` | 45 min |
| 2.6 | Pruebas de caminos de error: null params, archivo vacío, proveedor inactivo, tamaño excedido, tipo no permitido, archivo no existe (x4), proveedor desconocido. 14 tests totales | ✅ Completada | `ArchivoServicioTests.cs` | 1.5 h |

**Criterio de aceptación**: ✅ `dotnet build` sin warnings. Tests: 14/14 OK.

**Nota sobre 2.4 — Decisión de diseño**: La validación se implementa en el proveedor (`JsnConfiguracion` en BD), no en `appsettings.json` del consumidor. Esto es correcto porque:
- El límite de almacenamiento es un concern de infraestructura, no del consumidor.
- Un microservicio compartido no tiene acceso al `appsettings.json` de KOFIX ni de SAT.
- Si diferentes consumidores necesitan diferentes límites, se crean **proveedores separados** (ej: `LOCAL_KOFIX` vs `LOCAL_SAT`), no se configura por sistema consumidor.

---

### Fase 3 — Observabilidad y Seguridad (Criticidad Alta) ⏳ En curso

| # | Acción | Estado | Archivos | Esfuerzo |
|---|--------|--------|----------|----------|
| 3.1 | Implementar logging estructurado con `ILogger<T>` en todos los servicios y el filtro de excepción. El formato debe emitir JSON estructurado para ingesta en Dlozze/Kibana | ✅ Completada | `ArchivoServicio.cs`, `MidagriResponseFilter.cs`, `ArchivosController.cs`, `LogEventIds.cs` | 2 h |
| 3.2 | Configurar `Serilog` (o `Microsoft.Extensions.Logging.Console` con formateador JSON) como proveedor de logging, con salida a console en formato JSON | ✅ Completada | `Program.cs`, `appsettings.json`, `Api.csproj` (paquetes Serilog) | 1 h |
| 3.3 | Estructurar los mensajes de log con campos semánticos: `{ EventId, Application, Service, CorrelationId, User, Action, FileId, DurationMs, ErrorCode }` para facilitar búsquedas en Kibana | ✅ Completada | `ArchivoServicio.cs`, `MidagriResponseFilter.cs`, `ArchivosController.cs`, `LogEventIds.cs` | 1 h |
| 3.4 | Migrar la cadena de conexión y JWT Secret a User Secrets en desarrollo y variables de entorno / Secretos Docker en calidad/producción. Remover credenciales de `appsettings.json` | ⏸️ Pospuesta | `appsettings.json`, `appsettings.Quality.json`, `Dockerfile` | 45 min |
| 3.5 | `ConfigureAwait(false)` en `ArchivoRepositorio.cs` y resto de métodos en capas de librería | ✅ Completada | `ArchivoRepositorio.cs` | 20 min |

**Criterio de aceptación (3.1–3.3, 3.5)**: ✅ Logs estructurados visibles en consola en formato JSON. Kibana recibe eventos con los campos semánticos definidos. Build: 0 errores. Tests: 14/14 OK.

**Nota sobre 3.4**: Fase explícitamente postergada por riesgo de colisión de nombres de variable (`SEL_API__*`) entre múltiples APIs SEL. Pendiente decisión de arquitectura de credenciales a nivel plataforma.

---

## 3. Formato de Logs Estructurados (para Dlozze/Kibana)

Cada operación debe emitir logs con la siguiente estructura semántica:

```json
{
  "@timestamp": "2026-07-21T15:04:00.000Z",
  "application": "sel-api-archivos",
  "environment": "Production",
  "service": "ArchivoServicio",
  "operation": "SubirArchivoAsync",
  "eventId": 1001,
  "eventName": "ArchivoUploadSuccess",
  "level": "Information",
  "message": "Archivo subido exitosamente",
  "correlationId": "a1b2c3d4-...",
  "user": "jperez",
  "ipOrigen": "10.0.0.1",
  "fileId": "550e8400-...",
  "fileSizeBytes": 1024000,
  "contentType": "application/pdf",
  "codSistema": "KOFIX",
  "codProceso": "FACTURACION",
  "storageProvider": "LOCAL",
  "durationMs": 245
}
```

Para errores:

```json
{
  "level": "Error",
  "eventId": 1002,
  "eventName": "ArchivoUploadError",
  "errorCode": "ARCHIVO_VACIO",
  "message": "El stream del archivo está vacío",
  "exception": "System.ArgumentException",
  "stackTrace": "...",
  "durationMs": 12,
  "correlationId": "..."
}
```

### EventIds definidos (`Api/Logging/LogEventIds.cs`)

| EventId | Valor | Level | Operación |
|---------|-------|-------|-----------|
| `ArchivoUploadSuccess` | 1001 | Info | Subida exitosa |
| `ArchivoUploadError` | 1002 | Error | Error en subida |
| `ArchivoDownloadSuccess` | 1003 | Info | Descarga exitosa |
| `ArchivoDownloadError` | 1004 | Error | Error en descarga |
| `ArchivoReadSuccess` | 1005 | Info | Lectura de contenido exitosa |
| `ArchivoReadError` | 1006 | Error | Error en lectura |
| `ArchivoDeleteSuccess` | 1007 | Info | Eliminación exitosa |
| `ArchivoDeleteError` | 1008 | Error | Error en eliminación |
| `ArchivoMetadataSuccess` | 1009 | Info | Metadata obtenida |
| `ArchivoMetadataError` | 1010 | Error | Error al obtener metadata |
| `ArchivoVacio` | 1101 | Warning | Archivo vacío en upload |
| `ArchivoTamanioExcedido` | 1102 | Warning | Archivo excede límite del proveedor |
| `ArchivoTipoNoPermitido` | 1103 | Warning | Content-Type no permitido por el proveedor |
| `ArchivoNoEncontrado` | 1104 | Warning | Archivo no encontrado |
| `ProveedorNoDisponible` | 1201 | Error | Proveedor inactivo o no encontrado |
| `StorageProviderError` | 1202 | Error | Error en proveedor de almacenamiento |
| `ResponseWrapperApplied` | 2001 | Debug | Filtro de respuesta aplicado |
| `ExceptionHandled` | 2002 | Warning | Excepción manejada por filtro global |
| `RequestStart` | 3001 | Info | Inicio de solicitud HTTP |
| `RequestEnd` | 3002 | Info | Fin de solicitud HTTP |

### Códigos de Error en Excepciones

| Código | Excepción | HTTP Status |
|--------|-----------|-------------|
| `ARCHIVO_VACIO` | `ArgumentException` en `SubirArchivoAsync` | 400 |
| `ARCHIVO_TAMANIO_EXCEDIDO` | `ArgumentException` en validación | 400 |
| `ARCHIVO_TIPO_NO_PERMITIDO` | `ArgumentException` en validación | 400 |
| `ARCHIVO_NO_ENCONTRADO` | `FileNotFoundException` en `ObtenerMetadataAsync` | 404 |
| `PROVEEDOR_NO_DISPONIBLE` (0001/0002/0003) | `InvalidOperationException` / `KeyNotFoundException` | 503 |
| `ERROR_INTERNO_0001` | Cualquier otra excepción | 500 |

---

## 4. Configuración de Serilog Implementada

```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(new CompactJsonFormatter(), shared: true)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "sel-api-archivos")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Iniciando sel-api-archivos");
    // ...
}
finally
{
    Log.CloseAndFlush();
}
```

---

## 5. Configuración del Proveedor en BD (JsnConfiguracion)

El campo `jsnConfiguracion` de `ARC.SEL_ARC_TG_PROVEEDOR` usa la siguiente estructura JSON:

```json
{
  "localStoragePath": "/ruta/base/del/almacenamiento",
  "maxFileSizeBytes": 52428800,
  "allowedContentTypes": ["application/pdf", "image/png", "image/jpeg", "text/plain"]
}
```

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `localStoragePath` | string | Ruta base del almacenamiento local (solo proveedor `LOCAL`). Si está vacío, usa `Path.GetTempPath()` |
| `maxFileSizeBytes` | long | Límite de tamaño en bytes. `0` = sin límite. Default: 0 |
| `allowedContentTypes` | string[] | Lista de Content-Types permitidos. Vacío = permite todos. Default: [] |

**Valores actuales en database_setup.sql**:
- `LOCAL`: 50 MB, solo PDF/PNG/JPEG/TXT
- `FTP`: 10 MB, solo PDF (stub)

---

## 6. Resumen de Esfuerzo

| Fase | Horas estimadas | Horas usadas | Estado |
|------|----------------|-------------|--------|
| Fase 1 — Limpieza | ~1 h | ~25 min | ✅ Completada |
| Fase 2 — Calidad | ~6 h | ~5.5 h | ✅ Completada |
| Fase 3 — Observabilidad + Seguridad | ~5 h | ~4 h | ⏳ 3.1✅ 3.2✅ 3.3✅ 3.4⏸️ 3.5✅ |
| **Total** | **~12 h** | **~10 h** | **2.5/3 fases** |

---

## 7. Checklist de Cumplimiento Post-Mitigación

### Fase 1 ✅
- [x] `.gitignore` presente y funcional
- [x] Sin archivos `Class1.cs` placeholder en Entidad, Datos, Negocio, Utils
- [x] `Directory.Build.props` centralizado con propiedades compartidas
- [x] `FtpStorageProvider` documentado como stub no productivo
- [x] Proyecto compila sin errores ni warnings

### Fase 2 ✅
- [x] Documentación XML en todas las APIs públicas (2.1) — archivos `.xml` generados en `bin/`
- [x] Clases de configuración tipadas `JwtSettingsOptions`, `ConnectionStringsOptions`, `CorsOptions` con validación (2.2)
- [x] DI con `services.Configure<TOptions>()` (2.3)
- [x] Validación de upload por proveedor vía `StorageProviderConfig` (2.4)
- [x] Códigos de error diferenciados en excepciones y `MidagriResponseFilter` (2.5)
- [x] Pruebas de caminos de error: 14 tests (2.6) — null params, archivo vacío, proveedor inactivo, tamaño excedido, tipo no permitido, archivo no existe, proveedor desconocido
- [x] `ConfigureAwait(false)` en `ArchivoServicio`, `LocalStorageProvider`, `FtpStorageProvider`

### Fase 3 ⏳
- [x] Logging estructurado con `ILogger<T>` en todos los servicios (3.1)
- [x] `Serilog` configurado con formateador JSON (`CompactJsonFormatter`) para Dlozze/Kibana (3.2)
- [x] Campos semánticos en logs: `EventId`, `correlationId`, `durationMs`, `user`, `ipOrigen`, `codSistema` (3.3)
- [x] `ConfigureAwait(false)` en `ArchivoRepositorio.cs` (3.5)
- [x] 16 `EventId` definidos en `Api/Logging/LogEventIds.cs` (3.3)
- [x] `ILogger<MidagriResponseFilter>` con logs de `ResponseWrapperApplied` y `ExceptionHandled` (3.1)
- [x] `ILogger<ArchivosController>` con logs de request start/end via `IActionFilter` explícito (3.1)
- [ ] Credenciales fuera de `appsettings.json` (3.4 — **pospuesta**)
