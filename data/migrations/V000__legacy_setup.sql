-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Módulo: ARC (Archivos)
-- Descripción: Scripts de creación de base de datos y Stored Procedures
-- Fecha: 2026-06-22
-- =========================================================================

USE BD_SEL_DEV;
GO

-- 1. Crear Esquema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'ARC')
BEGIN
    EXEC('CREATE SCHEMA ARC;');
END;
GO

-- 2. Eliminar Procedimientos Almacenados si existen (para facilidad de despliegue)
IF OBJECT_ID('ARC.SEL_ARC_SP_C_ARCHIVO', 'P') IS NOT NULL DROP PROCEDURE ARC.SEL_ARC_SP_C_ARCHIVO;
IF OBJECT_ID('ARC.SEL_ARC_SP_R_ARCHIVO', 'P') IS NOT NULL DROP PROCEDURE ARC.SEL_ARC_SP_R_ARCHIVO;
IF OBJECT_ID('ARC.SEL_ARC_SP_D_ARCHIVO', 'P') IS NOT NULL DROP PROCEDURE ARC.SEL_ARC_SP_D_ARCHIVO;
IF OBJECT_ID('ARC.SEL_ARC_SP_C_AUDITORIA', 'P') IS NOT NULL DROP PROCEDURE ARC.SEL_ARC_SP_C_AUDITORIA;
IF OBJECT_ID('ARC.SEL_ARC_SP_R_PROVEEDORES', 'P') IS NOT NULL DROP PROCEDURE ARC.SEL_ARC_SP_R_PROVEEDORES;
GO

-- 3. Eliminar Tablas si existen (en orden inverso de dependencia)
IF OBJECT_ID('ARC.SEL_ARC_TC_AUDITORIA', 'U') IS NOT NULL DROP TABLE ARC.SEL_ARC_TC_AUDITORIA;
IF OBJECT_ID('ARC.SEL_ARC_TMM_ARCHIVO', 'U') IS NOT NULL DROP TABLE ARC.SEL_ARC_TMM_ARCHIVO;
IF OBJECT_ID('ARC.SEL_ARC_TG_PROVEEDOR', 'U') IS NOT NULL DROP TABLE ARC.SEL_ARC_TG_PROVEEDOR;
GO

-- 4. Creación de Tablas

-- Tabla General de Proveedores de Almacenamiento (Celeste - Maestros)
CREATE TABLE ARC.SEL_ARC_TG_PROVEEDOR (
    ideProveedor INT IDENTITY(1,1) NOT NULL,
    codProveedor VARCHAR(20) NOT NULL, -- 'LOCAL', 'FTP', 'SFTP', 'AWS_S3', 'GCS'
    txtNombre VARCHAR(100) NOT NULL,
    jsnConfiguracion NVARCHAR(MAX) NOT NULL,
    flgActivo BIT NOT NULL DEFAULT 1,
    fecRegistro DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT SEL_ARC_PROVEEDOR_PK PRIMARY KEY (ideProveedor),
    CONSTRAINT SEL_ARC_PROVEEDOR_AK UNIQUE (codProveedor)
);
GO

-- Tabla Transaccional de Metadata de Archivos (Amarillo - Transaccional Movimientos)
CREATE TABLE ARC.SEL_ARC_TMM_ARCHIVO (
    ideArchivo UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    codSistema VARCHAR(20) NOT NULL,      -- Identifica al sistema consumidor: 'KOFIX', 'SAT', 'SEL'
    codProceso VARCHAR(50) NULL,          -- Identifica el proceso: 'FACTURACION', 'CONTRATOS'
    txtNombreOriginal VARCHAR(255) NOT NULL,
    txtNombreFisico VARCHAR(255) NOT NULL,
    txtExtension VARCHAR(10) NOT NULL,
    canTamanioBytes BIGINT NOT NULL,
    txtContentType VARCHAR(100) NOT NULL,
    txtChecksumSHA256 VARCHAR(64) NOT NULL,
    txtRutaRelativa VARCHAR(1000) NOT NULL,
    ideProveedor INT NOT NULL,
    estArchivo BIT NOT NULL DEFAULT 1, -- 1: Activo, 0: Eliminado lógico
    txtCreadoPor VARCHAR(100) NULL,
    fecCreacion DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT SEL_ARC_ARCHIVO_PK PRIMARY KEY (ideArchivo),
    CONSTRAINT SEL_ARC_ARCHIVO_PROVEEDOR_FK_01 FOREIGN KEY (ideProveedor) REFERENCES ARC.SEL_ARC_TG_PROVEEDOR(ideProveedor)
);
GO

-- Crear índices para búsquedas rápidas por sistema y proceso
CREATE INDEX SEL_ARC_ARCHIVO_IDX_01 ON ARC.SEL_ARC_TMM_ARCHIVO(codSistema, codProceso);
GO

