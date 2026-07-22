-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V009__sp_r_proveedores.sql
-- Proposito: SP para listar proveedores de almacenamiento activos.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V002__crear_esquema_y_tablas.sql
-- -------------------------------------------------------------------------
-- CONTENIDO:
--   1. API_FILE_SP_R_PROVEEDORES    - Listar todos los activos
--   2. API_FILE_SP_R_PROVEEDOR_POR_ID - Obtener uno por IDE
-- =========================================================================

USE BD_API_FILE;
GO

-- =========================================================================
-- 1. SP: Listar Proveedores Activos (Read)
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_SP_R_PROVEEDORES', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE ARC.API_FILE_SP_R_PROVEEDORES;
END;
GO

CREATE PROCEDURE ARC.API_FILE_SP_R_PROVEEDORES
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ideProveedor,
        codProveedor,
        txtNombre,
        jsnConfiguracion,
        flgActivo,
        estRegistro,
        txtCreadoPor,
        fecCreacion,
        txtModificadoPor,
        fecModificacion
    FROM ARC.API_FILE_TG_PROVEEDOR
    WHERE flgActivo = 1
      AND estRegistro = 1;
END;
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Lista todos los proveedores de almacenamiento activos (flgActivo=1 y estRegistro=1).',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'PROCEDURE', @level1name = 'API_FILE_SP_R_PROVEEDORES';
GO

-- =========================================================================
-- 2. SP: Obtener Proveedor por IDE (Read)
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_SP_R_PROVEEDOR_POR_ID', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE ARC.API_FILE_SP_R_PROVEEDOR_POR_ID;
END;
GO

CREATE PROCEDURE ARC.API_FILE_SP_R_PROVEEDOR_POR_ID
    @ideProveedor INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ideProveedor,
        codProveedor,
        txtNombre,
        jsnConfiguracion,
        flgActivo,
        estRegistro,
        txtCreadoPor,
        fecCreacion,
        txtModificadoPor,
        fecModificacion
    FROM ARC.API_FILE_TG_PROVEEDOR
    WHERE ideProveedor = @ideProveedor
      AND estRegistro = 1;
END;
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Obtiene un proveedor de almacenamiento por su IDE. Excluye eliminados logicamente.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'PROCEDURE', @level1name = 'API_FILE_SP_R_PROVEEDOR_POR_ID';
GO

PRINT 'V009__sp_r_proveedores.sql ejecutado correctamente.';
GO
