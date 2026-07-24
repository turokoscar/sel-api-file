-- =========================================================================
-- MIDAGRI - Ministerio de Desarrollo Agrario y Riego
-- Sistema: SEL (Sistema de Elegibilidad)
-- Modulo: API_FILE (Archivos)
-- Script: V003__crear_indices.sql
-- Proposito: Crear indices complementarios para оптимизацию de consultas.
-- Autor: Equipo SEL
-- Fecha: 2026-07-21
-- Version: 1.0.0
-- Dependencias: V002__crear_esquema_y_tablas.sql
-- -------------------------------------------------------------------------
-- INDICES CREADOS:
--   TMM_ARCHIVO (6 indices):
--     IDX_01: codSistema, codProceso  (busqueda por sistema/proceso)
--     IDX_02: fecCreacion             (consultas por fecha)
--     IDX_03: txtChecksumSHA256       (verificacion de duplicados, filtro activo)
--     IDX_04: ideProveedor, estRegistro (archivos por proveedor, activos)
--   TC_AUDITORIA (2 indices):
--     IDX_01: ideArchivo, fecAcceso    (auditoria por archivo)
--     IDX_02: txtUsuario, fecAcceso   (auditoria por usuario)
-- =========================================================================

USE BD_API_FILE;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =========================================================================
-- Indice 01: TMM_ARCHIVO - codSistema, codProceso
-- Uso: Listar archivos por sistema y proceso (endpoint GET /archivos)
-- =========================================================================
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'API_FILE_ARCHIVO_IDX_01'
      AND object_id = OBJECT_ID('ARC.API_FILE_TMM_ARCHIVO')
)
BEGIN
    DROP INDEX API_FILE_ARCHIVO_IDX_01 ON ARC.API_FILE_TMM_ARCHIVO;
END;
GO

CREATE NONCLUSTERED INDEX API_FILE_ARCHIVO_IDX_01
ON ARC.API_FILE_TMM_ARCHIVO (codSistema ASC, codProceso ASC)
INCLUDE (ideArchivo, txtNombreOriginal, txtExtension, canTamanioBytes, txtContentType, fecCreacion)
WITH (FILLFACTOR = 90, ONLINE = ON);
GO

-- =========================================================================
-- Indice 02: TMM_ARCHIVO - fecCreacion
-- Uso: Consultas por rango de fechas, reportes, limpieza de archivos
-- =========================================================================
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'API_FILE_ARCHIVO_IDX_02'
      AND object_id = OBJECT_ID('ARC.API_FILE_TMM_ARCHIVO')
)
BEGIN
    DROP INDEX API_FILE_ARCHIVO_IDX_02 ON ARC.API_FILE_TMM_ARCHIVO;
END;
GO

CREATE NONCLUSTERED INDEX API_FILE_ARCHIVO_IDX_02
ON ARC.API_FILE_TMM_ARCHIVO (fecCreacion ASC)
INCLUDE (codSistema, txtNombreOriginal, canTamanioBytes)
WITH (FILLFACTOR = 90, ONLINE = ON);
GO

-- =========================================================================
-- Indice 03: TMM_ARCHIVO - txtChecksumSHA256
-- Uso: Verificacion de archivos duplicados por hash SHA256
-- Nota: Filtro solo activos para mejor rendimiento en busqueda de duplicados
-- =========================================================================
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'API_FILE_ARCHIVO_IDX_03'
      AND object_id = OBJECT_ID('ARC.API_FILE_TMM_ARCHIVO')
)
BEGIN
    DROP INDEX API_FILE_ARCHIVO_IDX_03 ON ARC.API_FILE_TMM_ARCHIVO;
END;
GO

CREATE UNIQUE NONCLUSTERED INDEX API_FILE_ARCHIVO_IDX_03
ON ARC.API_FILE_TMM_ARCHIVO (txtChecksumSHA256 ASC)
WHERE estRegistro = 1 AND estArchivo = 1;
GO

-- =========================================================================
-- Indice 04: TMM_ARCHIVO - ideProveedor, estRegistro
-- Uso: Listar archivos por proveedor, filtrar activos/inactivos
-- =========================================================================
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'API_FILE_ARCHIVO_IDX_04'
      AND object_id = OBJECT_ID('ARC.API_FILE_TMM_ARCHIVO')
)
BEGIN
    DROP INDEX API_FILE_ARCHIVO_IDX_04 ON ARC.API_FILE_TMM_ARCHIVO;
END;
GO

CREATE NONCLUSTERED INDEX API_FILE_ARCHIVO_IDX_04
ON ARC.API_FILE_TMM_ARCHIVO (ideProveedor ASC, estRegistro ASC)
INCLUDE (codSistema, txtNombreOriginal)
WITH (FILLFACTOR = 90, ONLINE = ON);
GO

-- =========================================================================
-- Indice 05: TC_AUDITORIA - ideArchivo, fecAcceso
-- Uso: Auditoria por archivo, historial de accesos a un archivo
-- =========================================================================
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'API_FILE_AUDITORIA_IDX_01'
      AND object_id = OBJECT_ID('ARC.API_FILE_TC_AUDITORIA')
)
BEGIN
    DROP INDEX API_FILE_AUDITORIA_IDX_01 ON ARC.API_FILE_TC_AUDITORIA;
END;
GO

CREATE NONCLUSTERED INDEX API_FILE_AUDITORIA_IDX_01
ON ARC.API_FILE_TC_AUDITORIA (ideArchivo ASC, fecAcceso DESC)
INCLUDE (txtAccion, txtUsuario, txtIpOrigen)
WITH (FILLFACTOR = 90, ONLINE = ON);
GO

-- =========================================================================
-- Indice 06: TC_AUDITORIA - txtUsuario, fecAcceso
-- Uso: Auditoria por usuario, reportes de actividad por usuario
-- =========================================================================
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'API_FILE_AUDITORIA_IDX_02'
      AND object_id = OBJECT_ID('ARC.API_FILE_TC_AUDITORIA')
)
BEGIN
    DROP INDEX API_FILE_AUDITORIA_IDX_02 ON ARC.API_FILE_TC_AUDITORIA;
END;
GO

CREATE NONCLUSTERED INDEX API_FILE_AUDITORIA_IDX_02
ON ARC.API_FILE_TC_AUDITORIA (txtUsuario ASC, fecAcceso DESC)
INCLUDE (txtAccion, ideArchivo, txtIpOrigen)
WITH (FILLFACTOR = 90, ONLINE = ON);
GO

-- =========================================================================
-- Verificacion: listar todos los indices creados
-- =========================================================================
SELECT
    i.name              AS index_name,
    OBJECT_NAME(i.object_id) AS table_name,
    i.type_desc         AS index_type,
    i.is_unique,
    i.is_primary_key,
    STUFF((
        SELECT ', ' + COL_NAME(c.object_id, ic.column_id)
        FROM sys.index_columns ic
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
        FOR XML PATH('')
    ), 1, 2, '') AS columns
FROM sys.indexes i
WHERE OBJECT_NAME(i.object_id) LIKE 'API_FILE_%'
  AND i.type > 0
ORDER BY OBJECT_NAME(i.object_id), i.index_id;
GO

PRINT 'V003__crear_indices.sql ejecutado correctamente.';
GO