-- Tabla de Control de Auditoría (Plomo - Control)
CREATE TABLE ARC.SEL_ARC_TC_AUDITORIA (
    ideAuditoria BIGINT IDENTITY(1,1) NOT NULL,
    ideArchivo UNIQUEIDENTIFIER NOT NULL,
    txtAccion VARCHAR(20) NOT NULL, -- 'UPLOAD', 'DOWNLOAD', 'READ', 'DELETE'
    txtUsuario VARCHAR(100) NULL,
    txtIpOrigen VARCHAR(50) NULL,
    fecAcceso DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT SEL_ARC_AUDITORIA_PK PRIMARY KEY (ideAuditoria),
    CONSTRAINT SEL_ARC_AUDITORIA_ARCHIVO_FK_01 FOREIGN KEY (ideArchivo) REFERENCES ARC.SEL_ARC_TMM_ARCHIVO(ideArchivo)
);
GO

-- 5. Creación de Procedimientos Almacenados (SPs)

-- SP: Registrar Archivo (Create)
CREATE PROCEDURE ARC.SEL_ARC_SP_C_ARCHIVO
    @codSistema VARCHAR(20),
    @codProceso VARCHAR(50),
    @txtNombreOriginal VARCHAR(255),
    @txtNombreFisico VARCHAR(255),
    @txtExtension VARCHAR(10),
    @canTamanioBytes BIGINT,
    @txtContentType VARCHAR(100),
    @txtChecksumSHA256 VARCHAR(64),
    @txtRutaRelativa VARCHAR(1000),
    @ideProveedor INT,
    @txtCreadoPor VARCHAR(100),
    @ideArchivoGenerated UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @ideArchivoGenerated = NEWID();

    INSERT INTO ARC.SEL_ARC_TMM_ARCHIVO (
        ideArchivo, codSistema, codProceso, txtNombreOriginal, txtNombreFisico, txtExtension,
        canTamanioBytes, txtContentType, txtChecksumSHA256, txtRutaRelativa,
        ideProveedor, estArchivo, txtCreadoPor, fecCreacion
    )
    VALUES (
        @ideArchivoGenerated, @codSistema, @codProceso, @txtNombreOriginal, @txtNombreFisico, @txtExtension,
        @canTamanioBytes, @txtContentType, @txtChecksumSHA256, @txtRutaRelativa,
        @ideProveedor, 1, @txtCreadoPor, GETDATE()
    );
END;
GO

-- SP: Obtener Metadata por ID (Read)
CREATE PROCEDURE ARC.SEL_ARC_SP_R_ARCHIVO
    @ideArchivo UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        ideArchivo, codSistema, codProceso, txtNombreOriginal, txtNombreFisico, txtExtension,
        canTamanioBytes, txtContentType, txtChecksumSHA256, txtRutaRelativa,
        ideProveedor, estArchivo, txtCreadoPor, fecCreacion
    FROM ARC.SEL_ARC_TMM_ARCHIVO
    WHERE ideArchivo = @ideArchivo AND estArchivo = 1;
END;
GO

-- SP: Eliminar Archivo - Lógico (Delete/Update)
CREATE PROCEDURE ARC.SEL_ARC_SP_D_ARCHIVO
    @ideArchivo UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ARC.SEL_ARC_TMM_ARCHIVO
    SET estArchivo = 0
    WHERE ideArchivo = @ideArchivo;
END;
GO

-- SP: Registrar Auditoría de Acceso (Create)
CREATE PROCEDURE ARC.SEL_ARC_SP_C_AUDITORIA
    @ideArchivo UNIQUEIDENTIFIER,
    @txtAccion VARCHAR(20),
    @txtUsuario VARCHAR(100),
    @txtIpOrigen VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ARC.SEL_ARC_TC_AUDITORIA (ideArchivo, txtAccion, txtUsuario, txtIpOrigen, fecAcceso)
    VALUES (@ideArchivo, @txtAccion, @txtUsuario, @txtIpOrigen, GETDATE());
END;
GO

-- SP: Listar Proveedores Activos (Read)
CREATE PROCEDURE ARC.SEL_ARC_SP_R_PROVEEDORES
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ideProveedor, codProveedor, txtNombre, jsnConfiguracion, flgActivo, fecRegistro
    FROM ARC.SEL_ARC_TG_PROVEEDOR
    WHERE flgActivo = 1;
END;
GO

-- 6. Insertar Proveedores de Almacenamiento Iniciales de Prueba/Configuración
-- Configuración: localStoragePath (ruta base), maxFileSizeBytes (0 = sin límite),
-- allowedContentTypes (vacío = permite todos)
INSERT INTO ARC.SEL_ARC_TG_PROVEEDOR (codProveedor, txtNombre, jsnConfiguracion, flgActivo)
VALUES
('LOCAL', 'Almacenamiento Local Servidor', '{"localStoragePath": "/home/opazos/MyProjects/AGROIDEAS/SEL_APIS/sel-api-archivos/storage", "maxFileSizeBytes": 52428800, "allowedContentTypes": ["application/pdf", "image/png", "image/jpeg", "text/plain"]}', 1),
('FTP', 'Servidor FTP Externo', '{"localStoragePath": "/ftp/storage", "maxFileSizeBytes": 10485760, "allowedContentTypes": ["application/pdf"]}', 1);
GO
