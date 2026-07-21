# ADR: Plan de Migración a Base de Datos Dedicada — sel-api-archivos

**Estado**: Propuesto
**Fecha**: 2026-07-21
**Contexto**: El microservicio `sel-api-archivos` actualmente opera dentro de la base de datos compartida `BD_SEL_DEV`. Se ha decidido asignarle una base de datos propia (`BD_SEL_ARC`) siguiendo los estándares MIDAGRI para microservicios.

---

## 1. Análisis del Estado Actual

### 1.1 Script Actual (`database_setup.sql`)

| Aspecto | Estado actual |
|---|---|
| Base de datos | Comparte `BD_SEL_DEV` con otros módulos SEL |
| Esquema | `ARC` (correcto) |
| Tablas | 3 tablas (`TG_PROVEEDOR`, `TMM_ARCHIVO`, `TC_AUDITORIA`) |
| SPs | 5 procedimientos (C/R/D archivo, C auditoría, R proveedores) |
| Auditoría de BD | ❌ Sin campos `txtCreadoPor`, `fecModificacion` en tablas |
| Soft delete | ⚠️ Solo `estArchivo` en `TMM_ARCHIVO`; `TG_PROVEEDOR` y `TC_AUDITORIA` no lo usan |
| Índices | 1 índice manual (`codSistema, codProceso`) — insuficientes |
| Versionado | ❌ Scripts `DROP/CREATE` sin migración ni historial |
| Seguridad | ❌ Sin usuarios dedicados ni roles a nivel base de datos |
| Documentación | ❌ Sin comentarios de propósito ni owner en objetos |
| Naming | ✅ Cumple convenciones MIDAGRI existentes |
| Conexión API | ✅ Cadena de conexión configurable via `appsettings.json` |

### 1.2 Elementos que Faltan para Estándar MIDAGRI en Microservicios

| # | Elemento | Descripción |
|---|---|---|
| 1 | Base de datos propia | `BD_SEL_ARC` self-contained |
| 2 | Campos de auditoría por fila | `txtCreadoPor`, `txtModificadoPor`, `fecCreacion`, `fecModificacion` en todas las tablas |
| 3 | Soft delete uniforme | `estRegistro` (o `flgActivo`) en todas las tablas, no solo `TMM_ARCHIVO` |
| 4 | Índices complementarios | Índice por `fecCreacion`, `txtChecksumSHA256`, `txtContentType` |
| 5 | Versionado de scripts | Sistema de migración (V1__, V2__, etc.) para deployments controladas |
| 6 | Roles de base de datos | Usuario `usr_sel_arc_app` (lectura/escritura) + `usr_sel_arc_read` (solo lectura) |
| 7 | Documentación de objetos | `<summary>` en cada tabla/SP, owner y fecha en headers |
| 8 | Validación de extensiones | `CHECK` constraints en `txtExtension` y `txtContentType` |
| 9 | Recuperación ante desastres |-backup schedule, `recovery model`, filegroup strategy |
| 10 | Conexión API independiente | Connection string apuntando a `BD_SEL_ARC` |

### 1.3 Convenciones MIDAGRI Confirmadas en el Script Actual

- **Prefijos de tabla**: `TG` (General/maestro), `TC` (Control), `TM`/`TMM` (Transaccional Maestro)
- **Prefijos de columna**: `ide` (ID), `cod` (código), `txt` (texto/descripción), `flg` (flag binario), `fec` (fecha), `can` (cantidad), `jsn` (JSON), `est` (estado), `bin` (binario)
- **Stored procedures**: `{SCHEMA}_SP_{C|R|U|D}_{NOMBRE}` — Create, Read, Update, Delete
- **Constraints**: `{SCHEMA}_{TABLA}_PK`, `{SCHEMA}_{TABLA}_FK_{N}`, `{SCHEMA}_{TABLA}_AK`, `{SCHEMA}_{TABLA}_CK`
- **Índices**: `{SCHEMA}_{TABLA}_IDX_{NN}`
- **Esquema**: código de 3 letras del módulo (`ARC`)

---

## 2. Plan de Migración por Fases

---

### Fase 1 — Estructura de Base de Datos Dedicada ✅ Propuesta

