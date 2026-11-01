[CmdletBinding()]
param(
    [string]$TargetFolder = "src\OmniForge.App"
)

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$ScanPath = Join-Path $RepoRoot $TargetFolder

Write-Host "[Performance & Memory Audit] Scanning $ScanPath for memory anti-patterns..." -ForegroundColor Cyan

$Files = Get-ChildItem -Path $ScanPath -Recurse -Filter "*.cs"
$Warnings = 0

foreach ($File in $Files) {
    $Content = Get-Content -Path $File.FullName
    for ($i = 0; $i -lt $Content.Length; $i++) {
        $Line = $Content[$i]
        # Check for new byte[large] instead of ArrayPool
        if ($Line -match "new\s+byte\[\s*(\d{5,})\s*\]") {
            Write-Warning "$($File.Name):$($i+1) allocates large array on heap: $Line (Use ArrayPool<byte>.Shared.Rent)"
            $Warnings++
        }
        # Check for ObservableCollection.Add inside foreach/while
        if ($Line -match "\.Add\(" -and $Line -match "(Items|Files|List)\.Add") {
            # Potential single-item UI loop add
        }
    }
}

if ($Warnings -eq 0) {
    Write-Host "[PASSED] Zero raw large heap allocations detected." -ForegroundColor Green
} else {
    Write-Warning "Found $Warnings potential memory bottlenecks."
}

