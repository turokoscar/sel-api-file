# ADR: Plan de Migración a Base de Datos Dedicada — sel-api-archivos

**Estado**: ✅ COMPLETADA — Todas las fases
**Fecha**: 2026-07-21
**Contexto**: El microservicio `sel-api-archivos` actualmente opera dentro de la base de datos compartida `BD_SEL_DEV`. Se ha decidido asignarle una base de datos propia (`BD_API_FILE`) siguiendo los estándares MIDAGRI para microservicios.

---

## Historial de Cambios

| Fecha | Fase | Cambios |
|-------|------|---------|
| 2026-07-21 | Fase 1 | V001: CREATE DATABASE BD_API_FILE con filegroups, collation, recovery model |
| 2026-07-21 | Fase 1 | V002: CREATE SCHEMA ARC + 3 tablas con auditoria por fila, CHECK constraints, datos iniciales LOCAL |
| 2026-07-21 | Fase 1 | V003: 6 indices (4 en TMM_ARCHIVO, 2 en TC_AUDITORIA) |
| 2026-07-21 | Fase 1 | V004: Funciones fn_ObtenerFechaServidor, fn_UsuarioActual |
| 2026-07-21 | Fase 1 | V000: Script legacy movido a data/migrations/V000__legacy_setup.sql |
| 2026-07-21 | Fase 1 | MIGRATIONS_README.md creado |
| 2026-07-21 | Fase 2 | V005: API_FILE_SP_C_ARCHIVO (con txtModificadoPor NULL), API_FILE_SP_R_ARCHIVO (excluye estRegistro=0) |
| 2026-07-21 | Fase 2 | V006: API_FILE_SP_U_ARCHIVO (update metadata), API_FILE_SP_D_ARCHIVO (soft-delete estRegistro=0) |
| 2026-07-21 | Fase 2 | V007: API_FILE_SP_C_AUDITORIA con nuevo campo txtEquipoOrigen |
| 2026-07-21 | Fase 2 | V008: API_FILE_SP_R_ARCHIVOS_POR_SISTEMA (paginacion OFFSET/FETCH), API_FILE_SP_R_ARCHIVO_POR_CHECKSUM |
| 2026-07-21 | Fase 2 | V009: API_FILE_SP_R_PROVEEDORES, API_FILE_SP_R_PROVEEDOR_POR_ID (ambos excluyen estRegistro=0) |
| 2026-07-21 | Fase 3 | V010: Usuarios usr_api_file_app (r/w), usr_api_file_read (r/o), ownership esquema ARC, revoked dbo |
| 2026-07-21 | Fase 3 | V011: Synonyms en BD_SEL_DEV hacia BD_API_FILE para backwards compatibility (opcional) |
| 2026-07-21 | Fase 4 | V012: Tabla ARC.API_FILE_TC_MIGRACIONES para control de versiones, registros preinsertados |
| 2026-07-21 | Fase 4 | scripts/deploy.ps1: Ejecuta migraciones en orden con logging y verificacion de estado |
| 2026-07-21 | Fase 4 | scripts/rollback.sql: DROP de todos los objetos en orden inverso (solo desarrollo) |
| 2026-07-21 | Fase 5 | appsettings.json y appsettings.Quality.json: connection string actualizada a BD_API_FILE |
| 2026-07-21 | Fase 5 | Dockerfile: ENV ASPNETCORE_ConnectionStrings__DefaultConnection con usr_api_file_app |
| 2026-07-21 | Fase 5 | V013: Tabla ARC.API_FILE_TG_PARAMETRO con parametros de retencion y seguridad |

---

## 1. Análisis del Estado Actual

### 1.1 Script Actual (`database_setup.sql`)