| # | Acción | Archivos | Esfuerzo |
|---|---|---|---|
| 1.1 | Crear script `V001__crear_base_datos.sql` con `CREATE DATABASE BD_SEL_ARC` + collation `SQL_Latin1_General_CP1_CI_AS` | `data/migrations/V001__crear_base_datos.sql` | 15 min |
| 1.2 | Crear script `V002__crear_esquema_y_tablas.sql` con las 3 tablas + campos de auditoría por fila (`txtCreadoPor`, `txtModificadoPor`, `fecCreacion`, `fecModificacion`, `estRegistro`) + CHECK constraints | `data/migrations/V002__crear_esquema_y_tablas.sql` | 1 h |
| 1.3 | Agregar `estRegistro` a `TG_PROVEEDOR` y `TC_AUDITORIA` (no aplica en auditoría de control, pero sí en maestro) | `V002__crear_esquema_y_tablas.sql` | 15 min |
| 1.4 | Agregar índices faltantes: `TMM_ARCHIVO(fecCreacion)`, `TMM_ARCHIVO(txtChecksumSHA256)`, `TMM_ARCHIVO(txtContentType)`, `TC_AUDITORIA(fecAcceso)` | `data/migrations/V003__crear_indices.sql` | 30 min |
| 1.5 | Crear script `V004__crear_funciones_helper.sql` (fn_ObtenerFechaServidor, fn_UsuarioActual) | `data/migrations/V004__crear_funciones_helper.sql` | 15 min |

**Criterio de aceptación**: Base de datos `BD_SEL_ARC` existe con esquema `ARC` y 3 tablas con campos de auditoría correctos. Sin datos aún.

---

### Fase 2 — Stored Procedures Actualizados ⏳ Pendiente

| # | Acción | Archivos | Esfuerzo |
|---|---|---|---|
| 2.1 | Reescribir `SEL_ARC_SP_C_ARCHIVO` incluyendo `txtModificadoPor` y `fecModificacion` (NULL en creación) | `data/migrations/V005__sp_c_archivo.sql` | 20 min |
| 2.2 | Reescribir `SEL_ARC_SP_R_ARCHIVO` para excluir registros con `estRegistro = 0` (soft delete) | `V005__sp_c_archivo.sql` | 15 min |
| 2.3 | Agregar `SEL_ARC_SP_U_ARCHIVO` (Update) para renombrar o reubicar archivo | `data/migrations/V006__sp_u_archivo.sql` | 20 min |
| 2.4 | Reescribir `SEL_ARC_SP_D_ARCHIVO` como soft-delete (`estRegistro = 0`) | `V005__sp_c_archivo.sql` | 10 min |
| 2.5 | Reescribir `SEL_ARC_SP_C_AUDITORIA` para incluir `txtEquipoOrigen` (nuevo campo) | `data/migrations/V007__sp_c_auditoria.sql` | 15 min |
| 2.6 | Crear `SEL_ARC_SP_R_ARCHIVOS_POR_SISTEMA` para listar archivos por `codSistema` con paginación | `data/migrations/V008__sp_r_archivos_por_sistema.sql` | 20 min |
| 2.7 | Crear `SEL_ARC_SP_R_ARCHIVO_POR_CHECKSUM` para verificar duplicados por SHA256 | `data/migrations/V008__sp_r_archivos_por_sistema.sql` | 15 min |
| 2.8 | Agregar documentación XML-style en cada SP (`-- ========================================`, `-- PURPOSE:`, `-- AUTHOR:`, `-- DATE:`) | Todos los SPs | 30 min |

**Criterio de aceptación**: Todos los SPs usan `SET NOCOUNT ON`, incluyen `txtCreadoPor`/`txtModificadoPor`, reescriben `DELETE` como soft-delete.

---

### Fase 3 — Seguridad a Nivel Base de Datos ⏳ Pendiente

| # | Acción | Archivos | Esfuerzo |
|---|---|---|---|
| 3.1 | Crear script `V009__crear_usuarios_y_roles.sql` con: `usr_sel_arc_app` (db_datareader + db_datawriter), `usr_sel_arc_read` (db_datareader) | `data/migrations/V009__crear_usuarios_y_roles.sql` | 20 min |
| 3.2 | Asignar ownership del esquema `ARC` a `usr_sel_arc_app` | `V009__crear_usuarios_y_roles.sql` | 10 min |
| 3.3 | Revocar permisos directos de `dbo` en objetos del esquema `ARC` | `V009__crear_usuarios_y_roles.sql` | 15 min |
| 3.4 | Crear script `V010__crear_synonyms.sql` si APIs consumidoras acceden desde `BD_SEL_SEL` usando synonyms (opcional, para backwards compatibility) | `data/migrations/V010__crear_synonyms.sql` | 15 min |

**Criterio de aceptación**: Conexión de producción usa `usr_sel_arc_app`; no se usa `sa` ni `dbo` directamente.

---

### Fase 4 — Versionado de Migraciones y Deploy ⏳ Pendiente

