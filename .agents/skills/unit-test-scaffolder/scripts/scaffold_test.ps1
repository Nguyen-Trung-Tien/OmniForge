<#
.SYNOPSIS
    Scaffolds a compact xUnit test class in tests/OmniForge.Tests/.
.PARAMETER TargetName
    Name of the class or service being tested (e.g. HashService).
.PARAMETER SubFolder
    Target subfolder inside tests/OmniForge.Tests (e.g. Core, Services, Architecture).
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$TargetName,

    [Parameter(Mandatory = $false)]
    [string]$SubFolder = "Services"
)

$solutionRoot = Resolve-Path "$PSScriptRoot\..\..\..\.."
$testDir = Join-Path $solutionRoot "tests\OmniForge.Tests\$SubFolder"
New-Item -ItemType Directory -Path $testDir -Force | Out-Null

$testFile = Join-Path $testDir "${TargetName}Tests.cs"
if (Test-Path $testFile) {
    Write-Error "Test file already exists: $testFile"
    exit 1
}

$content = @"
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;

namespace OmniForge.Tests.$SubFolder;

public sealed class ${TargetName}Tests
{
    [Fact]
    public void InitialState_ShouldBeValid()
    {
        var result = Result.Success(true);
        Assert.True(result.IsSuccess);
    }
}
"@

Set-Content -Path $testFile -Value $content -Encoding utf8
Write-Host "[OK] Created test file: $testFile" -ForegroundColor Green