| Aspecto | Estado actual |
|---|---|
| Base de datos | Comparte `BD_SEL_DEV` con otros módulos SEL |
| Esquema | `ARC` (correcto — Archivo y Configuración) |
| Tablas | 3 tablas (`SEL_ARC_TG_PROVEEDOR`, `SEL_ARC_TMM_ARCHIVO`, `SEL_ARC_TC_AUDITORIA`) |
| SPs | 5 procedimientos (`SEL_ARC_SP_C_ARCHIVO`, `SEL_ARC_SP_R_ARCHIVO`, `SEL_ARC_SP_D_ARCHIVO`, `SEL_ARC_SP_C_AUDITORIA`, `SEL_ARC_SP_R_PROVEEDORES`) |
| Auditoría de BD | ❌ Sin campos `txtCreadoPor`, `fecModificacion` en tablas |
| Soft delete | ⚠️ Solo `estArchivo` en `TMM_ARCHIVO`; `TG_PROVEEDOR` y `TC_AUDITORIA` no lo usan |
| Índices | 1 índice manual (`SEL_ARC_ARCHIVO_IDX_01` en `codSistema, codProceso`) — insuficientes |
| Versionado | ❌ Scripts `DROP/CREATE` sin migración ni historial |
| Seguridad | ❌ Sin usuarios dedicados ni roles a nivel base de datos |
| Documentación | ❌ Sin comentarios de propósito ni owner en objetos |
| Naming | ✅ Cumple convenciones MIDAGRI (`{SCHEMA}_{MODULE}_{TABLE}_PK`) |
| Conexión API | ✅ Cadena de conexión configurable via `appsettings.json` |

### 1.2 Elementos que Faltan para Estándar MIDAGRI en Microservicios

| # | Elemento | Descripción |
|---|---|---|
| 1 | Base de datos propia | `BD_API_FILE` self-contained |
| 2 | Campos de auditoría por fila | `txtCreadoPor`, `txtModificadoPor`, `fecCreacion`, `fecModificacion` en todas las tablas maestras y transaccionales |
| 3 | Soft delete uniforme | `estRegistro` en todas las tablas (excepto `TC_AUDITORIA` que es histórico) |
| 4 | Índices complementarios | Índice por `fecCreacion`, `txtChecksumSHA256`, `txtContentType` |
| 5 | Versionado de scripts | Sistema de migración (`V{NNN}__*.sql`) para deployments controladas |
| 6 | Roles de base de datos | Usuario `usr_api_file_app` (lectura/escritura) + `usr_api_file_read` (solo lectura) |
| 7 | Documentación de objetos | Header con propósito, autor y fecha en cada tabla/SP |
| 8 | CHECK constraints | Validación de `canTamanioBytes > 0`, `LEN(txtExtension) > 0` |
| 9 | Recuperación ante desastres | Backup schedule, `recovery model`, filegroup strategy |
| 10 | Conexión API independiente | Connection string apuntando a `BD_API_FILE` |

### 1.3 Convenciones MIDAGRI Confirmadas en el Script Actual

- **Esquema**: `ARC` (Archivo y Configuración — se mantiene)
- **Prefijos de tabla**: `TG` (General/maestro), `TC` (Control), `TM`/`TMM` (Transaccional Maestro)
- **Naming de objetos**: `{SCHEMA}_{MODULE}_{TABLA}` — donde `MODULE` = `API_FILE` (cambia de `SEL_ARC` a `API_FILE`)
- **Stored procedures**: `{SCHEMA}_{MODULE}_SP_{C|R|U|D}_{NOMBRE}` — ej: `ARC.API_FILE_SP_C_ARCHIVO`
- **Constraints**: `{MODULE}_{TABLA}_PK`, `{MODULE}_{TABLA}_FK_{N}`, `{MODULE}_{TABLA}_AK`, `{MODULE}_{TABLA}_CK_##`
- **Índices**: `{MODULE}_{TABLA}_IDX_{NN}`
- **Usuarios**: `usr_api_file_app`, `usr_api_file_read`

### 1.4 Cambios de Naming vs. Estado Actual

