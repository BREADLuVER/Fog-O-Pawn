param(
    [ValidateSet("15","16")]
    [string]$GameVersion = "15",

    [string]$RimWorldDir
)

if (-not $RimWorldDir) {
    Write-Error "You must pass -RimWorldDir <path to RimWorld folder>"
    exit 1
}

$managedDir = Join-Path $RimWorldDir "RimWorldWin64_Data\Managed"
if (-not (Test-Path (Join-Path $managedDir 'Assembly-CSharp.dll'))) {
    Write-Error "Assembly-CSharp.dll not found in $managedDir. Verify RimWorldDir."
    exit 1
}

$assembliesDir = Join-Path (Resolve-Path .) "$GameVersion/Assemblies"
if (-not (Test-Path $assembliesDir)) { New-Item -ItemType Directory -Path $assembliesDir | Out-Null }

# Build via dotnet passing game managed dir path
$props = @(
    "-p:GameVersion=$GameVersion",
    "-p:GameManagedDir=$managedDir",
    "-p:OutDir=$assembliesDir",
    "--configuration", "Release",
    "--no-incremental"
)

dotnet build .\Source\FogOfPawn.csproj @props 