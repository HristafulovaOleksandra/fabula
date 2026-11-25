param(
    [Parameter(Mandatory = $true)][string]$BackupFile
)

$ErrorActionPreference = "Stop"
$source = Resolve-Path $BackupFile

docker cp $source fabula-postgres:/tmp/fabula.dump
docker exec fabula-postgres sh -c 'pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists --no-owner /tmp/fabula.dump'
docker exec fabula-postgres rm /tmp/fabula.dump

Write-Output "Базу відновлено з: $source"