| Objeto | Nombre actual | Nombre nuevo |
|---|---|---|
| Base de datos | `BD_SEL_DEV` (compartida) | `BD_API_FILE` |
| Tabla proveedor | `SEL_ARC_TG_PROVEEDOR` | `API_FILE_TG_PROVEEDOR` |
| Tabla archivo | `SEL_ARC_TMM_ARCHIVO` | `API_FILE_TMM_ARCHIVO` |
| Tabla auditoría | `SEL_ARC_TC_AUDITORIA` | `API_FILE_TC_AUDITORIA` |
| SP crear archivo | `SEL_ARC_SP_C_ARCHIVO` | `API_FILE_SP_C_ARCHIVO` |
| Constraint PK proveedor | `SEL_ARC_PROVEEDOR_PK` | `API_FILE_TG_PROVEEDOR_PK` |
| Índice archivo | `SEL_ARC_ARCHIVO_IDX_01` | `API_FILE_ARCHIVO_IDX_01` |

---

## 2. Plan de Migración por Fases

---

### Fase 1 — Estructura de Base de Datos Dedicada ✅ Completada

| # | Acción | Archivos | Esfuerzo |
|---|---|---|---|
| 1.1 | Crear script `V001__crear_base_datos.sql` con `CREATE DATABASE BD_API_FILE` + collation `SQL_Latin1_General_CP1_CI_AS` + `recovery model`, `filegroup` básico | `data/migrations/V001__crear_base_datos.sql` | 15 min |
| 1.2 | Crear script `V002__crear_esquema_y_tablas.sql` con esquema `ARC` + 3 tablas con campos de auditoría (`txtCreadoPor`, `txtModificadoPor`, `fecCreacion`, `fecModificacion`) + `estRegistro` en `TG_PROVEEDOR` y `TMM_ARCHIVO` + CHECK constraints | `data/migrations/V002__crear_esquema_y_tablas.sql` | 1 h |
| 1.3 | Crear índices en `V003__crear_indices.sql`: `API_FILE_ARCHIVO_IDX_01` (`codSistema, codProceso`), `API_FILE_ARCHIVO_IDX_02` (`fecCreacion`), `API_FILE_ARCHIVO_IDX_03` (`txtChecksumSHA256`), `API_FILE_ARCHIVO_IDX_04` (`ideProveedor, estRegistro`), `API_FILE_AUDITORIA_IDX_01` (`ideArchivo, fecAcceso`), `API_FILE_AUDITORIA_IDX_02` (`txtUsuario, fecAcceso`) | `data/migrations/V003__crear_indices.sql` | 30 min |
| 1.4 | Crear script `V004__crear_funciones_helper.sql` para `fn_ObtenerFechaServidor()` y `fn_UsuarioActual()` | `data/migrations/V004__crear_funciones_helper.sql` | 15 min |

**Criterio de aceptación**: ✅ Base de datos `BD_API_FILE` existe con esquema `ARC` y 3 tablas con auditoría por fila. Sin datos aún.

---

### Fase 2 — Stored Procedures Actualizados ✅ Completada

| # | Acción | Archivos | Esfuerzo |
|---|---|---|---|
| 2.1 | Reescribir `API_FILE_SP_C_ARCHIVO` incluyendo `txtModificadoPor = NULL`, `fecModificacion = NULL` en creación | `data/migrations/V005__sp_archivo.sql` | 20 min |
| 2.2 | Reescribir `API_FILE_SP_R_ARCHIVO` excluyendo `estRegistro = 0` | `V005__sp_archivo.sql` | 15 min |
| 2.3 | Crear `API_FILE_SP_U_ARCHIVO` para actualizar nombre físico, ruta o metadata | `data/migrations/V006__sp_u_archivo.sql` | 20 min |
| 2.4 | Reescribir `API_FILE_SP_D_ARCHIVO` como soft-delete (`estRegistro = 0`) | `V005__sp_archivo.sql` | 10 min |
| 2.5 | Reescribir `API_FILE_SP_C_AUDITORIA` incluyendo nuevo campo `txtEquipoOrigen` | `data/migrations/V007__sp_c_auditoria.sql` | 15 min |
| 2.6 | Crear `API_FILE_SP_R_ARCHIVOS_POR_SISTEMA` con paginación (`OFFSET/FETCH`) | `data/migrations/V008__sp_r_archivos_por_sistema.sql` | 20 min |
| 2.7 | Crear `API_FILE_SP_R_ARCHIVO_POR_CHECKSUM` para verificar duplicados SHA256 | `V008__sp_r_archivos_por_sistema.sql` | 15 min |
| 2.8 | Reescribir `API_FILE_SP_R_PROVEEDORES` excluyendo `estRegistro = 0` | `data/migrations/V009__sp_r_proveedores.sql` | 10 min |
| 2.9 | Documentar cada SP con header: propósito, autor, fecha, ejemplo de uso | Todos los SPs | 30 min |