| # | Acción | Archivos | Esfuerzo |
|---|---|---|---|
| 4.1 | Crear script `MIGRATIONS_README.md` explicando convención `V{NNN}__descripcion.sql` | `data/migrations/MIGRATIONS_README.md` | 10 min |
| 4.2 | Renombrar `database_setup.sql` legacy a `V000__legacy_setup.sql` (conservar para referencia) | `data/database_setup.sql` → `data/migrations/V000__legacy_setup.sql` | 5 min |
| 4.3 | Crear script `deploy.sh` / `deploy.ps1` que ejecuta migraciones en orden | `scripts/deploy.sh` | 30 min |
| 4.4 | Crear script `rollback.sql` para hacer `DROP` de objetos en orden inverso (para testing local) | `scripts/rollback.sql` | 15 min |
| 4.5 | Agregar tabla de control `ARC.SEL_ARC_TC_MIGRACIONES` para tracking de versiones aplicadas | `data/migrations/V011__tabla_control_migraciones.sql` | 20 min |

**Criterio de aceptación**: Deploy script ejecuta todos los archivos `V{NNN}__*.sql` en orden. Tabla de control registra cada migración aplicada con timestamp.

---

### Fase 5 — Ajustes Finales y Conexión API ⏳ Pendiente

| # | Acción | Archivos | Esfuerzo |
|---|---|---|---|
| 5.1 | Actualizar `appsettings.json` con connection string a `BD_SEL_ARC` (remover referencia a `BD_SEL_DEV`) | `sel-api-archivos.Api/appsettings.json` | 5 min |
| 5.2 | Actualizar `appsettings.Quality.json` con connection string de calidad | `sel-api-archivos.Api/appsettings.Quality.json` | 5 min |
| 5.3 | Actualizar `Dockerfile` si la cadena de conexión se pasa via environment variable | `Dockerfile` | 10 min |
| 5.4 | Agregar tabla `ARC.SEL_ARC_TG_PARAMETRO` para configuraciones dinámicas (ej: rutas, límites genéricos) | `data/migrations/V012__tg_parametros.sql` | 30 min |
| 5.5 | Ejecutar pruebas de integración contra `BD_SEL_ARC` (验证) | `Pruebas/` (nuevo proyecto integration) | 2 h |
| 5.6 | Actualizar ADR con resultado de migración | `docs/adr/2026-07-21-plan-mitigacion-observaciones.md` | 15 min |

**Criterio de aceptación**: API se conecta a `BD_SEL_ARC` sin modificar código .NET. Todos los tests de integración pasan.

---

## 3. Diseño de Tablas Actualizado

### 3.1 `ARC.SEL_ARC_TG_PROVEEDOR` (Maestro — sin cambios estructurales, +audit)

```sql
CREATE TABLE ARC.SEL_ARC_TG_PROVEEDOR (
    ideProveedor       INT           IDENTITY(1,1)  NOT NULL,
    codProveedor       VARCHAR(20)   NOT NULL,
    txtNombre          VARCHAR(100)  NOT NULL,
    jsnConfiguracion   NVARCHAR(MAX) NOT NULL,
    flgActivo          BIT           NOT NULL DEFAULT 1,
    estRegistro        BIT           NOT NULL DEFAULT 1,  -- NUEVO
    txtCreadoPor       VARCHAR(100)  NOT NULL,
    fecCreacion        DATETIME      NOT NULL DEFAULT GETDATE(),
    txtModificadoPor   VARCHAR(100)  NULL,
    fecModificacion    DATETIME      NULL,                -- NUEVO
    CONSTRAINT SEL_ARC_PROVEEDOR_PK PRIMARY KEY (ideProveedor),
    CONSTRAINT SEL_ARC_PROVEEDOR_AK UNIQUE (codProveedor)
);
```

### 3.2 `ARC.SEL_ARC_TMM_ARCHIVO` (Transaccional — +audit fields)

