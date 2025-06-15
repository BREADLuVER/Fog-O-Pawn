$ErrorActionPreference = "Stop"

# Define the path to the project's source directory
$SourceDir = ".\Source"

# Build the project
Write-Host "Building project in $SourceDir..."
# The GameManagedDir parameter is no longer needed because the .csproj
# now uses relative paths to find the game assemblies.
dotnet build "$SourceDir" /p:GameVersion=16
Write-Host "Build finished."

# After build succeeds, copy the freshly-built assembly into the release folder
if ($LASTEXITCODE -eq 0) {
    $OutDir = Join-Path $SourceDir "bin\Debug\net48"
    $TargetDir = "1.6\Assemblies"

    if (-not (Test-Path $TargetDir)) {
        New-Item -Path $TargetDir -ItemType Directory | Out-Null
    }

    Write-Host "Syncing built DLL to $TargetDir ..."
    Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $TargetDir "FogOfPawn.*")
    Copy-Item -Force (Join-Path $OutDir "FogOfPawn.*") $TargetDir
    Write-Host "Assembly synced."
} 