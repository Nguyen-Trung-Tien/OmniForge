[CmdletBinding()]
param(
    [ValidateSet("x64", "arm64")]
    [string]$Platform = "x64",
    [string]$Configuration = "Release"
)

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
Push-Location $RepoRoot
try {
    Write-Host "[Release Packager] Publishing self-contained portable $Configuration build for $Platform..." -ForegroundColor Cyan
    $AppProj = "src\OmniForge.App\OmniForge.App.csproj"
    $Rid = "win-$Platform"
    $OutputDir = "artifacts\publish\$Rid"

    dotnet publish $AppProj `
        -c $Configuration `
        -r $Rid `
        -p:Platform=$Platform `
        -p:WindowsPackageType=None `
        -p:WindowsAppSDKSelfContained=true `
        --self-contained true `
        -o $OutputDir

    if ($LASTEXITCODE -eq 0) {
        Write-Host "[SUCCESS] Self-contained release published to $OutputDir" -ForegroundColor Green
    } else {
        Write-Error "dotnet publish failed with exit code $LASTEXITCODE."
    }
} finally {
    Pop-Location
}

