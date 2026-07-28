-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V002__crear_esquema_y_tablas.sql
-- Proposito: Crear el esquema ARC y las 3 tablas del modulo de archivos.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V001__crear_base_datos.sql
-- -------------------------------------------------------------------------
-- CONTENIDO:
--   1. Esquema ARC
--   2. ARC.API_FILE_TG_PROVEEDOR   (Maestro - proveedores de almacenamiento)
--   3. ARC.API_FILE_TMM_ARCHIVO     (Transaccional - metadata de archivos)
--   4. ARC.API_FILE_TC_AUDITORIA   (Control - log de accesos)
-- -------------------------------------------------------------------------
-- NOTA: Los campos de auditoria por fila (txtCreadoPor, txtModificadoPor,
--       fecCreacion, fecModificacion) son obligatorios en todas las tablas
--       maestras y transaccionales segun estandar MIDAGRI.
--       La tabla TC_AUDITORIA no lleva soft-delete porque es un historico.
-- =========================================================================

USE BD_API_FILE;
GO

-- =========================================================================
-- 1. Crear esquema ARC
-- =========================================================================
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ARC')
BEGIN
    EXEC('CREATE SCHEMA ARC AUTHORIZATION dbo;');
END;
GO

-- =========================================================================
-- 2. Tabla de Maestros - Proveedores de Almacenamiento
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_TG_PROVEEDOR', 'U') IS NOT NULL
BEGIN
    DROP TABLE ARC.API_FILE_TG_PROVEEDOR;
END;
GO

CREATE TABLE ARC.API_FILE_TG_PROVEEDOR (
    -- Identificacion
    ideProveedor         INT             IDENTITY(1,1) NOT NULL,

    -- Datos del proveedor
    codProveedor         VARCHAR(20)     NOT NULL,  -- 'LOCAL', 'FTP', 'SFTP', 'AWS_S3', 'GCS'
    txtNombre            VARCHAR(100)    NOT NULL,

    -- Configuracion en JSON (maxFileSizeBytes, allowedContentTypes, localStoragePath, etc.)
    jsnConfiguracion     NVARCHAR(MAX)  NOT NULL,

    -- Flags de estado
    flgActivo            BIT            NOT NULL DEFAULT 1,  -- 1=activo, 0=inactivo
    estRegistro          BIT            NOT NULL DEFAULT 1,  -- 1=registro activo, 0=eliminado logico

    -- Auditoria por fila
    txtCreadoPor         VARCHAR(100)   NOT NULL,
    fecCreacion          DATETIME       NOT NULL DEFAULT GETDATE(),
    txtModificadoPor     VARCHAR(100)   NULL,
    fecModificacion      DATETIME       NULL,

    -- Constraints
    CONSTRAINT API_FILE_TG_PROVEEDOR_PK PRIMARY KEY CLUSTERED (ideProveedor),
    CONSTRAINT API_FILE_TG_PROVEEDOR_AK UNIQUE (codProveedor),
    CONSTRAINT API_FILE_TG_PROVEEDOR_CK_01 CHECK (flgActivo IN (0, 1)),
    CONSTRAINT API_FILE_TG_PROVEEDOR_CK_02 CHECK (estRegistro IN (0, 1))
);
GO

-- Comentarios de tabla y columnas
EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'Maestro de proveedores de almacenamiento.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TG_PROVEEDOR';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'Codigo unico del proveedor: LOCAL, FTP, SFTP, AWS_S3, GCS.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TG_PROVEEDOR',
    @level2type = N'COLUMN', @level2name = 'codProveedor';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'JSON con configuracion: localStoragePath, maxFileSizeBytes, allowedContentTypes.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TG_PROVEEDOR',
    @level2type = N'COLUMN', @level2name = 'jsnConfiguracion';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'Indica si el proveedor esta activo para operaciones.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TG_PROVEEDOR',
    @level2type = N'COLUMN', @level2name = 'flgActivo';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'Eliminado logico del registro.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TG_PROVEEDOR',
    @level2type = N'COLUMN', @level2name = 'estRegistro';
GO

-- =========================================================================
-- 3. Tabla Transaccional - Metadata de Archivos
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_TMM_ARCHIVO', 'U') IS NOT NULL
BEGIN
    DROP TABLE ARC.API_FILE_TMM_ARCHIVO;
END;
GO

