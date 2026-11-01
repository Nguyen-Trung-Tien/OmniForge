[CmdletBinding()]
param(
    [int]$SizeBytes = 10485760 # 10 MB
)

Write-Host "[Hash Engine Benchmark] Generating $SizeBytes bytes of test data..." -ForegroundColor Cyan
$TempFile = [System.IO.Path]::GetTempFileName()

try {
    $Bytes = New-Object byte[] $SizeBytes
    (New-Object System.Random).NextBytes($Bytes)
    [System.IO.File]::WriteAllBytes($TempFile, $Bytes)

    $Algorithms = @("SHA256", "SHA512", "MD5")
    foreach ($Alg in $Algorithms) {
        $Sw = [System.Diagnostics.Stopwatch]::StartNew()
        $Stream = [System.IO.File]::OpenRead($TempFile)
        $Hasher = [System.Security.Cryptography.HashAlgorithm]::Create($Alg)
        $Hash = $Hasher.ComputeHash($Stream)
        $Stream.Dispose()
        $Hasher.Dispose()
        $Sw.Stop()

        $MbSec = [Math]::Round(($SizeBytes / (1024 * 1024)) / ($Sw.ElapsedMilliseconds / 1000.0), 2)
        $Hex = [Convert]::ToHexString($Hash).ToLowerInvariant()
        Write-Host "[$Alg] Duration: $($Sw.ElapsedMilliseconds) ms ($MbSec MB/s) | Digest: $Hex" -ForegroundColor Green
    }
} finally {
    if (Test-Path $TempFile) {
        Remove-Item -Path $TempFile -Force
    }
}

