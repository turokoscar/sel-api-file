-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V008__sp_read_adicionales.sql
-- Proposito: SPs de lectura adicionales con paginacion y busqueda por checksum.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V002__crear_esquema_y_tablas.sql, V005__sp_archivo.sql
-- -------------------------------------------------------------------------
-- CONTENIDO:
--   1. API_FILE_SP_R_ARCHIVOS_POR_SISTEMA - Listar archivos con paginacion
--   2. API_FILE_SP_R_ARCHIVO_POR_CHECKSUM  - Buscar archivo por SHA256
-- =========================================================================

USE BD_API_FILE;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =========================================================================
-- 1. SP: Listar Archivos por Sistema con Paginacion (Read)
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_SP_R_ARCHIVOS_POR_SISTEMA', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE ARC.API_FILE_SP_R_ARCHIVOS_POR_SISTEMA;
END;
GO

CREATE PROCEDURE ARC.API_FILE_SP_R_ARCHIVOS_POR_SISTEMA
    @codSistema  VARCHAR(20),
    @codProceso  VARCHAR(50)   = NULL,
    @pageIndex   INT           = 1,
    @pageSize    INT           = 20,
    @totalRows   INT           = 0 OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Validaciones
    IF @pageIndex < 1 SET @pageIndex = 1;
    IF @pageSize < 1 SET @pageSize = 20;
    IF @pageSize > 100 SET @pageSize = 100;

    DECLARE @offsetRows INT = (@pageIndex - 1) * @pageSize;

    -- Contar total de filas
    SELECT @totalRows = COUNT(1)
    FROM ARC.API_FILE_TMM_ARCHIVO
    WHERE codSistema = @codSistema
      AND estRegistro = 1
      AND (@codProceso IS NULL OR codProceso = @codProceso);

    -- Obtener pagina
    SELECT
        ideArchivo,
        codSistema,
        codProceso,
        txtNombreOriginal,
        txtExtension,
        canTamanioBytes,
        txtContentType,
        txtChecksumSHA256,
        fecCreacion,
        txtCreadoPor
    FROM ARC.API_FILE_TMM_ARCHIVO
    WHERE codSistema = @codSistema
      AND estRegistro = 1
      AND (@codProceso IS NULL OR codProceso = @codProceso)
    ORDER BY fecCreacion DESC
    OFFSET @offsetRows ROWS
    FETCH NEXT @pageSize ROWS ONLY;
END;
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Lista archivos por sistema y opcionalmente por proceso, con paginacion. Retorna @totalRows como output.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'PROCEDURE', @level1name = 'API_FILE_SP_R_ARCHIVOS_POR_SISTEMA';
GO

-- =========================================================================
-- 2. SP: Buscar Archivo por Checksum SHA256 (Read)
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_SP_R_ARCHIVO_POR_CHECKSUM', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE ARC.API_FILE_SP_R_ARCHIVO_POR_CHECKSUM;
END;
GO

CREATE PROCEDURE ARC.API_FILE_SP_R_ARCHIVO_POR_CHECKSUM
    @txtChecksumSHA256 VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ideArchivo,
        codSistema,
        codProceso,
        txtNombreOriginal,
        txtNombreFisico,
        txtExtension,
        canTamanioBytes,
        txtContentType,
        txtChecksumSHA256,
        txtRutaRelativa,
        ideProveedor,
        estArchivo,
        txtCreadoPor,
        fecCreacion
    FROM ARC.API_FILE_TMM_ARCHIVO
    WHERE txtChecksumSHA256 = @txtChecksumSHA256
      AND estRegistro = 1
      AND estArchivo = 1;
END;
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Busca un archivo activo por su checksum SHA256. Util para detectar duplicados.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'PROCEDURE', @level1name = 'API_FILE_SP_R_ARCHIVO_POR_CHECKSUM';
GO

PRINT 'V008__sp_read_adicionales.sql ejecutado correctamente.';
GO
