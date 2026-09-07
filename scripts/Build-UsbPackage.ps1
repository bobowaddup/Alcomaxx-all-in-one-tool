[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\artifacts")
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $repositoryRoot "src\AlcomaxxMaintenance\AlcomaxxMaintenance.csproj"
$publishDirectory = Join-Path $OutputDirectory "ALCOMAXX Maintenance Tool"
$zipPath = Join-Path $OutputDirectory "ALCOMAXX-Maintenance-Tool-win-x64.zip"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encontró .NET SDK. Instale .NET 8 SDK x64 y vuelva a ejecutar este script."
}

Remove-Item $publishDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
New-Item $publishDirectory -ItemType Directory -Force | Out-Null

dotnet publish $project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDirectory `
    -p:PublishSingleFile=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish terminó con el código $LASTEXITCODE."
}

$executable = Join-Path $publishDirectory "ALCOMAXX Maintenance Tool.exe"
if (-not (Test-Path $executable)) {
    throw "La publicación terminó sin crear '$executable'."
}

foreach ($folder in @("Config", "Jobs", "Reports", "Tools\BleachBit", "Tools\ZHPCleaner", "Tools\CCleaner")) {
    New-Item (Join-Path $publishDirectory $folder) -ItemType Directory -Force | Out-Null
}

Compress-Archive -Path $publishDirectory -DestinationPath $zipPath -CompressionLevel Optimal
Write-Host "Paquete USB creado correctamente:" -ForegroundColor Green
Write-Host $zipPath
