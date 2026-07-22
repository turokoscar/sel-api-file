-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V011__crear_synonyms.sql
-- Proposito: Crear synonyms en BD_SEL_DEV hacia BD_API_FILE para mantener
--            compatibilidad hacia atras con sistemas que referencian la BD
--            compartida original.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V002__crear_esquema_y_tablas.sql a V009__sp_r_proveedores.sql
-- -------------------------------------------------------------------------
-- NOTA: Este script es OPCIONAL. Solo ejecutar si existen aplicaciones
--       consumidoras que acceden a los objetos via BD_SEL_DEV y no es
--       posible actualizar sus cadenas de conexion de forma inmediata.
--       Una vez migradas las apps, eliminar este script y los synonyms.
-- =========================================================================

USE BD_SEL_DEV;
GO

SET NOCOUNT ON;
PRINT 'Iniciando V011__crear_synonyms.sql (BD_SEL_DEV)';
GO

-- =========================================================================
-- Synonyms para tablas
-- =========================================================================

IF EXISTS (SELECT 1 FROM sys.synonyms WHERE name = 'SEL_ARC_TG_PROVEEDOR')
BEGIN
    DROP SYNONYM dbo.SEL_ARC_TG_PROVEEDOR;
END
CREATE SYNONYM dbo.SEL_ARC_TG_PROVEEDOR
FOR BD_API_FILE.ARC.API_FILE_TG_PROVEEDOR;
PRINT 'Synonym dbo.SEL_ARC_TG_PROVEEDOR creado.';
GO

IF EXISTS (SELECT 1 FROM sys.synonyms WHERE name = 'SEL_ARC_TMM_ARCHIVO')
BEGIN
    DROP SYNONYM dbo.SEL_ARC_TMM_ARCHIVO;
END
CREATE SYNONYM dbo.SEL_ARC_TMM_ARCHIVO
FOR BD_API_FILE.ARC.API_FILE_TMM_ARCHIVO;
PRINT 'Synonym dbo.SEL_ARC_TMM_ARCHIVO creado.';
GO

IF EXISTS (SELECT 1 FROM sys.synonyms WHERE name = 'SEL_ARC_TC_AUDITORIA')
BEGIN
    DROP SYNONYM dbo.SEL_ARC_TC_AUDITORIA;
END
CREATE SYNONYM dbo.SEL_ARC_TC_AUDITORIA
FOR BD_API_FILE.ARC.API_FILE_TC_AUDITORIA;
PRINT 'Synonym dbo.SEL_ARC_TC_AUDITORIA creado.';
GO

-- =========================================================================
-- Synonyms para Stored Procedures
-- =========================================================================

IF EXISTS (SELECT 1 FROM sys.synonyms WHERE name = 'SEL_ARC_SP_C_ARCHIVO')
BEGIN
    DROP SYNONYM dbo.SEL_ARC_SP_C_ARCHIVO;
END
CREATE SYNONYM dbo.SEL_ARC_SP_C_ARCHIVO
FOR BD_API_FILE.ARC.API_FILE_SP_C_ARCHIVO;
PRINT 'Synonym dbo.SEL_ARC_SP_C_ARCHIVO creado.';
GO

IF EXISTS (SELECT 1 FROM sys.synonyms WHERE name = 'SEL_ARC_SP_R_ARCHIVO')
BEGIN
    DROP SYNONYM dbo.SEL_ARC_SP_R_ARCHIVO;
END
CREATE SYNONYM dbo.SEL_ARC_SP_R_ARCHIVO
FOR BD_API_FILE.ARC.API_FILE_SP_R_ARCHIVO;
PRINT 'Synonym dbo.SEL_ARC_SP_R_ARCHIVO creado.';
GO

IF EXISTS (SELECT 1 FROM sys.synonyms WHERE name = 'SEL_ARC_SP_U_ARCHIVO')
BEGIN
    DROP SYNONYM dbo.SEL_ARC_SP_U_ARCHIVO;
END
CREATE SYNONYM dbo.SEL_ARC_SP_U_ARCHIVO
FOR BD_API_FILE.ARC.API_FILE_SP_U_ARCHIVO;
PRINT 'Synonym dbo.SEL_ARC_SP_U_ARCHIVO creado.';
GO

IF EXISTS (SELECT 1 FROM sys.synonyms WHERE name = 'SEL_ARC_SP_D_ARCHIVO')
BEGIN
    DROP SYNONYM dbo.SEL_ARC_SP_D_ARCHIVO;
END
CREATE SYNONYM dbo.SEL_ARC_SP_D_ARCHIVO
FOR BD_API_FILE.ARC.API_FILE_SP_D_ARCHIVO;
PRINT 'Synonym dbo.SEL_ARC_SP_D_ARCHIVO creado.';
GO

IF EXISTS (SELECT 1 FROM sys.synonyms WHERE name = 'SEL_ARC_SP_C_AUDITORIA')
BEGIN
    DROP SYNONYM dbo.SEL_ARC_SP_C_AUDITORIA;
END
CREATE SYNONYM dbo.SEL_ARC_SP_C_AUDITORIA
FOR BD_API_FILE.ARC.API_FILE_SP_C_AUDITORIA;
PRINT 'Synonym dbo.SEL_ARC_SP_C_AUDITORIA creado.';
GO

IF EXISTS (SELECT 1 FROM sys.synonyms WHERE name = 'SEL_ARC_SP_R_PROVEEDORES')
BEGIN
    DROP SYNONYM dbo.SEL_ARC_SP_R_PROVEEDORES;
END
CREATE SYNONYM dbo.SEL_ARC_SP_R_PROVEEDORES
FOR BD_API_FILE.ARC.API_FILE_SP_R_PROVEEDORES;
PRINT 'Synonym dbo.SEL_ARC_SP_R_PROVEEDORES creado.';
GO

-- =========================================================================
-- Verificacion
-- =========================================================================
SELECT
    name       AS synonym_name,
    base_object_name AS objeto_referenciado
FROM sys.synonyms
WHERE name LIKE 'SEL_ARC_%'
ORDER BY name;
GO

PRINT 'V011__crear_synonyms.sql ejecutado correctamente.';
GO
