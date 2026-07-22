# Migraciones de Base de Datos — BD_API_FILE

## Objetivo

Este directorio contiene los scripts de migracion para crear y mantener la base de datos `BD_API_FILE` del microservicio `sel-api-archivos`.

## Convenciones

### Versionado

Los scripts siguen el formato `V{NNN}__descripcion.sql`:

| Prefijo | Significado |
|---------|-------------|
| `V000` | Script legacy original (antes de migracion a versionado) |
| `V001` – `V999` | Migraciones numeradas secuencialmente |

### Orden de Ejecucion

Ejecutar en orden numerico creciente. Cada script debe ser idempotente (puede ejecutarse varias veces sin errores).

### Dependencias

Las dependencias se documentan en el header de cada script:

```sql
-- Dependencias: V001__crear_base_datos.sql
```

## Lista de Migraciones

| # | Script | Proposito | Dependencias |
|---|--------|-----------|-------------|
| V000 | `V000__legacy_setup.sql` | Script original sin versionar (referencia) | Ninguna |
| V001 | `V001__crear_base_datos.sql` | Crear base de datos `BD_API_FILE` + filegroups | Ninguna |
| V002 | `V002__crear_esquema_y_tablas.sql` | Crear esquema `ARC` + 3 tablas + datos iniciales | V001 |
| V003 | `V003__crear_indices.sql` | Crear 6 indices complementarios | V002 |
| V004 | `V004__crear_funciones_helper.sql` | Crear funciones `fn_ObtenerFechaServidor`, `fn_UsuarioActual` | V002 |
| V005 | `V005__sp_archivo.sql` | `SP_C_ARCHIVO`, `SP_R_ARCHIVO` | V002 |
| V006 | `V006__sp_u_d_archivo.sql` | `SP_U_ARCHIVO`, `SP_D_ARCHIVO` (soft-delete) | V005 |
| V007 | `V007__sp_c_auditoria.sql` | `SP_C_AUDITORIA` con `txtEquipoOrigen` | V002 |
| V008 | `V008__sp_read_adicionales.sql` | `SP_R_ARCHIVOS_POR_SISTEMA` (paginado), `SP_R_ARCHIVO_POR_CHECKSUM` | V005 |
| V009 | `V009__sp_r_proveedores.sql` | `SP_R_PROVEEDORES`, `SP_R_PROVEEDOR_POR_ID` | V002 |
| V010 | `V010__crear_usuarios_y_roles.sql` | Usuarios `usr_api_file_app`, `usr_api_file_read` + permisos | V002 |
| V011 | `V011__crear_synonyms.sql` | Synonyms en `BD_SEL_DEV` hacia `BD_API_FILE` (opcional) | V002-V010 |
| V012 | `V012__tabla_control_migraciones.sql` | Tabla `TC_MIGRACIONES` + registros de migraciones aplicadas | V002 |
| V013 | `V013__tg_parametros.sql` | Tabla `TG_PARAMETRO` con parametros de retencion y seguridad | V002 |

## Ejecucion

### En desarrollo (SQL Server Management Studio)

Ejecutar en orden con `:r` o copiando el contenido:

```
V001 → V002 → V003 → V004 → V005 → V006 → V007 → V008 → V009 → V010 → V012 → V013
```

`V011` es opcional y debe ejecutarse en `BD_SEL_DEV`.

### Con PowerShell (recomendado)

```powershell
.\scripts\deploy.ps1 -Server localhost -Username sa -Password "MiPassword123!"
```

Parametros disponibles:
- `-Server`: Servidor SQL (default: localhost)
- `-Database`: Base de datos (default: BD_API_FILE)
- `-Username`: Usuario SQL (default: autenticacion de Windows)
- `-Password`: Contrasena (requerido si -Username)
- `-DataPath`: Ruta para archivos MDF
- `-LogPath`: Ruta para archivos LDF

### En produccion (Docker / CI-CD)

```bash
# 1. Crear base de datos
sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -d master -i "data/migrations/V001__crear_base_datos.sql"

# 2. Ejecutar migraciones
for f in data/migrations/V00*.sql data/migrations/V01*.sql; do
    [ "${f}" = "data/migrations/V011__crear_synonyms.sql" ] && continue  # Saltar synonyms (ejecutar en BD_SEL_DEV)
    sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -d BD_API_FILE -i "$f"
done
```

## Parametros de Configuracion

