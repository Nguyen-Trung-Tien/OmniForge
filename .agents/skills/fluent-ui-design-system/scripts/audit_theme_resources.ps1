[CmdletBinding()]
param(
    [string]$TargetFolder = "src\OmniForge.App"
)

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\..\.."
$ScanPath = Join-Path $RepoRoot $TargetFolder

Write-Host "[Fluent UI Audit] Checking XAML for hardcoded color hex values..." -ForegroundColor Cyan

$Files = Get-ChildItem -Path $ScanPath -Recurse -File -Filter "*.xaml" | Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }
$Violations = 0

foreach ($File in $Files) {
    if ($File.Name -eq "SharedStyles.xaml") { continue }
    $Content = Get-Content -Path $File.FullName
    for ($i = 0; $i -lt $Content.Length; $i++) {
        $Line = $Content[$i]
        if ($Line -match "(Background|Foreground|BorderBrush|Fill|Stroke)\s*=\s*`"#(?:[0-9a-fA-F]{3,8})`"") {
            Write-Warning "$($File.Name):$($i+1) has hardcoded hex color: $Line"
            $Violations++
        }
    }
}

if ($Violations -eq 0) {
    Write-Host "[PASSED] Zero hardcoded hex colors found. All views comply with Fluent ThemeResource rules." -ForegroundColor Green
} else {
    Write-Warning "Found $Violations hardcoded hex colors. Replace with {ThemeResource ...} brushes."
}

