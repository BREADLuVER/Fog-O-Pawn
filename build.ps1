$ErrorActionPreference = "Stop"

# Define the path to the RimWorld Managed directory
$RimWorldManagedDir = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed"

# Define the path to the project's source directory
$SourceDir = ".\Source"

# Build the project
Write-Host "Building project in $SourceDir..."
dotnet build "$SourceDir" /p:GameVersion=16 /p:GameManagedDir="$RimWorldManagedDir"
Write-Host "Build finished." 