CREATE TABLE ARC.API_FILE_TMM_ARCHIVO (
    -- Identificacion
    ideArchivo           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),

    -- Clasificacion
    codSistema          VARCHAR(20)     NOT NULL,  -- Sistema consumidor: KOFIX, SAT, SEL
    codProceso          VARCHAR(50)     NULL,      -- Proceso dentro del sistema

    -- Nombre y extension
    txtNombreOriginal   VARCHAR(255)    NOT NULL,  -- Nombre original del archivo
    txtNombreFisico     VARCHAR(255)    NOT NULL,  -- Nombre fisico generado (GUID)
    txtExtension        VARCHAR(10)     NOT NULL,  -- Extension: pdf, png, jpg, etc.

    -- Tamanio y tipo
    canTamanioBytes     BIGINT          NOT NULL,  -- Tamanio en bytes
    txtContentType      VARCHAR(100)    NOT NULL,  -- MIME type

    -- Hash y ruta
    txtChecksumSHA256    VARCHAR(64)     NOT NULL,  -- SHA256 del contenido
    txtRutaRelativa     VARCHAR(1000)   NOT NULL,  -- Ruta relativa dentro del provider

    -- Relacion con proveedor
    ideProveedor        INT             NOT NULL,

    -- Estados
    estArchivo          BIT            NOT NULL DEFAULT 1,  -- 1=activo, 0=eliminado logico
    estRegistro         BIT            NOT NULL DEFAULT 1,  -- Eliminado logico general

    -- Auditoria por fila
    txtCreadoPor        VARCHAR(100)    NOT NULL,
    fecCreacion         DATETIME        NOT NULL DEFAULT GETDATE(),
    txtModificadoPor    VARCHAR(100)    NULL,
    fecModificacion     DATETIME        NULL,

    -- Constraints
    CONSTRAINT API_FILE_TMM_ARCHIVO_PK PRIMARY KEY CLUSTERED (ideArchivo),
    CONSTRAINT API_FILE_TMM_ARCHIVO_PROVEEDOR_FK_01
        FOREIGN KEY (ideProveedor) REFERENCES ARC.API_FILE_TG_PROVEEDOR(ideProveedor),
    CONSTRAINT API_FILE_TMM_ARCHIVO_CK_01 CHECK (canTamanioBytes > 0),
    CONSTRAINT API_FILE_TMM_ARCHIVO_CK_02 CHECK (LEN(txtExtension) > 0),
    CONSTRAINT API_FILE_TMM_ARCHIVO_CK_03 CHECK (estArchivo IN (0, 1)),
    CONSTRAINT API_FILE_TMM_ARCHIVO_CK_04 CHECK (estRegistro IN (0, 1))
);
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'Metadata de archivos almacenados.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TMM_ARCHIVO';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'Codigo del sistema consumidor que creo el archivo.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TMM_ARCHIVO',
    @level2type = N'COLUMN', @level2name = 'codSistema';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'Checksum SHA256 del contenido del archivo.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TMM_ARCHIVO',
    @level2type = N'COLUMN', @level2name = 'txtChecksumSHA256';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'Eliminado logico especifico del archivo.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TMM_ARCHIVO',
    @level2type = N'COLUMN', @level2name = 'estArchivo';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'Eliminado logico general del registro.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TMM_ARCHIVO',
    @level2type = N'COLUMN', @level2name = 'estRegistro';
GO

-- =========================================================================
-- 4. Tabla de Control - Auditoria de Accesos
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_TC_AUDITORIA', 'U') IS NOT NULL
BEGIN
    DROP TABLE ARC.API_FILE_TC_AUDITORIA;
END;
GO

CREATE TABLE ARC.API_FILE_TC_AUDITORIA (
    -- Identificacion
    ideAuditoria        BIGINT IDENTITY(1,1) NOT NULL,

    -- Referencia al archivo
    ideArchivo          UNIQUEIDENTIFIER NOT NULL,

    -- Accion realizada
    txtAccion           VARCHAR(20)     NOT NULL,  -- UPLOAD, DOWNLOAD, READ, DELETE

    -- Usuario y origen
    txtUsuario          VARCHAR(100)    NULL,      -- Usuario JWT
    txtIpOrigen         VARCHAR(50)     NULL,      -- IP del cliente
    txtEquipoOrigen     VARCHAR(100)    NULL,      -- Nombre del host de origen

    -- Timestamp
    fecAcceso           DATETIME        NOT NULL DEFAULT GETDATE(),

    -- Constraint
    CONSTRAINT API_FILE_TC_AUDITORIA_PK PRIMARY KEY CLUSTERED (ideAuditoria),
    CONSTRAINT API_FILE_TC_AUDITORIA_ARCHIVO_FK_01
        FOREIGN KEY (ideArchivo) REFERENCES ARC.API_FILE_TMM_ARCHIVO(ideArchivo)
);
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'Log de auditoria de accesos a archivos.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TC_AUDITORIA';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description', @value = N'Nombre del equipo desde donde se realizo el acceso.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TC_AUDITORIA',
    @level2type = N'COLUMN', @level2name = 'txtEquipoOrigen';
GO

-- =========================================================================
-- 5. Insertar datos iniciales de proveedores
-- =========================================================================
INSERT INTO ARC.API_FILE_TG_PROVEEDOR (codProveedor, txtNombre, jsnConfiguracion, flgActivo, estRegistro, txtCreadoPor)
VALUES
(
    'LOCAL',
    'Almacenamiento Local del Servidor',
    '{"localStoragePath": "/var/storage", "maxFileSizeBytes": 52428800, "allowedContentTypes": ["application/pdf", "image/png", "image/jpeg", "text/plain"]}',
    1,
    1,
    'SYSTEM'
);
GO

PRINT 'V002__crear_esquema_y_tablas.sql ejecutado correctamente.';
GO