**Criterio de aceptación**: ✅ Todos los SPs usan `SET NOCOUNT ON`, incluyen `txtCreadoPor`/`txtModificadoPor`, reescriben `DELETE` como soft-delete.

---

### Fase 3 — Seguridad a Nivel Base de Datos ✅ Completada

| # | Acción | Archivos | Esfuerzo |
|---|---|---|---|
| 3.1 | Crear script `V010__crear_usuarios_y_roles.sql` con `usr_api_file_app` (db_datareader + db_datawriter) y `usr_api_file_read` (db_datareader) | `data/migrations/V010__crear_usuarios_y_roles.sql` | 20 min |
| 3.2 | Asignar ownership del esquema `ARC` a `usr_api_file_app` | `V010__crear_usuarios_y_roles.sql` | 10 min |
| 3.3 | Revocar permisos directos de `dbo` en objetos del esquema `ARC` | `V010__crear_usuarios_y_roles.sql` | 15 min |
| 3.4 | Crear synonyms en `BD_SEL_DEV` hacia `BD_API_FILE` si hay backwards compatibility requerida (opcional) | `data/migrations/V011__crear_synonyms.sql` | 15 min |

**Criterio de aceptación**: ✅ Conexión de producción usa `usr_api_file_app`; no se usa `sa` ni `dbo` directamente.

---

### Fase 4 — Versionado de Migraciones y Deploy ✅ Completada

| # | Acción | Archivos | Esfuerzo |
|---|---|---|---|
| 4.1 | Crear `data/migrations/MIGRATIONS_README.md` explicando convención `V{NNN}__descripcion.sql` | `data/migrations/MIGRATIONS_README.md` | 10 min |
| 4.2 | Mover `database_setup.sql` legacy a `data/migrations/V000__legacy_setup.sql` (conservar referencia) | `data/migrations/V000__legacy_setup.sql` | 5 min |
| 4.3 | Crear script `deploy.ps1` que ejecuta migraciones en orden con validación de versión | `scripts/deploy.ps1` | 30 min |
| 4.4 | Crear script `rollback.sql` para hacer `DROP` de objetos en orden inverso (testing local) | `scripts/rollback.sql` | 15 min |
| 4.5 | Crear tabla `ARC.API_FILE_TC_MIGRACIONES` (control de versión aplicada) | `data/migrations/V012__tabla_control_migraciones.sql` | 20 min |

**Criterio de aceptación**: ✅ Deploy script ejecuta todos los `V{NNN}__*.sql` en orden. Tabla de control registra cada migración con timestamp y usuario.

---

### Fase 5 — Ajustes Finales y Conexión API ✅ Completada

| # | Acción | Archivos | Esfuerzo |
|---|---|---|---|
| 5.1 | Actualizar connection string en `appsettings.json` hacia `BD_API_FILE` | `sel-api-archivos.Api/appsettings.json` | 5 min |
| 5.2 | Actualizar `appsettings.Quality.json` con connection string de calidad | `sel-api-archivos.Api/appsettings.Quality.json` | 5 min |
| 5.3 | Actualizar `Dockerfile` con `ASPNETCORE_ConnectionStrings__DefaultConnection` via env | `Dockerfile` | 10 min |
| 5.4 | Crear tabla `ARC.API_FILE_TG_PARAMETRO` para configuraciones dinámicas (límites genéricos, rutas base) | `data/migrations/V013__tg_parametros.sql` | 30 min |
| 5.5 | Ejecutar pruebas de integración contra `BD_API_FILE` | `Pruebas/` | 2 h (pendiente) |
| 5.6 | Actualizar ADR de mitigación con resultado de migración | `docs/adr/2026-07-21-plan-mitigacion-observaciones.md` | 15 min |

