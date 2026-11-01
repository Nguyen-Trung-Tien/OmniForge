[CmdletBinding()]
param(
    [Parameter(Position = 0, Mandatory = $true)]
    [ValidateSet("verify", "arch", "build", "run", "test", "clean", "doctor", "logs")]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$Argument = ""
)

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
Push-Location $RepoRoot
try {
    Write-Host "[CLI Task Runner] Executing '.\forge $Command $Argument'..." -ForegroundColor Cyan
    if ([string]::IsNullOrWhiteSpace($Argument)) {
        & ".\forge.ps1" $Command
    } else {
        & ".\forge.ps1" $Command $Argument
    }
} finally {
    Pop-Location
}

