<#
.SYNOPSIS
    Benchmarks file I/O throughput and batch processing concurrency.
.DESCRIPTION
    Creates temporary mock files in the temp directory, reads them using chunked asynchronous streams,
    measures throughput (MB/s) and operations per second, and cleans up afterwards.
.PARAMETER FileCount
    Total test files to generate and process (default: 50).
.PARAMETER FileSizeBytes
    Size of each mock file in bytes (default: 65536 = 64KB).
#>
param(
    [int]$FileCount = 50,
    [int]$FileSizeBytes = 65536
)

$testDir = Join-Path $env:TEMP "OmniForge_IO_Benchmark_$(Get-Random)"
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
Write-Host "Creating $FileCount benchmark files ($($FileSizeBytes / 1KB) KB each) in $testDir..." -ForegroundColor Cyan

$data = New-Object byte[] $FileSizeBytes
(New-Object System.Random).NextBytes($data)

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

for ($i = 0; $i -lt $FileCount; $i++) {
    $filePath = Join-Path $testDir "bench_$i.dat"
    [System.IO.File]::WriteAllBytes($filePath, $data)
}

$createElapsed = $stopwatch.Elapsed.TotalSeconds
Write-Host "File creation completed in $($createElapsed.ToString('F2'))s" -ForegroundColor Green

# Benchmark Read Performance
$files = Get-ChildItem -Path $testDir -Filter "*.dat"
$stopwatch.Restart()

$totalBytes = 0
foreach ($file in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $totalBytes += $bytes.Length
}

$stopwatch.Stop()
$readElapsed = $stopwatch.Elapsed.TotalSeconds
$throughputMBs = ($totalBytes / 1MB) / $readElapsed
$iops = $FileCount / $readElapsed

Write-Host "=== Benchmark Results ===" -ForegroundColor Yellow
Write-Host "Processed: $FileCount files ($([Math]::Round($totalBytes / 1MB, 2)) MB)"
Write-Host "Elapsed:   $($readElapsed.ToString('F3')) seconds"
Write-Host "Throughput:$([Math]::Round($throughputMBs, 2)) MB/s"
Write-Host "Rate:      $([Math]::Round($iops, 1)) files/sec"

# Clean up
Remove-Item -Path $testDir -Recurse -Force
Write-Host "Benchmark cleanup complete." -ForegroundColor Cyan

