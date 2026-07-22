-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V013__tg_parametros.sql
-- Proposito: Crear tabla de parametros generales para configuraciones
--            dinamicas del modulo de archivos.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V002__crear_esquema_y_tablas.sql
-- -------------------------------------------------------------------------
-- DESCRIPCION:
--   Tabla para almacenar configuraciones dinamicas del modulo de archivos,
--   como rutas base, limites genericos, politicas de retencion, etc.
--   Evitando hardcodificar valores en la logica de negocio.
-- =========================================================================

USE BD_API_FILE;
GO

-- =========================================================================
-- Tabla de Parametros
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_TG_PARAMETRO', 'U') IS NOT NULL
BEGIN
    DROP TABLE ARC.API_FILE_TG_PARAMETRO;
END;
GO

CREATE TABLE ARC.API_FILE_TG_PARAMETRO (
    ideParametro       INT             IDENTITY(1,1) NOT NULL,
    txtCategoria      VARCHAR(50)     NOT NULL,  -- 'ALMACENAMIENTO', 'SEGURIDAD', 'RETENCION'
    txtCodigo         VARCHAR(100)    NOT NULL,  -- Codigo unico del parametro
    txtNombre         VARCHAR(255)    NOT NULL,
    txtValor          NVARCHAR(MAX)  NOT NULL,  -- Valor del parametro
    txtDescripcion    VARCHAR(500)    NULL,
    flgActivo         BIT            NOT NULL DEFAULT 1,
    estRegistro       BIT            NOT NULL DEFAULT 1,
    txtCreadoPor      VARCHAR(100)   NOT NULL,
    fecCreacion       DATETIME       NOT NULL DEFAULT GETDATE(),
    txtModificadoPor  VARCHAR(100)   NULL,
    fecModificacion   DATETIME       NULL,
    CONSTRAINT API_FILE_TG_PARAMETRO_PK PRIMARY KEY (ideParametro),
    CONSTRAINT API_FILE_TG_PARAMETRO_AK UNIQUE (txtCategoria, txtCodigo),
    CONSTRAINT API_FILE_TG_PARAMETRO_CK_01 CHECK (flgActivo IN (0, 1)),
    CONSTRAINT API_FILE_TG_PARAMETRO_CK_02 CHECK (estRegistro IN (0, 1))
);
GO

-- Indice
CREATE NONCLUSTERED INDEX API_FILE_PARAMETRO_IDX_01
ON ARC.API_FILE_TG_PARAMETRO (txtCategoria ASC, flgActivo ASC, estRegistro ASC)
WITH (FILLFACTOR = 90);
GO

-- Comentarios
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Tabla de parametros generales para configuraciones dinamicas.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TG_PARAMETRO';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Categoria del parametro: ALMACENAMIENTO, SEGURIDAD, RETENCION, NOTIFICACION.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TG_PARAMETRO',
    @level2type = N'COLUMN', @level2name = 'txtCategoria';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Codigo unico del parametro dentro de la categoria.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TG_PARAMETRO',
    @level2type = N'COLUMN', @level2name = 'txtCodigo';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Valor del parametro. Puede ser JSON, texto o numero serializado.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TG_PARAMETRO',
    @level2type = N'COLUMN', @level2name = 'txtValor';
GO

-- =========================================================================
-- Datos iniciales de ejemplo
-- =========================================================================
INSERT INTO ARC.API_FILE_TG_PARAMETRO (txtCategoria, txtCodigo, txtNombre, txtValor, txtDescripcion, flgActivo, estRegistro, txtCreadoPor)
VALUES
(
    'ALMACENAMIENTO',
    'DIAS_RETENCION_ARCHIVOS',
    'Dias de retencion de archivos eliminados',
    '90',
    'Cantidad de dias que un archivo eliminado logicamente permanece en la base de datos antes de ser purgado fisicamente.',
    1,
    1,
    'SYSTEM'
),
(
    'ALMACENAMIENTO',
    'TAMANIO_MAXIMO_UPLOAD_GENERICO',
    'Tamanio maximo de upload generico',
    '52428800',
    'Tamanio maximo en bytes para cualquier archivo (50 MB). Puede ser sobreescrito por configuracion del proveedor.',
    1,
    1,
    'SYSTEM'
),
(
    'SEGURIDAD',
    'HABILITAR_AUDITORIA_DETALLADA',
    'Habilitar auditoria detallada',
    'true',
    'Si true, registra cada acceso a archivo en TC_AUDITORIA. Si false, solo registros de alto nivel (UPLOAD, DELETE).',
    1,
    1,
    'SYSTEM'
),
(
    'RETENCION',
    'DIAS_PURGA_AUDITORIA',
    'Dias para purga de registros de auditoria',
    '365',
    'Registros de auditoria mayores a este numero de dias seran eliminados en el proximo ciclo de limpieza.',
    1,
    1,
    'SYSTEM'
);
GO

PRINT 'V013__tg_parametros.sql ejecutado correctamente.';
GO
