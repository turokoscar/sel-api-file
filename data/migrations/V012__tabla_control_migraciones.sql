-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V012__tabla_control_migraciones.sql
-- Proposito: Crear tabla de control para registrar las migraciones
--            aplicadas y evitar ejecuciones duplicadas o fuera de orden.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V002__crear_esquema_y_tablas.sql
-- -------------------------------------------------------------------------
-- NOTA: Esta tabla es automaticamente actualizada por deploy.ps1.
--       No ejecutar manualmente en produccion.
-- =========================================================================

USE BD_API_FILE;
GO

-- =========================================================================
-- Tabla de control de migraciones
-- =========================================================================
IF OBJECT_ID('ARC.API_FILE_TC_MIGRACIONES', 'U') IS NOT NULL
BEGIN
    DROP TABLE ARC.API_FILE_TC_MIGRACIONES;
END;
GO

CREATE TABLE ARC.API_FILE_TC_MIGRACIONES (
    ideMigracion     INT             IDENTITY(1,1) NOT NULL,
    txtVersion       VARCHAR(10)     NOT NULL,  -- V001, V002, etc.
    txtScript        VARCHAR(255)    NOT NULL,
    txtDescripcion   VARCHAR(500)    NOT NULL,
    txtUsuario       VARCHAR(100)    NOT NULL,
    fecEjecucion     DATETIME        NOT NULL DEFAULT GETDATE(),
    txtEstado        VARCHAR(20)     NOT NULL DEFAULT 'APLICADA',
    txtObservaciones VARCHAR(500)    NULL,
    CONSTRAINT API_FILE_TC_MIGRACIONES_PK PRIMARY KEY (ideMigracion),
    CONSTRAINT API_FILE_TC_MIGRACIONES_AK UNIQUE (txtVersion)
);
GO

-- Indices
CREATE NONCLUSTERED INDEX API_FILE_MIGRACIONES_IDX_01
ON ARC.API_FILE_TC_MIGRACIONES (fecEjecucion DESC);
GO

-- Comentarios
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Tabla de control de migraciones de base de datos.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TC_MIGRACIONES';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Version del script (V001, V002, etc.)',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TC_MIGRACIONES',
    @level2type = N'COLUMN', @level2name = 'txtVersion';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Usuario de SQL Server que ejecuto la migracion.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TC_MIGRACIONES',
    @level2type = N'COLUMN', @level2name = 'txtUsuario';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Estado: APLICADA, FALLIDA, REVERTIDA.',
    @level0type = N'SCHEMA', @level0name = 'ARC',
    @level1type = N'TABLE',  @level1name = 'API_FILE_TC_MIGRACIONES',
    @level2type = N'COLUMN', @level2name = 'txtEstado';
GO

-- =========================================================================
-- Insertar registro de migraciones ya aplicadas
-- =========================================================================
INSERT INTO ARC.API_FILE_TC_MIGRACIONES (txtVersion, txtScript, txtDescripcion, txtUsuario, fecEjecucion, txtEstado)
VALUES
('V001', 'V001__crear_base_datos.sql',       'CREATE DATABASE BD_API_FILE + filegroups',             SYSTEM_USER, GETDATE(), 'APLICADA'),
('V002', 'V002__crear_esquema_y_tablas.sql',  'CREATE SCHEMA ARC + 3 tablas + datos iniciales',        SYSTEM_USER, GETDATE(), 'APLICADA'),
('V003', 'V003__crear_indices.sql',           'CREATE 6 indices complementarios',                      SYSTEM_USER, GETDATE(), 'APLICADA'),
('V004', 'V004__crear_funciones_helper.sql',  'CREATE fn_ObtenerFechaServidor, fn_UsuarioActual',      SYSTEM_USER, GETDATE(), 'APLICADA'),
('V005', 'V005__sp_archivo.sql',              'SP_C_ARCHIVO, SP_R_ARCHIVO',                           SYSTEM_USER, GETDATE(), 'APLICADA'),
('V006', 'V006__sp_u_d_archivo.sql',          'SP_U_ARCHIVO, SP_D_ARCHIVO',                          SYSTEM_USER, GETDATE(), 'APLICADA'),
('V007', 'V007__sp_c_auditoria.sql',          'SP_C_AUDITORIA con txtEquipoOrigen',                   SYSTEM_USER, GETDATE(), 'APLICADA'),
('V008', 'V008__sp_read_adicionales.sql',      'SP_R_ARCHIVOS_POR_SISTEMA, SP_R_ARCHIVO_POR_CHECKSUM',SYSTEM_USER, GETDATE(), 'APLICADA'),
('V009', 'V009__sp_r_proveedores.sql',        'SP_R_PROVEEDORES, SP_R_PROVEEDOR_POR_ID',            SYSTEM_USER, GETDATE(), 'APLICADA'),
('V010', 'V010__crear_usuarios_y_roles.sql',  'Usuarios y roles de base de datos',                    SYSTEM_USER, GETDATE(), 'APLICADA');
GO

PRINT 'V012__tabla_control_migraciones.sql ejecutado correctamente.';
GO
