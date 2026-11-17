<#
.SYNOPSIS
    Build script for MacShell project

.DESCRIPTION
    Builds the MacShell solution in Debug or Release configuration.
    Supports building, testing, and packaging.

.PARAMETER Configuration
    Build configuration: Debug or Release (default: Release)

.PARAMETER Target
    Build target: Build, Clean, Rebuild, Pack, Test (default: Build)

.PARAMETER Verbosity
    MSBuild verbosity: Quiet, Minimal, Normal, Detailed, Diagnostic (default: Minimal)

.EXAMPLE
    .\build.ps1 -Configuration Release -Target Build

.EXAMPLE
    .\build.ps1 -Target Clean

.EXAMPLE
    .\build.ps1 -Configuration Debug -Target Test
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('Build', 'Clean', 'Rebuild', 'Pack', 'Test')]
    [string]$Target = 'Build',

    [ValidateSet('Quiet', 'Minimal', 'Normal', 'Detailed', 'Diagnostic')]
    [string]$Verbosity = 'Minimal',

    [switch]$NoRestore,
    [switch]$NoLogo
)

# Set strict mode
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$SolutionFile = Join-Path $ScriptDir 'MacShell.sln'
$ArtifactsDir = Join-Path $ScriptDir 'artifacts'

# Colors for output
$Green = [ConsoleColor]::Green
$Yellow = [ConsoleColor]::Yellow
$Red = [ConsoleColor]::Red
$Cyan = [ConsoleColor]::Cyan

function Write-Info { param([string]$Message) Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor $Cyan }
function Write-Success { param([string]$Message) Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor $Green }
function Write-Warning { param([string]$Message) Write-Host "[$(Get-Date -Format 'HH:mm:ss')] WARNING: $Message" -ForegroundColor $Yellow }
function Write-Error { param([string]$Message) Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ERROR: $Message" -ForegroundColor $Red }

# Check if solution exists
if (-not (Test-Path $SolutionFile)) {
    Write-Error "Solution file not found: $SolutionFile"
    exit 1
}

# Build MSBuild arguments
$MsBuildArgs = @(
    $SolutionFile,
    "/t:$Target",
    "/p:Configuration=$Configuration",
    "/v:$Verbosity",
    "/nologo"
)

if ($NoRestore) {
    $MsBuildArgs += "/p:RestorePackagesConfig=false"
}

# Add platform for Release
if ($Configuration -eq 'Release') {
    $MsBuildArgs += "/p:Platform=x64"
}

Write-Info "Building MacShell..."
Write-Info "Configuration: $Configuration"
Write-Info "Target: $Target"
Write-Info "Solution: $SolutionFile"

# Run MSBuild
try {
    $result = & dotnet msbuild @MsBuildArgs
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Build completed successfully!"

        # Show output location for Release builds
        if ($Configuration -eq 'Release' -and $Target -in @('Build', 'Rebuild')) {
            $OutputDir = Join-Path $ArtifactsDir "bin\$Configuration"
            if (Test-Path $OutputDir) {
                Write-Info "Output directory: $OutputDir"

                # Find the main executable
                $ExePath = Get-ChildItem -Path $OutputDir -Filter 'MacShell.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
                if ($ExePath) {
                    Write-Success "Executable: $($ExePath.FullName)"
                }
            }
        }
    } else {
        Write-Error "Build failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}
catch {
    Write-Error "Build failed: $_"
    exit 1
}