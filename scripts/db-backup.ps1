param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\backups")
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm"
$target = Join-Path (Resolve-Path $OutputDirectory) "fabula_$timestamp.dump"

docker exec fabula-postgres sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f /tmp/fabula.dump'
docker cp fabula-postgres:/tmp/fabula.dump $target
docker exec fabula-postgres rm /tmp/fabula.dump

Write-Output "Резервну копію збережено: $target"
