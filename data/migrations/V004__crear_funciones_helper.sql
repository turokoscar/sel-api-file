-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V004__crear_funciones_helper.sql
-- Proposito: Crear funciones auxiliares de uso comun en stored procedures.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V002__crear_esquema_y_tablas.sql
-- -------------------------------------------------------------------------
-- FUNCIONES CREADAS:
--   1. ARC.fn_ObtenerFechaServidor()  - Retorna GETDATE() normalizado
--   2. ARC.fn_UsuarioActual()         - Retorna el usuario de contexto actual
-- =========================================================================

USE BD_API_FILE;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =========================================================================
-- 1. Funcion: fn_ObtenerFechaServidor
-- Proposito: Estandarizar la obtencion de fecha/hora del servidor.
--            Usar en lugar de GETDATE() directo en SPs para facilitar
--            cambios futuros de zona horaria o servidor.
-- Uso: SELECT ARC.fn_ObtenerFechaServidor()
-- =========================================================================
IF OBJECT_ID('ARC.fn_ObtenerFechaServidor', 'FN') IS NOT NULL
BEGIN
    DROP FUNCTION ARC.fn_ObtenerFechaServidor;
END;
GO

CREATE FUNCTION ARC.fn_ObtenerFechaServidor()
RETURNS DATETIME
WITH SCHEMABINDING
AS
BEGIN
    RETURN GETDATE();
END;
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Retorna la fecha y hora actual del servidor SQL Server. Usar en lugar de GETDATE().',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'FUNCTION', @level1name = 'fn_ObtenerFechaServidor';
GO

-- =========================================================================
-- 2. Funcion: fn_UsuarioActual
-- Proposito: Obtener el usuario de base de datos actual de forma consistente.
--            Incluye el host cuando esta disponible.
-- Uso: SELECT ARC.fn_UsuarioActual()
-- =========================================================================
IF OBJECT_ID('ARC.fn_UsuarioActual', 'FN') IS NOT NULL
BEGIN
    DROP FUNCTION ARC.fn_UsuarioActual;
END;
GO

CREATE FUNCTION ARC.fn_UsuarioActual()
RETURNS VARCHAR(100)
WITH SCHEMABINDING
AS
BEGIN
    DECLARE @usuario VARCHAR(100);

    -- Intentar obtener el login de SQL Server
    SET @usuario = ORIGINAL_LOGIN();

    -- Si esta vacio, usar el usuario de la sesion
    IF @usuario IS NULL OR @usuario = ''
    BEGIN
        SET @usuario = SYSTEM_USER;
    END

    -- Limitar longitud
    IF LEN(@usuario) > 100
    BEGIN
        SET @usuario = LEFT(@usuario, 100);
    END

    RETURN @usuario;
END;
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Retorna el usuario de base de datos actual (ORIGINAL_LOGIN o SYSTEM_USER).',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'FUNCTION', @level1name = 'fn_UsuarioActual';
GO

-- =========================================================================
-- 3. Verificacion
-- =========================================================================
SELECT
    'fn_ObtenerFechaServidor' AS funcion,
    CONVERT(VARCHAR(30), ARC.fn_ObtenerFechaServidor(), 121) AS resultado
UNION ALL
SELECT
    'fn_UsuarioActual',
    ARC.fn_UsuarioActual();
GO

PRINT 'V004__crear_funciones_helper.sql ejecutado correctamente.';
GO
