-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V001__crear_base_datos.sql
-- Proposito: Crear la base de datos BD_API_FILE dedicada para el
--            microservicio sel-api-archivos.
-- Autor:Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- -------------------------------------------------------------------------
-- DETALLES TECNICOS:
--   - Collation: SQL_Latin1_General_CP1_CI_AS (estandar MIDAGRI)
--   - Recovery Model: FULL (requiere backup de transaction log regular)
--   - Filegroups: PRIMARY (datos), FILEGROUP_AUDIT (auditoria)
-- -------------------------------------------------------------------------
-- NOTA: Ejecutar este script como sysadmin o dbcreator.
--       Requiere permisos para CREATE DATABASE.
-- =========================================================================

IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'BD_API_FILE')
BEGIN
    PRINT 'La base de datos BD_API_FILE ya existe. Omitiendo creacion.';
END
ELSE
BEGIN
    CREATE DATABASE BD_API_FILE
    ON PRIMARY (
        name       = 'BD_API_FILE_Data',
        filename   = '$(DATA_PATH)\BD_API_FILE_Data.mdf',
        size       = 100MB,
        maxsize    = UNLIMITED,
        filegrowth  = 64MB
    )
    LOG ON (
        name       = 'BD_API_FILE_Log',
        filename   = '$(LOG_PATH)\BD_API_FILE_Log.ldf',
        size       = 50MB,
        maxsize    = UNLIMITED,
        filegrowth = 32MB
    );
END;
GO

-- Asignar collation
ALTER DATABASE BD_API_FILE
COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

-- Establecer recovery model
ALTER DATABASE BD_API_FILE
SET RECOVERY FULL;
GO

-- Crear filegroup para auditoria (futuro crecimiento)
ALTER DATABASE BD_API_FILE
ADD FILEGROUP FILEGROUP_AUDIT;
GO

-- Mostrar configuracion final
SELECT
    name                          AS database_name,
    collation_name                AS collation,
    recovery_model_desc           AS recovery_model,
    is_read_only                  AS read_only,
    state_desc                    AS state
FROM sys.databases
WHERE name = 'BD_API_FILE';
GO

PRINT 'V001__crear_base_datos.sql ejecutado correctamente.';
GO