```sql
CREATE TABLE ARC.SEL_ARC_TMM_ARCHIVO (
    ideArchivo         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    codSistema         VARCHAR(20)     NOT NULL,
    codProceso         VARCHAR(50)     NULL,
    txtNombreOriginal  VARCHAR(255)    NOT NULL,
    txtNombreFisico    VARCHAR(255)    NOT NULL,
    txtExtension       VARCHAR(10)     NOT NULL,
    canTamanioBytes    BIGINT          NOT NULL,
    txtContentType      VARCHAR(100)    NOT NULL,
    txtChecksumSHA256   VARCHAR(64)     NOT NULL,
    txtRutaRelativa     VARCHAR(1000)  NOT NULL,
    ideProveedor        INT            NOT NULL,
    estArchivo          BIT            NOT NULL DEFAULT 1,
    estRegistro         BIT            NOT NULL DEFAULT 1,  -- NUEVO
    txtCreadoPor        VARCHAR(100)   NOT NULL,
    fecCreacion         DATETIME       NOT NULL DEFAULT GETDATE(),
    txtModificadoPor    VARCHAR(100)   NULL,
    fecModificacion     DATETIME       NULL,
    CONSTRAINT SEL_ARC_ARCHIVO_PK PRIMARY KEY (ideArchivo),
    CONSTRAINT SEL_ARC_ARCHIVO_PROVEEDOR_FK_01
        FOREIGN KEY (ideProveedor) REFERENCES ARC.SEL_ARC_TG_PROVEEDOR(ideProveedor),
    CONSTRAINT SEL_ARC_ARCHIVO_CK_01 CHECK (canTamanioBytes > 0),
    CONSTRAINT SEL_ARC_ARCHIVO_CK_02 CHECK (LEN(txtExtension) > 0)
);
```

### 3.3 `ARC.SEL_ARC_TC_AUDITORIA` (Control — sin soft delete, +txtEquipoOrigen)

```sql
CREATE TABLE ARC.SEL_ARC_TC_AUDITORIA (
    ideAuditoria       BIGINT         IDENTITY(1,1) NOT NULL,
    ideArchivo         UNIQUEIDENTIFIER NOT NULL,
    txtAccion          VARCHAR(20)    NOT NULL,
    txtUsuario         VARCHAR(100)   NULL,
    txtIpOrigen        VARCHAR(50)    NULL,
    txtEquipoOrigen    VARCHAR(100)   NULL,   -- NUEVO: nombre del host
    fecAcceso          DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT SEL_ARC_AUDITORIA_PK PRIMARY KEY (ideAuditoria),
    CONSTRAINT SEL_ARC_AUDITORIA_ARCHIVO_FK_01
        FOREIGN KEY (ideArchivo) REFERENCES ARC.SEL_ARC_TMM_ARCHIVO(ideArchivo)
);
```

---

## 4. Índices Propuestos

| # | Índice | Tabla | Columnas | Tipo |
|---|---|---|---|---|
| 1 | `SEL_ARC_ARCHIVO_IDX_01` | `TMM_ARCHIVO` | `codSistema, codProceso` | Non-clustered |
| 2 | `SEL_ARC_ARCHIVO_IDX_02` | `TMM_ARCHIVO` | `fecCreacion` | Non-clustered |
| 3 | `SEL_ARC_ARCHIVO_IDX_03` | `TMM_ARCHIVO` | `txtChecksumSHA256` | Non-clustered (unique filter) |
| 4 | `SEL_ARC_ARCHIVO_IDX_04` | `TMM_ARCHIVO` | `ideProveedor, estArchivo` | Non-clustered |
| 5 | `SEL_ARC_AUDITORIA_IDX_01` | `TC_AUDITORIA` | `ideArchivo, fecAcceso` | Non-clustered |
| 6 | `SEL_ARC_AUDITORIA_IDX_02` | `TC_AUDITORIA` | `txtUsuario, fecAcceso` | Non-clustered |

---

## 5. Resumen de Esfuerzo

| Fase | Horas estimadas | Entregables |
|------|-----------------|-------------|
| Fase 1 — Estructura BD dedicada | ~2.5 h | 5 scripts de migración |
| Fase 2 — Stored Procedures | ~2.5 h | 4 scripts con SPs actualizados/nuevos |
| Fase 3 — Seguridad | ~1 h | 2 scripts de usuarios/roles |
| Fase 4 — Versionado y Deploy | ~1.5 h | Scripts de deploy + tabla de control |
| Fase 5 — Ajustes finales | ~3 h | Config API + integración |
| **Total** | **~10.5 h** | **~20 scripts** |

---

## 6. Riesgos y Mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Aplicaciones consumidoras usan synonyms hacia `BD_SEL_DEV` | Media | Alto | Crear synonyms en `BD_SEL_SEL` hacia `BD_SEL_ARC` (Fase 3.4) |
| Downtime durante migración de datos existentes | Baja | Alto | Planificar ventana de migración; usar `INSERT INTO...SELECT` con transacción |
| Colisión de nombres de stored procedures en `BD_SEL_DEV` compartida | Baja | Medio | SPs vivirán en `BD_SEL_ARC` nueva; no hay colisión |
| Permisos de red para crear base de datos nueva | Media | Medio | Coordinar con DBA para crear `BD_SEL_ARC`; el script de migrate asume que la BD ya existe |
