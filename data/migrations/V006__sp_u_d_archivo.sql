-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V006__sp_u_d_archivo.sql
-- Proposito: SP para actualizar y eliminar (soft-delete) archivos.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V002__crear_esquema_y_tablas.sql, V005__sp_archivo.sql
-- -------------------------------------------------------------------------
-- CONTENIDO:
--   1. API_FILE_SP_U_ARCHIVO  - Actualizar metadata de archivo
--   2. API_FILE_SP_D_ARCHIVO  - Eliminacion logica (soft-delete)
-- =========================================================================

USE BD_API_FILE;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =========================================================================
-- 1. SP: Actualizar Archivo (Update)
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_SP_U_ARCHIVO', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE ARC.API_FILE_SP_U_ARCHIVO;
END;
GO

CREATE PROCEDURE ARC.API_FILE_SP_U_ARCHIVO
    @ideArchivo          UNIQUEIDENTIFIER,
    @txtNombreFisico    VARCHAR(255)    = NULL,
    @txtRutaRelativa     VARCHAR(1000)   = NULL,
    @txtModificadoPor    VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Verificar que el archivo existe y esta activo
    IF NOT EXISTS (
        SELECT 1
        FROM ARC.API_FILE_TMM_ARCHIVO
        WHERE ideArchivo = @ideArchivo
          AND estRegistro = 1
    )
    BEGIN
        RAISERROR('ARCHIVO_NO_ENCONTRADO', 16, 1);
        RETURN;
    END

    UPDATE ARC.API_FILE_TMM_ARCHIVO
    SET
        txtNombreFisico   = ISNULL(@txtNombreFisico, txtNombreFisico),
        txtRutaRelativa   = ISNULL(@txtRutaRelativa, txtRutaRelativa),
        txtModificadoPor   = @txtModificadoPor,
        fecModificacion   = GETDATE()
    WHERE ideArchivo = @ideArchivo
      AND estRegistro = 1;
END;
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Actualiza la metadata de un archivo (nombre fisico y/o ruta). Solo campos no nulos son actualizados.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'PROCEDURE', @level1name = 'API_FILE_SP_U_ARCHIVO';
GO

-- =========================================================================
-- 2. SP: Eliminar Archivo - Logico (Delete / Soft-Delete)
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_SP_D_ARCHIVO', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE ARC.API_FILE_SP_D_ARCHIVO;
END;
GO

CREATE PROCEDURE ARC.API_FILE_SP_D_ARCHIVO
    @ideArchivo          UNIQUEIDENTIFIER,
    @txtModificadoPor    VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- No lanzamos error si no existe - retornamos rowsAffected = 0
    UPDATE ARC.API_FILE_TMM_ARCHIVO
    SET
        estArchivo         = 0,  -- Marcar archivo como inactivo
        estRegistro        = 0,  -- Eliminacion logica
        txtModificadoPor   = ISNULL(@txtModificadoPor, 'SYSTEM'),
        fecModificacion    = GETDATE()
    WHERE ideArchivo = @ideArchivo
      AND estRegistro = 1;  -- Solo si no esta ya eliminado
END;
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Elimina un archivo de forma logica (soft-delete). Marca estArchivo=0 y estRegistro=0.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'PROCEDURE', @level1name = 'API_FILE_SP_D_ARCHIVO';
GO

PRINT 'V006__sp_u_d_archivo.sql ejecutado correctamente.';
GO