| Parametro | Valor por defecto | Descripcion |
|-----------|-------------------|-------------|
| `$(DATA_PATH)` | `$(DEFAULT_DATA_PATH)` | Ruta para archivos .mdf |
| `$(LOG_PATH)` | `$(DEFAULT_LOG_PATH)` | Ruta para archivos .ldf |

## Objetos Creados

### Tablas

| Tabla | Tipo | Descripcion |
|-------|------|-------------|
| `ARC.API_FILE_TG_PROVEEDOR` | Maestro | Proveedores de almacenamiento |
| `ARC.API_FILE_TMM_ARCHIVO` | Transaccional | Metadata de archivos |
| `ARC.API_FILE_TC_AUDITORIA` | Control | Log de auditoria de accesos |
| `ARC.API_FILE_TC_MIGRACIONES` | Control | Control de versiones de migracion |
| `ARC.API_FILE_TG_PARAMETRO` | Maestro | Parametros generales (retencion, seguridad) |

### Funciones

| Funcion | Descripcion |
|---------|-------------|
| `ARC.fn_ObtenerFechaServidor()` | Retorna GETDATE() normalizado |
| `ARC.fn_UsuarioActual()` | Retorna el login de SQL actual |

### Stored Procedures

| SP | Descripcion |
|----|-------------|
| `ARC.API_FILE_SP_C_ARCHIVO` | Registrar nuevo archivo |
| `ARC.API_FILE_SP_R_ARCHIVO` | Obtener metadata por IDE |
| `ARC.API_FILE_SP_U_ARCHIVO` | Actualizar metadata (nombre fisico, ruta) |
| `ARC.API_FILE_SP_D_ARCHIVO` | Eliminacion logica (soft-delete) |
| `ARC.API_FILE_SP_C_AUDITORIA` | Registrar accion de auditoria |
| `ARC.API_FILE_SP_R_ARCHIVOS_POR_SISTEMA` | Listar archivos con paginacion |
| `ARC.API_FILE_SP_R_ARCHIVO_POR_CHECKSUM` | Buscar archivo por SHA256 |
| `ARC.API_FILE_SP_R_PROVEEDORES` | Listar proveedores activos |
| `ARC.API_FILE_SP_R_PROVEEDOR_POR_ID` | Obtener proveedor por IDE |

### Indices

| Indice | Tabla | Proposito |
|--------|-------|-----------|
| `API_FILE_ARCHIVO_IDX_01` | TMM_ARCHIVO | Busqueda por sistema/proceso |
| `API_FILE_ARCHIVO_IDX_02` | TMM_ARCHIVO | Consultas por fecha |
| `API_FILE_ARCHIVO_IDX_03` | TMM_ARCHIVO | Verificacion SHA256 (unique filter) |
| `API_FILE_ARCHIVO_IDX_04` | TMM_ARCHIVO | Archivos por proveedor |
| `API_FILE_AUDITORIA_IDX_01` | TC_AUDITORIA | Auditoria por archivo |
| `API_FILE_AUDITORIA_IDX_02` | TC_AUDITORIA | Auditoria por usuario |

### Usuarios de Base de Datos

| Usuario | Roles | Proposito |
|---------|-------|-----------|
| `usr_api_file_app` | db_datareader, db_datawriter | Aplicacion (lectura + escritura) |
| `usr_api_file_read` | db_datareader | Lectura unicamente (reportes, auditoria) |

## Notas

- La tabla `TC_AUDITORIA` es un historico y no lleva soft-delete ni `estRegistro`.
- Los campos de auditoria (`txtCreadoPor`, `txtModificadoPor`, `fecCreacion`, `fecModificacion`) son obligatorios en todas las tablas maestras y transaccionales.
- El campo `jsnConfiguracion` en `TG_PROVEEDOR` almacena JSON: `localStoragePath`, `maxFileSizeBytes`, `allowedContentTypes`.
- Todos los SPs usan `SET NOCOUNT ON`.
- `SP_D_ARCHIVO` hace soft-delete (`estRegistro = 0, estArchivo = 0`), no DELETE fisico.
- `V011__crear_synonyms.sql` es **opcional** — solo ejecutar en `BD_SEL_DEV` si hay aplicaciones que no pueden actualizar sus connection strings inmediatamente.
- Para hacer rollback en desarrollo: ejecutar `scripts/rollback.sql`.