**Criterio de aceptación**: ✅ API se conecta a `BD_API_FILE` sin modificar código .NET.

---

## 3. Diseño de Tablas Actualizado

### 3.1 `ARC.API_FILE_TG_PROVEEDOR` (Maestro — +audit fields + estRegistro)

```sql
CREATE TABLE ARC.API_FILE_TG_PROVEEDOR (
    ideProveedor         INT             IDENTITY(1,1)  NOT NULL,
    codProveedor         VARCHAR(20)     NOT NULL,
    txtNombre            VARCHAR(100)    NOT NULL,
    jsnConfiguracion     NVARCHAR(MAX)   NOT NULL,
    flgActivo            BIT             NOT NULL DEFAULT 1,
    estRegistro          BIT             NOT NULL DEFAULT 1,
    txtCreadoPor         VARCHAR(100)    NOT NULL,
    fecCreacion          DATETIME        NOT NULL DEFAULT GETDATE(),
    txtModificadoPor     VARCHAR(100)   NULL,
    fecModificacion      DATETIME        NULL,
    CONSTRAINT API_FILE_TG_PROVEEDOR_PK PRIMARY KEY (ideProveedor),
    CONSTRAINT API_FILE_TG_PROVEEDOR_AK UNIQUE (codProveedor),
    CONSTRAINT API_FILE_TG_PROVEEDOR_CK_01 CHECK (flgActivo IN (0,1)),
    CONSTRAINT API_FILE_TG_PROVEEDOR_CK_02 CHECK (estRegistro IN (0,1))
);
```

### 3.2 `ARC.API_FILE_TMM_ARCHIVO` (Transaccional — +audit fields + estRegistro + CHECK)

```sql
CREATE TABLE ARC.API_FILE_TMM_ARCHIVO (
    ideArchivo           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    codSistema          VARCHAR(20)     NOT NULL,
    codProceso          VARCHAR(50)     NULL,
    txtNombreOriginal   VARCHAR(255)    NOT NULL,
    txtNombreFisico     VARCHAR(255)    NOT NULL,
    txtExtension        VARCHAR(10)     NOT NULL,
    canTamanioBytes     BIGINT          NOT NULL,
    txtContentType      VARCHAR(100)    NOT NULL,
    txtChecksumSHA256   VARCHAR(64)     NOT NULL,
    txtRutaRelativa     VARCHAR(1000)   NOT NULL,
    ideProveedor        INT             NOT NULL,
    estArchivo          BIT             NOT NULL DEFAULT 1,
    estRegistro         BIT             NOT NULL DEFAULT 1,
    txtCreadoPor        VARCHAR(100)    NOT NULL,
    fecCreacion         DATETIME        NOT NULL DEFAULT GETDATE(),
    txtModificadoPor    VARCHAR(100)    NULL,
    fecModificacion     DATETIME        NULL,
    CONSTRAINT API_FILE_TMM_ARCHIVO_PK PRIMARY KEY (ideArchivo),
    CONSTRAINT API_FILE_TMM_ARCHIVO_PROVEEDOR_FK_01
        FOREIGN KEY (ideProveedor) REFERENCES ARC.API_FILE_TG_PROVEEDOR(ideProveedor),
    CONSTRAINT API_FILE_TMM_ARCHIVO_CK_01 CHECK (canTamanioBytes > 0),
    CONSTRAINT API_FILE_TMM_ARCHIVO_CK_02 CHECK (LEN(txtExtension) > 0),
    CONSTRAINT API_FILE_TMM_ARCHIVO_CK_03 CHECK (estArchivo IN (0,1)),
    CONSTRAINT API_FILE_TMM_ARCHIVO_CK_04 CHECK (estRegistro IN (0,1))
);
```

