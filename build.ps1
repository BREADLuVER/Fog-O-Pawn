param(
    [string[]]$Versions = @('1.4','1.5','1.6'),
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$projectPath = "./Source/FogOfPawn/FogOfPawn.csproj"

foreach ($ver in $Versions) {
    Write-Host "Building for RimWorld $ver ..." -ForegroundColor Cyan

    # You need to set these environment variables to your local RimWorld installs
    switch ($ver) {
        '1.4' { $env:RimWorld14Dir = $env:RimWorld14Dir ?? 'C:/Program Files (x86)/Steam/steamapps/common/RimWorld' }
        '1.5' { $env:RimWorld15Dir = $env:RimWorld15Dir ?? 'C:/Program Files (x86)/Steam/steamapps/common/RimWorld 1.5' }
        '1.6' { $env:RimWorld16Dir = $env:RimWorld16Dir ?? 'C:/Program Files (x86)/Steam/steamapps/common/RimWorld 1.6' }
    }

    dotnet build $projectPath -c $Configuration -p:GameVersion=$ver
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $ver" }

    $outDir = "./Assemblies/$ver"
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null

    Copy-Item ./Source/FogOfPawn/bin/$Configuration/FogOfPawn.dll $outDir -Force
}

Write-Host "All builds completed successfully." -ForegroundColor Green 