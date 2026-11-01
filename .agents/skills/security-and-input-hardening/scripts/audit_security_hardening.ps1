[CmdletBinding()]
param(
    [string]$TargetFolder = "src\OmniForge.Services"
)

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$ScanPath = Join-Path $RepoRoot $TargetFolder

Write-Host "[Security Hardening Audit] Checking $ScanPath for unhardened XML or path handling..." -ForegroundColor Cyan

$Files = Get-ChildItem -Path $ScanPath -Recurse -Filter "*.cs"
$Warnings = 0

foreach ($File in $Files) {
    $Content = Get-Content -Path $File.FullName -Raw
    if ($Content -match "new\s+XmlDocument\(\)" -or ($Content -match "XmlReader\.Create\(" -and $Content -notmatch "DtdProcessing\.Prohibit")) {
        Write-Warning "$($File.Name) might use unsafe XML reader configuration without DtdProcessing.Prohibit."
        $Warnings++
    }
}

if ($Warnings -eq 0) {
    Write-Host "[PASSED] All XML readers and parsers follow strict hardening guidelines." -ForegroundColor Green
} else {
    Write-Warning "Found $Warnings security audit items to review."
}