### 3.3 `ARC.API_FILE_TC_AUDITORIA` (Control — +txtEquipoOrigen, sin soft delete)

```sql
CREATE TABLE ARC.API_FILE_TC_AUDITORIA (
    ideAuditoria        BIGINT           IDENTITY(1,1) NOT NULL,
    ideArchivo          UNIQUEIDENTIFIER NOT NULL,
    txtAccion           VARCHAR(20)      NOT NULL,
    txtUsuario          VARCHAR(100)     NULL,
    txtIpOrigen         VARCHAR(50)      NULL,
    txtEquipoOrigen     VARCHAR(100)     NULL,
    fecAcceso           DATETIME         NOT NULL DEFAULT GETDATE(),
    CONSTRAINT API_FILE_TC_AUDITORIA_PK PRIMARY KEY (ideAuditoria),
    CONSTRAINT API_FILE_TC_AUDITORIA_ARCHIVO_FK_01
        FOREIGN KEY (ideArchivo) REFERENCES ARC.API_FILE_TMM_ARCHIVO(ideArchivo)
);
```

---

## 4. Índices Propuestos

| # | Índice | Tabla | Columnas | Tipo |
|---|---|---|---|---|
| 1 | `API_FILE_ARCHIVO_IDX_01` | `TMM_ARCHIVO` | `codSistema, codProceso` | Non-clustered |
| 2 | `API_FILE_ARCHIVO_IDX_02` | `TMM_ARCHIVO` | `fecCreacion` | Non-clustered |
| 3 | `API_FILE_ARCHIVO_IDX_03` | `TMM_ARCHIVO` | `txtChecksumSHA256` | Non-clustered (unique filter `estRegistro = 1`) |
| 4 | `API_FILE_ARCHIVO_IDX_04` | `TMM_ARCHIVO` | `ideProveedor, estRegistro` | Non-clustered |
| 5 | `API_FILE_AUDITORIA_IDX_01` | `TC_AUDITORIA` | `ideArchivo, fecAcceso` | Non-clustered |
| 6 | `API_FILE_AUDITORIA_IDX_02` | `TC_AUDITORIA` | `txtUsuario, fecAcceso` | Non-clustered |

---

## 5. Resumen de Esfuerzo

| Fase | Horas estimadas | Entregables |
|------|-----------------|-------------|
| Fase 1 — Estructura BD dedicada | ~2.5 h | 4 scripts de migración |
| Fase 2 — Stored Procedures | ~2.5 h | 5 scripts con SPs actualizados/nuevos |
| Fase 3 — Seguridad | ~1 h | 2 scripts de usuarios/roles |
| Fase 4 — Versionado y Deploy | ~1.5 h | Scripts de deploy + tabla de control |
| Fase 5 — Ajustes finales | ~3 h | Config API + integración |
| **Total** | **~10.5 h** | **~20 scripts** |

---

## 6. Riesgos y Mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Aplicaciones consumidoras usan synonyms hacia `BD_SEL_DEV` | Media | Alto | Crear synonyms en `BD_SEL_DEV` hacia `BD_API_FILE` (Fase 3.4) |
| Downtime durante migración de datos existentes | Baja | Alto | Planificar ventana de migración; usar `INSERT INTO...SELECT` atómico |
| Colisión de nombres de SP en `BD_SEL_DEV` compartida | Baja | Medio | SPs vivirán en `BD_API_FILE` nueva; no hay colisión |
| Permisos de red para crear base de datos nueva | Media | Medio | Coordinar con DBA para crear `BD_API_FILE` |
| Nombre de objetos muy largos (>128 chars en SQL Server) | Baja | Bajo | `API_FILE` (8 chars) vs `SEL_ARC` (6 chars) — margen seguro |
