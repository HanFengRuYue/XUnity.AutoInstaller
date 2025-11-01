# Build-Release.ps1
# Build script for XUnity.AutoInstaller - Creates a single executable file

$ErrorActionPreference = "Stop"

# Configuration
$ProjectPath = "XUnity.AutoInstaller\XUnity.AutoInstaller.csproj"
$Platform = "x64"
$Configuration = "Release"
$RuntimeIdentifier = "win-x64"
$OutputDir = "Release"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "XUnity.AutoInstaller Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify dotnet is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: dotnet CLI not found. Please install .NET SDK." -ForegroundColor Red
    Write-Host ""
    Write-Host "Press Enter to close..." -ForegroundColor Yellow
    Read-Host
    exit 1
}

# Verify project file exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "ERROR: Project file not found at: $ProjectPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Press Enter to close..." -ForegroundColor Yellow
    Read-Host
    exit 1
}

Write-Host "Step 1/4: Cleaning previous builds..." -ForegroundColor Yellow
try {
    dotnet clean $ProjectPath -c $Configuration -p:Platform=$Platform --verbosity quiet
    Write-Host "  Clean completed successfully" -ForegroundColor Green
} catch {
    Write-Host "  WARNING: Clean failed, continuing anyway..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 2/4: Building and publishing..." -ForegroundColor Yellow
Write-Host "  Configuration: $Configuration" -ForegroundColor Gray
Write-Host "  Platform: $Platform" -ForegroundColor Gray
Write-Host "  Runtime: $RuntimeIdentifier" -ForegroundColor Gray
Write-Host ""

try {
    dotnet publish $ProjectPath `
        -c $Configuration `
        -p:Platform=$Platform `
        -r $RuntimeIdentifier `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:PublishReadyToRun=true `
        -p:PublishTrimmed=true

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "  Build completed successfully" -ForegroundColor Green
} catch {
    Write-Host ""
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Press Enter to close..." -ForegroundColor Yellow
    Read-Host
    exit 1
}

Write-Host ""
Write-Host "Step 3/4: Preparing output directory..." -ForegroundColor Yellow

# Create Release directory if it doesn't exist
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
    Write-Host "  Created directory: $OutputDir" -ForegroundColor Green
} else {
    Write-Host "  Output directory already exists: $OutputDir" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 4/4: Copying executable..." -ForegroundColor Yellow

# Find the published exe
$PublishDir = "XUnity.AutoInstaller\bin\$Configuration\net9.0-windows10.0.26100.0\$RuntimeIdentifier\publish"
$ExeName = "XUnity.AutoInstaller.exe"
$SourceExe = Join-Path $PublishDir $ExeName

if (-not (Test-Path $SourceExe)) {
    Write-Host "ERROR: Published executable not found at: $SourceExe" -ForegroundColor Red
    Write-Host ""
    Write-Host "Press Enter to close..." -ForegroundColor Yellow
    Read-Host
    exit 1
}

# Copy to Release directory
$DestExe = Join-Path $OutputDir $ExeName
Copy-Item $SourceExe $DestExe -Force

Write-Host "  Copied: $ExeName" -ForegroundColor Green
Write-Host "  Destination: $DestExe" -ForegroundColor Green

# Get file size
$FileSize = (Get-Item $DestExe).Length
$FileSizeMB = [math]::Round($FileSize / 1MB, 2)

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "BUILD SUCCESSFUL!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output Information:" -ForegroundColor White
Write-Host "  Executable: $DestExe" -ForegroundColor White
Write-Host "  File Size: $FileSizeMB MB" -ForegroundColor White
Write-Host "  Platform: $RuntimeIdentifier" -ForegroundColor White
Write-Host "  Configuration: $Configuration" -ForegroundColor White
Write-Host ""
Write-Host "You can now run the application from:" -ForegroundColor Yellow
Write-Host "  .\$OutputDir\$ExeName" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Enter to close..." -ForegroundColor Green
Read-Host
