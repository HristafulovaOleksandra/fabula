param(
    [Parameter(Mandatory = $true)]
    [string]$Login
)

$ErrorActionPreference = "Stop"
$normalized = $Login.Trim().TrimStart('@').ToLowerInvariant()
if ($normalized -notmatch '^[a-z0-9_.@+-]+$') {
    throw "Нік або пошта містять недопустимі символи."
}

$sql = "UPDATE ""Users"" SET ""Role"" = 'Admin' WHERE lower(""Username"") = '$normalized' OR lower(""Email"") = '$normalized' RETURNING ""Username"";"
$result = $sql | docker exec -i fabula-postgres sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -t -A -q'
$username = $result | Where-Object { $_ -and $_.Trim() } | Select-Object -First 1
if (-not $username) {
    Write-Error "Користувача '$Login' не знайдено."
    exit 1
}

Write-Output "Користувач $username тепер адміністратор. Йому треба вийти й увійти знову."
