-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V007__sp_c_auditoria.sql
-- Proposito: SP para registrar auditoria de accesos a archivos.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V002__crear_esquema_y_tablas.sql
-- -------------------------------------------------------------------------
-- CONTENIDO:
--   1. API_FILE_SP_C_AUDITORIA - Registrar accion sobre archivo
-- =========================================================================

USE BD_API_FILE;
GO

-- =========================================================================
-- 1. SP: Registrar Auditoria (Create)
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_SP_C_AUDITORIA', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE ARC.API_FILE_SP_C_AUDITORIA;
END;
GO

CREATE PROCEDURE ARC.API_FILE_SP_C_AUDITORIA
    @ideArchivo       UNIQUEIDENTIFIER,
    @txtAccion        VARCHAR(20),
    @txtUsuario      VARCHAR(100)  = NULL,
    @txtIpOrigen     VARCHAR(50)   = NULL,
    @txtEquipoOrigen VARCHAR(100)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ARC.API_FILE_TC_AUDITORIA (
        ideArchivo,
        txtAccion,
        txtUsuario,
        txtIpOrigen,
        txtEquipoOrigen,
        fecAcceso
    )
    VALUES (
        @ideArchivo,
        @txtAccion,
        @txtUsuario,
        @txtIpOrigen,
        @txtEquipoOrigen,
        GETDATE()
    );
END;
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Registra una accion de auditoria sobre un archivo. Acciones validas: UPLOAD, DOWNLOAD, READ, DELETE.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'PROCEDURE', @level1name = 'API_FILE_SP_C_AUDITORIA';
GO

PRINT 'V007__sp_c_auditoria.sql ejecutado correctamente.';
GO
