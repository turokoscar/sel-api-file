-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V005__sp_archivo.sql
-- Proposito: SP para crear y obtener archivos.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V002__crear_esquema_y_tablas.sql
-- -------------------------------------------------------------------------
-- CONTENIDO:
--   1. API_FILE_SP_C_ARCHIVO  - Registrar nuevo archivo
--   2. API_FILE_SP_R_ARCHIVO  - Obtener metadata por IDE
-- =========================================================================

USE BD_API_FILE;
GO

-- =========================================================================
-- 1. SP: Registrar Archivo (Create)
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_SP_C_ARCHIVO', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE ARC.API_FILE_SP_C_ARCHIVO;
END;
GO

CREATE PROCEDURE ARC.API_FILE_SP_C_ARCHIVO
    @codSistema          VARCHAR(20),
    @codProceso          VARCHAR(50),
    @txtNombreOriginal   VARCHAR(255),
    @txtNombreFisico     VARCHAR(255),
    @txtExtension        VARCHAR(10),
    @canTamanioBytes    BIGINT,
    @txtContentType      VARCHAR(100),
    @txtChecksumSHA256   VARCHAR(64),
    @txtRutaRelativa     VARCHAR(1000),
    @ideProveedor        INT,
    @txtCreadoPor        VARCHAR(100),
    @ideArchivoGenerated UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @ideArchivoGenerated = NEWID();

    INSERT INTO ARC.API_FILE_TMM_ARCHIVO (
        ideArchivo, codSistema, codProceso, txtNombreOriginal, txtNombreFisico,
        txtExtension, canTamanioBytes, txtContentType, txtChecksumSHA256,
        txtRutaRelativa, ideProveedor, estArchivo, estRegistro,
        txtCreadoPor, fecCreacion, txtModificadoPor, fecModificacion
    )
    VALUES (
        @ideArchivoGenerated,
        @codSistema,
        @codProceso,
        @txtNombreOriginal,
        @txtNombreFisico,
        @txtExtension,
        @canTamanioBytes,
        @txtContentType,
        @txtChecksumSHA256,
        @txtRutaRelativa,
        @ideProveedor,
        1,   -- estArchivo: activo
        1,   -- estRegistro: activo
        @txtCreadoPor,
        GETDATE(),
        NULL, -- txtModificadoPor: NULL en creacion
        NULL  -- fecModificacion: NULL en creacion
    );
END;
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Registra un nuevo archivo en la base de datos. Retorna el IDE generado en @ideArchivoGenerated.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'PROCEDURE', @level1name = 'API_FILE_SP_C_ARCHIVO';
GO

-- =========================================================================
-- 2. SP: Obtener Metadata por IDE (Read)
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_SP_R_ARCHIVO', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE ARC.API_FILE_SP_R_ARCHIVO;
END;
GO

CREATE PROCEDURE ARC.API_FILE_SP_R_ARCHIVO
    @ideArchivo UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.ideArchivo,
        a.codSistema,
        a.codProceso,
        a.txtNombreOriginal,
        a.txtNombreFisico,
        a.txtExtension,
        a.canTamanioBytes,
        a.txtContentType,
        a.txtChecksumSHA256,
        a.txtRutaRelativa,
        a.ideProveedor,
        a.estArchivo,
        a.estRegistro,
        a.txtCreadoPor,
        a.fecCreacion,
        a.txtModificadoPor,
        a.fecModificacion,
        p.codProveedor  AS txtCodProveedor
    FROM ARC.API_FILE_TMM_ARCHIVO a
    INNER JOIN ARC.API_FILE_TG_PROVEEDOR p
        ON p.ideProveedor = a.ideProveedor
    WHERE a.ideArchivo = @ideArchivo
      AND a.estRegistro = 1;  -- Excluir eliminados logicamente
END;
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Obtiene la metadata completa de un archivo por su IDE. Excluye registros con estRegistro=0.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'PROCEDURE', @level1name = 'API_FILE_SP_R_ARCHIVO';
GO

PRINT 'V005__sp_archivo.sql ejecutado correctamente.';
GO
