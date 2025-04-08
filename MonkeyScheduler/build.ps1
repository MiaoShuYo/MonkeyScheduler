# Set code page to UTF-8
chcp 65001 | Out-Null

# Set output encoding to UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# Check if README.md exists
if (-not (Test-Path "README.md")) {
    Write-Error "README.md file not found. Please ensure the file is in the correct location."
    exit 1
}

# Clean previous build
Write-Output "Starting build cleanup..."
dotnet clean MonkeyScheduler.csproj

# Restore NuGet packages
Write-Output "`nRestoring NuGet packages..."
dotnet restore MonkeyScheduler.csproj

# Build project
Write-Output "`nBuilding project..."
dotnet build MonkeyScheduler.csproj -c Release

# Pack
Write-Output "`nCreating NuGet package..."
dotnet pack MonkeyScheduler.csproj -c Release

# Output package location
Write-Output "`nNuGet package generated at:"
$packagePath = Get-ChildItem -Path "bin/Release" -Filter "*.nupkg" | Select-Object -First 1 -ExpandProperty FullName
Write-Output $packagePath

# Verify package contents
if ($packagePath) {
    Write-Output "`nVerifying package contents..."
    $tempDir = Join-Path $env:TEMP "MonkeySchedulerCheck"
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $tempDir | Out-Null
    
    try {
        # Copy .nupkg to .zip
        $zipPath = Join-Path $tempDir "package.zip"
        Copy-Item -Path $packagePath -Destination $zipPath
        
        # Extract
        $extractPath = Join-Path $tempDir "extracted"
        Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force
        
        # Check for README.md
        $readmeExists = Get-ChildItem -Path $extractPath -Recurse -Filter "Readme.md"
        
        if ($readmeExists) {
            Write-Output "Readme.md successfully included in package"
        } else {
            Write-Error "Readme.md not found in package"
        }
    }
    catch {
        Write-Error "Error while verifying package: $_"
    }
    finally {
        if (Test-Path $tempDir) {
            Remove-Item -Path $tempDir -Recurse -Force
        }
    }
} 