-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: rollback.sql
-- Proposito: Revertir todos los objetos creados en orden inverso.
--            SOLO PARA AMBIENTE DE DESARROLLO / TESTING.
--            NO EJECUTAR EN PRODUCCION.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- =========================================================================
-- ADVERTENCIA: Este script hace DROP de todos los objetos creados.
--              NO ejecutar en produccion ni en entornos con datos reales.
-- =========================================================================

USE BD_API_FILE;
GO

SET NOCOUNT ON;
PRINT '============================================';
PRINT 'INICIO DE ROLLBACK - BD_API_FILE';
PRINT 'ADVERTENCIA: SOLO PARA DESARROLLO / TESTING';
PRINT '============================================';
GO

-- Orden inverso de creacion

-- 1. Tabla de control de migraciones
IF OBJECT_ID('ARC.API_FILE_TC_MIGRACIONES', 'U') IS NOT NULL
BEGIN
    DROP TABLE ARC.API_FILE_TC_MIGRACIONES;
    PRINT 'Tabla ARC.API_FILE_TC_MIGRACIONES eliminada.';
END
GO

-- 2. Procedimientos almacenados (en orden inverso)
IF OBJECT_ID('ARC.API_FILE_SP_R_PROVEEDOR_POR_ID', 'P') IS NOT NULL
BEGIN DROP PROCEDURE ARC.API_FILE_SP_R_PROVEEDOR_POR_ID; PRINT 'SP ARC.API_FILE_SP_R_PROVEEDOR_POR_ID eliminado.'; END
IF OBJECT_ID('ARC.API_FILE_SP_R_PROVEEDORES', 'P') IS NOT NULL
BEGIN DROP PROCEDURE ARC.API_FILE_SP_R_PROVEEDORES; PRINT 'SP ARC.API_FILE_SP_R_PROVEEDORES eliminado.'; END
IF OBJECT_ID('ARC.API_FILE_SP_R_ARCHIVO_POR_CHECKSUM', 'P') IS NOT NULL
BEGIN DROP PROCEDURE ARC.API_FILE_SP_R_ARCHIVO_POR_CHECKSUM; PRINT 'SP ARC.API_FILE_SP_R_ARCHIVO_POR_CHECKSUM eliminado.'; END
IF OBJECT_ID('ARC.API_FILE_SP_R_ARCHIVOS_POR_SISTEMA', 'P') IS NOT NULL
BEGIN DROP PROCEDURE ARC.API_FILE_SP_R_ARCHIVOS_POR_SISTEMA; PRINT 'SP ARC.API_FILE_SP_R_ARCHIVOS_POR_SISTEMA eliminado.'; END
IF OBJECT_ID('ARC.API_FILE_SP_C_AUDITORIA', 'P') IS NOT NULL
BEGIN DROP PROCEDURE ARC.API_FILE_SP_C_AUDITORIA; PRINT 'SP ARC.API_FILE_SP_C_AUDITORIA eliminado.'; END
IF OBJECT_ID('ARC.API_FILE_SP_D_ARCHIVO', 'P') IS NOT NULL
BEGIN DROP PROCEDURE ARC.API_FILE_SP_D_ARCHIVO; PRINT 'SP ARC.API_FILE_SP_D_ARCHIVO eliminado.'; END
IF OBJECT_ID('ARC.API_FILE_SP_U_ARCHIVO', 'P') IS NOT NULL
BEGIN DROP PROCEDURE ARC.API_FILE_SP_U_ARCHIVO; PRINT 'SP ARC.API_FILE_SP_U_ARCHIVO eliminado.'; END
IF OBJECT_ID('ARC.API_FILE_SP_R_ARCHIVO', 'P') IS NOT NULL
BEGIN DROP PROCEDURE ARC.API_FILE_SP_R_ARCHIVO; PRINT 'SP ARC.API_FILE_SP_R_ARCHIVO eliminado.'; END
IF OBJECT_ID('ARC.API_FILE_SP_C_ARCHIVO', 'P') IS NOT NULL
BEGIN DROP PROCEDURE ARC.API_FILE_SP_C_ARCHIVO; PRINT 'SP ARC.API_FILE_SP_C_ARCHIVO eliminado.'; END
GO

-- 3. Funciones
IF OBJECT_ID('ARC.fn_UsuarioActual', 'FN') IS NOT NULL
BEGIN DROP FUNCTION ARC.fn_UsuarioActual; PRINT 'Funcion ARC.fn_UsuarioActual eliminada.'; END
IF OBJECT_ID('ARC.fn_ObtenerFechaServidor', 'FN') IS NOT NULL
BEGIN DROP FUNCTION ARC.fn_ObtenerFechaServidor; PRINT 'Funcion ARC.fn_ObtenerFechaServidor eliminada.'; END
GO

-- 4. Tablas (en orden inverso de dependencia)
IF OBJECT_ID('ARC.API_FILE_TC_AUDITORIA', 'U') IS NOT NULL
BEGIN DROP TABLE ARC.API_FILE_TC_AUDITORIA; PRINT 'Tabla ARC.API_FILE_TC_AUDITORIA eliminada.'; END
IF OBJECT_ID('ARC.API_FILE_TMM_ARCHIVO', 'U') IS NOT NULL
BEGIN DROP TABLE ARC.API_FILE_TMM_ARCHIVO; PRINT 'Tabla ARC.API_FILE_TMM_ARCHIVO eliminada.'; END
IF OBJECT_ID('ARC.API_FILE_TG_PROVEEDOR', 'U') IS NOT NULL
BEGIN DROP TABLE ARC.API_FILE_TG_PROVEEDOR; PRINT 'Tabla ARC.API_FILE_TG_PROVEEDOR eliminada.'; END
GO

-- 5. Esquema
IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ARC')
BEGIN
    DROP SCHEMA ARC;
    PRINT 'Esquema ARC eliminado.';
END
GO

-- 6. Usuarios de base de datos
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'usr_api_file_app')
BEGIN
    DROP USER usr_api_file_app;
    PRINT 'Usuario usr_api_file_app eliminado.';
END
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'usr_api_file_read')
BEGIN
    DROP USER usr_api_file_read;
    PRINT 'Usuario usr_api_file_read eliminado.';
END
GO

-- 7. Logins de servidor
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'usr_api_file_app')
BEGIN
    DROP LOGIN usr_api_file_app;
    PRINT 'Login usr_api_file_app eliminado.';
END
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'usr_api_file_read')
BEGIN
    DROP LOGIN usr_api_file_read;
    PRINT 'Login usr_api_file_read eliminado.';
END
GO

PRINT '============================================';
PRINT 'ROLLBACK COMPLETADO';
PRINT 'ADVERTENCIA: La base de datos BD_API_FILE aun existe.';
PRINT 'Para eliminarla: DROP DATABASE BD_API_FILE;';
PRINT '============================================';
GO
