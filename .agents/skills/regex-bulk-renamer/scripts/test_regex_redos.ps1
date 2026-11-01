[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Pattern,
    [string]$InputString = "aaaaaaaaaaaaaaaaaaaaaaaaaaaa!"
)

Write-Host "[ReDoS Guard Test] Testing pattern '$Pattern' with 250ms timeout..." -ForegroundColor Cyan

try {
    $Timeout = [TimeSpan]::FromMilliseconds(250)
    $Regex = New-Object System.Text.RegularExpressions.Regex($Pattern, [System.Text.RegularExpressions.RegexOptions]::None, $Timeout)
    $Sw = [System.Diagnostics.Stopwatch]::StartNew()
    $Match = $Regex.IsMatch($InputString)
    $Sw.Stop()
    Write-Host "[PASSED] Completed in $($Sw.ElapsedMilliseconds) ms. IsMatch = $Match" -ForegroundColor Green
} catch [System.Text.RegularExpressions.RegexMatchTimeoutException] {
    Write-Warning "[BLOCKED] ReDoS Catastrophic Backtracking Detected! Caught RegexMatchTimeoutException safely."
} catch {
    Write-Error "Invalid pattern: $_"
}

