param(
    [Parameter(Mandatory = $false)]
    [string]$BaseUrl = "https://localhost:7286",

    [Parameter(Mandatory = $false)]
    [int]$RowId = 123
)

$ErrorActionPreference = "Stop"

$endpoint = "$BaseUrl/ingest/v1/stream"

$line = @{ table = "observation"; installationId = "00000000-0000-0000-0000-000000000000"; rowid = $RowId; id = "00000000-0000-0000-0000-000000000000"; application_received_utc_timestampz = 0; payload = @{ hello = "world" } } | ConvertTo-Json -Compress
$ndjson = "$line`n"

$ms = New-Object System.IO.MemoryStream
$gzip = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionMode]::Compress, $true)
$sw = New-Object System.IO.StreamWriter($gzip, New-Object System.Text.UTF8Encoding($false))
$sw.Write($ndjson)
$sw.Flush()
$gzip.Flush()
$gzip.Dispose()
$bytes = $ms.ToArray()

$headers = @{ "Content-Encoding" = "gzip" }

Write-Host "POST $endpoint"
$response = Invoke-RestMethod -Method Post -Uri $endpoint -ContentType "application/x-ndjson" -Headers $headers -Body $bytes
$response | ConvertTo-Json -Depth 6
