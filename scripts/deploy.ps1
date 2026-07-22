#Requires -Version 5.1
<#
.SYNOPSIS
    Ejecuta las migraciones de base de datos en orden para BD_API_FILE.
.DESCRIPTION
    Este script conecta a SQL Server y ejecuta secuencialmente todos los
    scripts de migracion en orden numerico (V001, V002, ...).
    Registra cada migracion en ARC.API_FILE_TC_MIGRACIONES.
.PARAMETER Server
    Nombre del servidor SQL Server. Default: localhost.
.PARAMETER Database
    Nombre de la base de datos. Default: BD_API_FILE.
.PARAMETER Username
    Usuario de SQL Server. Si no se especifica, usa autenticacion de Windows.
.PARAMETER Password
    Contrasena de SQL Server. Requerido si se especifica -Username.
.PARAMETER DataPath
    Ruta para archivos de datos MDF. Default: la configuracion actual del servidor.
.PARAMETER LogPath
    Ruta para archivos de log LDF. Default: la configuracion actual del servidor.
.EXAMPLE
    .\deploy.ps1 -Server localhost -Username sa -Password "MiPassword123!"
.EXAMPLE
    .\deploy.ps1 -Server localhost -Database BD_API_FILE
#>

param(
    [string]$Server = "localhost",
    [string]$Database = "BD_API_FILE",
    [string]$Username,
    [string]$Password,
    [string]$DataPath,
    [string]$LogPath
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# ============================================================================
# Configuracion
# ============================================================================
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path
$MIGRATIONS_DIR = Join-Path $SCRIPT_DIR "..\data\migrations"
$DEPLOY_LOG = Join-Path $SCRIPT_DIR "logs\deploy_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

# Crear directorio de logs
$logDir = Split-Path -Parent $DEPLOY_LOG
if (!(Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    Write-Host $logEntry
    Add-Content -Path $DEPLOY_LOG -Value $logEntry -Encoding UTF8
}

function Get-SqlConnection {
    $connectionString = "Server=$Server;Database=$Database;TrustServerCertificate=True"
    if ($Username) {
        $connectionString += ";User Id=$Username;Password=$Password"
    } else {
        $connectionString += ";Integrated Security=True"
    }
    return New-Object System.Data.SqlClient.SqlConnection $connectionString
}

function Invoke-SqlScript {
    param(
        [string]$FilePath,
        [string]$Database,
        [hashtable]$Params = @{}
    )

    $fileName = Split-Path -Leaf $FilePath
    Write-Log "Ejecutando: $fileName"

    $sqlcmdArgs = @(
        "-S", $Server,
        "-d", $Database,
        "-i", $FilePath,
        "-E",
        "-b"
    )

    if ($Username) {
        $sqlcmdArgs = @(
            "-S", $Server,
            "-d", $Database,
            "-U", $Username,
            "-P", $Password,
            "-i", $FilePath,
            "-b"
        )
    }

    # Reemplazar variables en el script
    $content = Get-Content $FilePath -Raw -Encoding UTF8
    if ($DataPath) {
        $content = $content -replace '\$\(DATA_PATH\)', $DataPath
    }
    if ($LogPath) {
        $content = $content -replace '\$\(LOG_PATH\)', $LogPath
    }

    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    Set-Content -Path $tempFile -Value $content -Encoding UTF8

    try {
        $result = & sqlcmd @sqlcmdArgs 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Log "ERROR en $fileName : $result" "ERROR"
            throw "Script $fileName fallo con codigo $LASTEXITCODE"
        }
        Write-Log "OK: $fileName" "INFO"
        return $true
    }
    finally {
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
}

function Test-MigrationApplied {
    param([string]$Version)

    $connection = Get-SqlConnection
    try {
        $connection.Open()
        $cmd = $connection.CreateCommand()
        $cmd.CommandText = "SELECT COUNT(1) FROM ARC.API_FILE_TC_MIGRACIONES WHERE txtVersion = @v AND txtEstado = 'APLICADA'"
        $param = $cmd.Parameters.AddWithValue("@v", $Version)
        $count = [int]$cmd.ExecuteScalar()
        return $count -gt 0
    }
    catch {
        # Tabla aun no existe o hay error - no aplicar
        return $false
    }
    finally {
        $connection.Close()
    }
}

# ============================================================================
# Inicio
# ============================================================================
Write-Log "============================================"
Write-Log "INICIO DE DEPLOY - BD_API_FILE"
Write-Log "Servidor: $Server"
Write-Log "Base de datos: $Database"
Write-Log "============================================"

# Verificar que existe el directorio de migraciones
if (!(Test-Path $MIGRATIONS_DIR)) {
    Write-Log "ERROR: Directorio de migraciones no encontrado: $MIGRATIONS_DIR" "ERROR"
    exit 1
}

# Obtener scripts de migracion (excluir V000 legacy y README)
$scripts = Get-ChildItem -Path $MIGRATIONS_DIR -Filter "V*.sql" |
    Where-Object { $_.Name -notlike "*legacy*" -and $_.Name -ne "MIGRATIONS_README.md" } |
    Sort-Object { [regex]::Match($_.Name, 'V(\d+)').Groups[1].Value } |
    Where-Object { [int]([regex]::Match($_.Name, 'V(\d+)').Groups[1].Value) -ge 1 }

Write-Log "Scripts encontrados: $($scripts.Count)"

$applied = 0
$failed = 0

foreach ($script in $scripts) {
    $version = [regex]::Match($script.Name, '(V\d+)').Groups[1].Value

    # Saltar si la migracion ya fue aplicada
    if ((Test-MigrationApplied -Version $version)) {
        Write-Log "SALTADO (ya aplicada): $version"
        continue
    }

    try {
        $result = Invoke-SqlScript -FilePath $script.FullName -Database $Database
        if ($result) {
            $applied++
        }
    }
    catch {
        Write-Log "FALLO: $($script.Name) - $_" "ERROR"
        $failed++
        break
    }
}

# ============================================================================
# Resumen
# ============================================================================
Write-Log "============================================"
Write-Log "FIN DE DEPLOY"
Write-Log "Aplicadas: $applied"
Write-Log "Fallidas: $failed"
Write-Log "Log: $DEPLOY_LOG"
Write-Log "============================================"

if ($failed -gt 0) {
    exit 1
}
exit 0
