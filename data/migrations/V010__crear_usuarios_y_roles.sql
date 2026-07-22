-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V010__crear_usuarios_y_roles.sql
-- Proposito: Crear usuarios de base de datos y roles con permisos minimalistas.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V002__crear_esquema_y_tablas.sql
-- -------------------------------------------------------------------------
-- USUARIOS CREADOS:
--   usr_api_file_app  - Aplicacion (lectura + escritura)
--   usr_api_file_read - Lectura unicamente (reportes, auditoria)
-- -------------------------------------------------------------------------
-- NOTA: Este script debe ejecutarse en la base de datos BD_API_FILE.
--       Las contrasenas deben cambiarse en produccion.
--       No usar estas credenciales en texto plano en repositorio.
-- =========================================================================

USE BD_API_FILE;
GO

SET NOCOUNT ON;
PRINT 'Iniciando V010__crear_usuarios_y_roles.sql';
GO

-- =========================================================================
-- 1. Crear login para la aplicacion
-- =========================================================================
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'usr_api_file_app')
BEGIN
    PRINT 'Login usr_api_file_app ya existe. Omitiendo creacion.';
END
ELSE
BEGIN
    CREATE LOGIN usr_api_file_app
    WITH PASSWORD = 'CambiarContrasena2026!',
         DEFAULT_DATABASE = BD_API_FILE,
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = ON;
    PRINT 'Login usr_api_file_app creado.';
END
GO

-- =========================================================================
-- 2. Crear login para lectura
-- =========================================================================
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'usr_api_file_read')
BEGIN
    PRINT 'Login usr_api_file_read ya existe. Omitiendo creacion.';
END
ELSE
BEGIN
    CREATE LOGIN usr_api_file_read
    WITH PASSWORD = 'CambiarContrasena2026!',
         DEFAULT_DATABASE = BD_API_FILE,
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = ON;
    PRINT 'Login usr_api_file_read creado.';
END
GO

-- =========================================================================
-- 3. Crear usuarios en la base de datos
-- =========================================================================
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'usr_api_file_app')
BEGIN
    DROP USER usr_api_file_app;
END
CREATE USER usr_api_file_app FOR LOGIN usr_api_file_app;
PRINT 'Usuario usr_api_file_app creado en BD_API_FILE.';
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'usr_api_file_read')
BEGIN
    DROP USER usr_api_file_read;
END
CREATE USER usr_api_file_read FOR LOGIN usr_api_file_read;
PRINT 'Usuario usr_api_file_read creado en BD_API_FILE.';
GO

-- =========================================================================
-- 4. Asignar ownership del esquema ARC a la aplicacion
-- =========================================================================
ALTER AUTHORIZATION ON SCHEMA::ARC TO usr_api_file_app;
PRINT 'Ownership del esquema ARC asignado a usr_api_file_app.';
GO

-- =========================================================================
-- 5. Permisos para usr_api_file_app (lectura + escritura)
-- =========================================================================
GRANT CONNECT ON DATABASE::BD_API_FILE TO usr_api_file_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::ARC TO usr_api_file_app;
GRANT EXECUTE ON SCHEMA::ARC TO usr_api_file_app;
PRINT 'Permisos de lectura/escritura asignados a usr_api_file_app.';
GO

-- =========================================================================
-- 6. Permisos para usr_api_file_read (solo lectura)
-- =========================================================================
GRANT CONNECT ON DATABASE::BD_API_FILE TO usr_api_file_read;
GRANT SELECT ON SCHEMA::ARC TO usr_api_file_read;
GRANT EXECUTE ON SCHEMA::ARC TO usr_api_file_read;
PRINT 'Permisos de solo lectura asignados a usr_api_file_read.';
GO

-- =========================================================================
-- 7. Revocar permisos de dbo (si estaban asignados)
-- =========================================================================
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'dbo')
BEGIN
    REVOKE SELECT, INSERT, UPDATE, DELETE ON SCHEMA::ARC TO dbo;
    PRINT 'Permisos de dbo sobre esquema ARC revocados.';
END
GO

-- =========================================================================
-- 8. Verificacion
-- =========================================================================
SELECT
    dp.name              AS usuario,
    dp.type_desc        AS tipo,
    dp.authentication_type_desc AS auth_type,
    QUOTENAME(schemas.name) AS esquema,
    dp.create_date
FROM sys.database_principals dp
INNER JOIN sys.schemas schemas ON schemas.principal_id = dp.principal_id
WHERE dp.name IN ('usr_api_file_app', 'usr_api_file_read')
   OR (dp.type IN ('S', 'U', 'G') AND dp.name = 'dbo')
ORDER BY dp.name;
GO

-- Permisos efectivos
SELECT
    USER_NAME(dp.grantee_principal_id) AS usuario,
    dp.permission_name,
    dp.state_desc,
    QUOTENAME(OBJECT_SCHEMA_NAME(dp.major_id)) + '.' + QUOTENAME(OBJECT_NAME(dp.major_id)) AS objeto
FROM sys.database_permissions dp
WHERE dp.grantee_principal_id IN (
    SELECT principal_id FROM sys.database_principals
    WHERE name IN ('usr_api_file_app', 'usr_api_file_read')
);
GO

PRINT 'V010__crear_usuarios_y_roles.sql ejecutado correctamente.';
GO
