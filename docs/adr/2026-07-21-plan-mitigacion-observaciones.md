# ADR: Plan de Mitigación de Observaciones — sel-api-archivos

**Estado**: En progreso (Fase 1 completada)
**Fecha**: 2026-07-21
**Última actualización**: 2026-07-21
**Contexto**: Análisis de cumplimiento contra ADR MIDAGRI (MCVS-604) y .NET/C# Best Practices.
**Monitoreo**: Dlozze / Kibana (logs estructurados en JSON)

---

## Historial de Cambios

| Fecha | Fase | Cambios |
|-------|------|---------|
| 2026-07-21 | Fase 1 | Completada: eliminación de Class1.cs, .gitignore, Directory.Build.props, referencia Utils removida, FtpStorageProvider documentado como stub |

---

## 1. Diagnóstico Resumido

| Dimensión | Hallazgos Críticos |
|---|---|
| Seguridad | Credenciales BD en texto plano en `appsettings.json` |
| Observabilidad | Sin logging estructurado (`ILogger`), errores sin diferenciación |
| Calidad | Sin documentación XML, sin pruebas de error, código placeholder obsoleto |
| Mantenibilidad | Sin clases de configuración tipadas, sin `ConfigureAwait(false)` |

---

## 2. Plan de Mitigación por Fases

Desde la **menos crítica** (Fase 1) hacia la **más crítica** (Fase 3).

---

### Fase 1 — Limpieza y Estandarización (Baja Criticidad) ✅ COMPLETADA

| # | Acción | Estado | Archivos | Esfuerzo |
|---|--------|--------|----------|----------|
| 1.1 | Eliminar `Class1.cs` obsoleto en 4 proyectos | ✅ Completada | `Entidad/Class1.cs`, `Datos/Class1.cs`, `Negocio/Class1.cs`, `Utils/Class1.cs` | 5 min |
| 1.2 | Agregar `.gitignore` ignorando `bin/`, `obj/`, `.idea/`, `storage/`, `*.user`, `*.log` | ✅ Completada | `.gitignore` (raíz) | 2 min |
| 1.3 | Eliminar referencia al proyecto `Utils` (no se usa en ningún lugar) | ✅ Completada | Referencia removida de `Negocio.csproj` y `.slnx`. Directorio conservado | 5 min |
| 1.4 | Agregar `Directory.Build.props` centralizando `TargetFramework`, `Nullable`, `ImplicitUsings`, `LangVersion`. Propiedades duplicadas eliminadas de los 5 `.csproj` restantes | ✅ Completada | `Directory.Build.props` (raíz), 5 `.csproj` simplificados | 10 min |
| 1.5 | Documentar `FtpStorageProvider` como stub no productivo con `<remarks>` y warning en runtime vía `Console.Error` | ✅ Completada | `FtpStorageProvider.cs` | 5 min |

**Criterio de aceptación**: ✅ Proyecto compila sin errors ni warnings. Tests: 2/2 OK.

**Nota sobre Utils**: El proyecto `sel-api-archivos.Utils` y su `.csproj` fueron conservados en disco tras remover la referencia de `Negocio.csproj` y `.slnx`. El directorio persiste como placeholder por si se necesita en el futuro. Para eliminarlo completamente, eliminar manualmente `sel-api-archivos.Utils/`.

---

### Fase 2 — Calidad y Mantenibilidad (Criticidad Media) ⏳ Pendiente

| # | Acción | Archivos | Esfuerzo |
|---|--------|----------|----------|
| 2.1 | Agregar documentación XML en todas las clases, interfaces, métodos y propiedades públicas | Todos los archivos `.cs` | 2 h |
| 2.2 | Crear clases fuertemente tipadas para configuración: `JwtSettingsOptions`, `ConnectionStringsOptions`, `CorsOptions` con atributos de validación | `sel-api-archivos.Api/Options/` | 1 h |
| 2.3 | Migrar DI de configuración a `services.Configure<TOptions>()` + `IOptions<T>` / `IOptionsSnapshot<T>` | `Program.cs` + servicios | 30 min |
| 2.4 | Agregar validación de entrada en upload: límite de tamaño (`[RequestSizeLimit]`), validación de `ContentType` permitido | `ArchivosController.cs` | 30 min |
| 2.5 | Diferenciar códigos de error en `MidagriResponseFilter`: `ARCHIVO_NO_ENCONTRADO`, `ARCHIVO_VACIO`, `PROVEEDOR_NO_DISPONIBLE`, etc. | `MidagriResponseFilter.cs`, `ArchivoServicio.cs` | 45 min |
| 2.6 | Agregar pruebas unitarias de caminos de error: null parameters, ID inválido, archivo faltante, proveedor inactivo | `ArchivoServicioTests.cs` | 1.5 h |

**Criterio de aceptación**: `dotnet build` sin warnings de documentación faltante. Tests pasan. Validaciones de upload funcionales.

---

### Fase 3 — Observabilidad y Seguridad (Criticidad Alta) ⏳ Pendiente

