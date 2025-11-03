# Build-Release.ps1
# Build script for XUnity-AutoInstaller - Creates a single executable file

$ErrorActionPreference = "Stop"

# Configuration
$ProjectPath = "XUnity-AutoInstaller\XUnity-AutoInstaller.csproj"
$Platform = "x64"
$Configuration = "Release"
$RuntimeIdentifier = "win-x64"
$OutputDir = "Release"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "XUnity-AutoInstaller Build Script" -ForegroundColor Cyan
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

Write-Host "Step 1/5: Cleaning previous builds..." -ForegroundColor Yellow
try {
    dotnet clean $ProjectPath -c $Configuration -p:Platform=$Platform --verbosity quiet
    Write-Host "  Clean completed successfully" -ForegroundColor Green
} catch {
    Write-Host "  WARNING: Clean failed, continuing anyway..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 2/5: Building and publishing..." -ForegroundColor Yellow
Write-Host "  Configuration: $Configuration" -ForegroundColor Gray
Write-Host "  Platform: $Platform" -ForegroundColor Gray
Write-Host "  Runtime: $RuntimeIdentifier" -ForegroundColor Gray
Write-Host "  Deployment: Framework-Dependent (Size Optimized)" -ForegroundColor Gray
Write-Host ""

try {
    dotnet publish $ProjectPath `
        -c $Configuration `
        -p:Platform=$Platform `
        -r $RuntimeIdentifier `
        --self-contained false `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=true `
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
Write-Host "Step 3/5: Preparing output directory..." -ForegroundColor Yellow

# Create Release directory if it doesn't exist
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
    Write-Host "  Created directory: $OutputDir" -ForegroundColor Green
} else {
    Write-Host "  Output directory already exists: $OutputDir" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 4/5: Copying executable..." -ForegroundColor Yellow

# Find the published exe
$PublishDir = "XUnity-AutoInstaller\bin\$Configuration\net9.0-windows10.0.26100.0\$RuntimeIdentifier\publish"
$ExeName = "XUnity-AutoInstaller.exe"
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
Write-Host "Step 5/5: Creating ZIP archive..." -ForegroundColor Yellow

# Find 7-Zip executable
$SevenZipPaths = @(
    "$env:ProgramFiles\7-Zip\7z.exe",
    "${env:ProgramFiles(x86)}\7-Zip\7z.exe"
)

$SevenZipPath = $null
foreach ($path in $SevenZipPaths) {
    if (Test-Path $path) {
        $SevenZipPath = $path
        Write-Host "  Found 7-Zip at: $path" -ForegroundColor Green
        break
    }
}

if (-not $SevenZipPath) {
    Write-Host ""
    Write-Host "ERROR: 7-Zip not found!" -ForegroundColor Red
    Write-Host "Please install 7-Zip from: https://www.7-zip.org/" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Searched locations:" -ForegroundColor Gray
    foreach ($path in $SevenZipPaths) {
        Write-Host "  - $path" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "Press Enter to close..." -ForegroundColor Yellow
    Read-Host
    exit 1
}

# Create ZIP archive
$ZipName = "XUnity-AutoInstaller-win-x64.zip"
$ZipPath = Join-Path $OutputDir $ZipName

Write-Host "  Creating archive: $ZipName" -ForegroundColor Gray

try {
    # Save current location and change to output directory
    # This ensures the ZIP contains only the exe file without folder structure
    Push-Location $OutputDir

    try {
        # Use 7-Zip to create the archive with only the exe file (no folder path)
        & "$SevenZipPath" a -tzip $ZipName $ExeName | Out-Null

        if ($LASTEXITCODE -ne 0) {
            throw "7-Zip failed with exit code $LASTEXITCODE"
        }

        Write-Host "  Archive created successfully" -ForegroundColor Green

        # Get ZIP file size
        $ZipSize = (Get-Item $ZipName).Length
        $ZipSizeMB = [math]::Round($ZipSize / 1MB, 2)
        Write-Host "  ZIP Size: $ZipSizeMB MB" -ForegroundColor Green

    } finally {
        # Restore original location
        Pop-Location
    }

} catch {
    Write-Host ""
    Write-Host "ERROR: Failed to create ZIP archive!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Press Enter to close..." -ForegroundColor Yellow
    Read-Host
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "BUILD SUCCESSFUL!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output Information:" -ForegroundColor White
Write-Host "  Executable: $DestExe" -ForegroundColor White
Write-Host "  Exe Size: $FileSizeMB MB" -ForegroundColor White
Write-Host "  ZIP Archive: $ZipPath" -ForegroundColor White
Write-Host "  ZIP Size: $ZipSizeMB MB" -ForegroundColor White
Write-Host "  Platform: $RuntimeIdentifier" -ForegroundColor White
Write-Host "  Configuration: $Configuration" -ForegroundColor White
Write-Host ""
Write-Host "You can now:" -ForegroundColor Yellow
Write-Host "  - Run the application: .\$OutputDir\$ExeName" -ForegroundColor Cyan
Write-Host "  - Distribute the ZIP: .\$OutputDir\$ZipName" -ForegroundColor Cyan
Write-Host ""
Write-Host "IMPORTANT - Runtime Requirements:" -ForegroundColor Yellow
Write-Host "  End users must have installed:" -ForegroundColor White
Write-Host "  - .NET 9.0 Desktop Runtime (x64)" -ForegroundColor White
Write-Host "    Download: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Cyan
Write-Host "  - Windows App Runtime 1.8+" -ForegroundColor White
Write-Host "    (Usually pre-installed on Windows 11, may need manual install on Windows 10)" -ForegroundColor Gray
Write-Host ""
Write-Host "Press Enter to close..." -ForegroundColor Green
Read-Host