| # | Acción | Archivos | Esfuerzo |
|---|--------|----------|----------|
| 3.1 | Implementar logging estructurado con `ILogger<T>` en todos los servicios y el filtro de excepción. El formato debe emitir JSON estructurado para ingesta en Dlozze/Kibana | `ArchivoServicio.cs`, `MidagriResponseFilter.cs`, `ArchivosController.cs` | 2 h |
| 3.2 | Configurar `Serilog` (o `Microsoft.Extensions.Logging.Console` con formateador JSON) como proveedor de logging, con salida a console en formato JSON | `Program.cs`, `appsettings.json` | 1 h |
| 3.3 | Estructurar los mensajes de log con campos semánticos: `{ EventId, Application, Service, CorrelationId, User, Action, FileId, DurationMs, ErrorCode }` para facilitar búsquedas en Kibana | Todos los servicios | 1 h |
| 3.4 | Migrar la cadena de conexión y JWT Secret a User Secrets en desarrollo y variables de entorno / Secretos Docker en calidad/producción. Remover credenciales de `appsettings.json` | `appsettings.json`, `appsettings.Quality.json`, `Dockerfile` | 45 min |
| 3.5 | Agregar `ConfigureAwait(false)` en todas las llamadas `await` en la capa de Negocio y Datos (proyectos de librería, no en Api) | `ArchivoServicio.cs`, `ArchivoRepositorio.cs` | 20 min |

**Criterio de aceptación**: Logs estructurados visibles en consola en formato JSON. Kibana recibe eventos con los campos semánticos definidos. No hay credenciales en repositorio.

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
  "eventId": "ARCHIVO_UPLOAD_SUCCESS",
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
  "durationMs": 245,
  "errorCode": null
}
```

Para errores:

```json
{
  "level": "Error",
  "eventId": "ARCHIVO_UPLOAD_ERROR",
  "errorCode": "ARCHIVO_VACIO_0001",
  "message": "El stream del archivo está vacío",
  "exception": "System.ArgumentException",
  "stackTrace": "...",
  "durationMs": 12
}
```

### EventIds definidos

| EventId | Level | Operación |
|---------|-------|-----------|
| `ARCHIVO_UPLOAD_SUCCESS` | Info | Subida exitosa |
| `ARCHIVO_UPLOAD_ERROR` | Error | Error en subida |
| `ARCHIVO_DOWNLOAD_SUCCESS` | Info | Descarga exitosa |
| `ARCHIVO_DOWNLOAD_ERROR` | Error | Error en descarga |
| `ARCHIVO_READ_SUCCESS` | Info | Lectura de contenido exitosa |
| `ARCHIVO_READ_ERROR` | Error | Error en lectura |
| `ARCHIVO_DELETE_SUCCESS` | Info | Eliminación exitosa |
| `ARCHIVO_DELETE_ERROR` | Error | Error en eliminación |
| `ARCHIVO_METADATA_SUCCESS` | Info | Metadata obtenida |
| `ARCHIVO_METADATA_ERROR` | Error | Error al obtener metadata |
| `ARCHIVO_NOT_FOUND` | Warning | Archivo no encontrado |
| `PROVEEDOR_NO_DISPONIBLE` | Error | Proveedor inactivo o no encontrado |
| `STORAGE_PROVIDER_ERROR` | Error | Error en proveedor de almacenamiento |

---

## 4. Configuración de Serilog (ejemplo para `Program.cs`)

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(new JsonFormatter(renderMessage: true))
    .CreateLogger();

builder.Host.UseSerilog();
```

Y en `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Json.JsonFormatter"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
  }
}
```

---

## 5. Resumen de Esfuerzo

| Fase | Horas estimadas | Horas usadas | Estado |
|------|----------------|-------------|--------|
| Fase 1 — Limpieza | ~1 h | ~25 min | ✅ Completada |
| Fase 2 — Calidad | ~6 h | — | ⏳ Pendiente |
| Fase 3 — Observabilidad + Seguridad | ~5 h | — | ⏳ Pendiente |
| **Total** | **~12 h** | **~25 min** | 2/3 fases |

---

## 6. Checklist de Cumplimiento Post-Mitigación

- [x] ~~No hay credenciales en repositorio~~ ⏳ Pendiente (Fase 3)
- [x] ~~Logs estructurados visibles en Kibana con campos semánticos~~ ⏳ Pendiente (Fase 3)
- [x] ~~`dotnet build` sin warnings de documentación XML~~ ⏳ Pendiente (Fase 2) — temporalmente suprimido con `NoWarn=CS1591` en `Directory.Build.props`
- [x] ~~`dotnet test` pasa con cobertura de casos felices y de error~~ ⏳ Pendiente (Fase 2) — actualmente 2/2 tests (solo feliz)
- [x] ~~Códigos de error diferenciados por tipo de fallo~~ ⏳ Pendiente (Fase 2)
- [x] ~~`MidagriResponseFilter` emite log con `EventId` y contexto~~ ⏳ Pendiente (Fase 3)
- [x] ~~Validación de tamaño y tipo de archivo en upload~~ ⏳ Pendiente (Fase 2)
- [x] ~~`ConfigureAwait(false)` en todo código async de librerías~~ ⏳ Pendiente (Fase 3)
- [x] ✅ `.gitignore` presente y funcional
- [x] ✅ Sin archivos `Class1.cs` placeholder en Entidad, Datos, Negocio, Utils
- [x] ✅ `Directory.Build.props` centralizado con propiedades compartidas
- [x] ✅ `FtpStorageProvider` documentado como stub no productivo
- [x] ✅ Proyecto compila sin errores ni warnings
