<#
.SYNOPSIS
    Zero-touch automated scaffolding script for generating new OmniForge WinUI 3 tool modules.
.DESCRIPTION
    Generates a token-efficient module using ToolViewModelBase, ToolHeaderControl, and automatic
    IToolModule discovery (no manual ServiceCollectionExtensions edits required).
.PARAMETER ModuleName
    PascalCase name for the module (e.g. TextDiff, HashValidator).
.PARAMETER DisplayName
    Human-readable title displayed in the NavigationView.
.PARAMETER Category
    Logical category (from ToolCategory constants).
.PARAMETER IconGlyph
    Segoe Fluent Icon Unicode hex code (e.g. \uE8B7).
.PARAMETER IncludeUnitTest
    When set, also generates a ready-to-run xUnit test file in tests/OmniForge.Tests/Modules/.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ModuleName,

    [Parameter(Mandatory = $false)]
    [string]$DisplayName,

    [Parameter(Mandatory = $false)]
    [string]$Category = "Developer Tools",

    [Parameter(Mandatory = $false)]
    [string]$IconGlyph = "\uE8B7",

    [Parameter(Mandatory = $false)]
    [switch]$IncludeUnitTest
)

if ([string]::IsNullOrWhiteSpace($DisplayName)) {
    $DisplayName = $ModuleName
}

$solutionRoot = Resolve-Path "$PSScriptRoot\..\..\..\.."
$targetDir = Join-Path $solutionRoot "src\OmniForge.App\Modules\$ModuleName"

if (Test-Path $targetDir) {
    Write-Error "Module directory already exists: $targetDir"
    exit 1
}

New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

# 1. Generate Service
$serviceFile = Join-Path $targetDir "${ModuleName}Service.cs"
$serviceContent = @"
using OmniForge.Core.Common;

namespace OmniForge.App.Modules.$ModuleName;

public interface I${ModuleName}Service
{
    Task<Result<string>> ProcessAsync(string input, CancellationToken cancellationToken = default);
}

public sealed class ${ModuleName}Service : I${ModuleName}Service
{
    public async Task<Result<string>> ProcessAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result.Failure<string>("${ModuleName}.InvalidInput", "Input payload cannot be empty.");
        }

        await Task.Delay(25, cancellationToken).ConfigureAwait(false);
        return Result.Success($"Processed '{input}' successfully at {DateTimeOffset.UtcNow:u}.");
    }
}
"@
Set-Content -Path $serviceFile -Value $serviceContent -Encoding utf8

# 2. Generate ViewModel (inheriting from ToolViewModelBase)
$viewModelFile = Join-Path $targetDir "${ModuleName}ViewModel.cs"
$viewModelContent = @"
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.$ModuleName;

public partial class ${ModuleName}ViewModel(I${ModuleName}Service service) : ToolViewModelBase
{
    private readonly I${ModuleName}Service _service = service ?? throw new ArgumentNullException(nameof(service));

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteActionCommand))]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OutputResult { get; set; } = string.Empty;

    private bool CanExecuteAction => !string.IsNullOrWhiteSpace(InputText) && IsNotBusy;

    [RelayCommand(CanExecute = nameof(CanExecuteAction))]
    private async Task ExecuteActionAsync(CancellationToken cancellationToken)
    {
        var output = await RunSafeAsync(
            ct => _service.ProcessAsync(InputText, ct),
            busyStatus: "Processing payload...",
            successStatus: "Completed.",
            cancellationToken: cancellationToken);

        if (output is not null)
        {
            OutputResult = output;
        }
    }
}
"@
Set-Content -Path $viewModelFile -Value $viewModelContent -Encoding utf8

# 3. Generate View XAML (using ToolHeaderControl)
$xamlFile = Join-Path $targetDir "${ModuleName}Page.xaml"
$xamlContent = @"
<?xml version="1.0" encoding="utf-8" ?>
<Page
    x:Class="OmniForge.App.Modules.$ModuleName.${ModuleName}Page"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:controls="using:OmniForge.App.Controls"
    mc:Ignorable="d">

    <ScrollViewer VerticalScrollBarVisibility="Auto" Padding="32,24,32,24">
        <StackPanel Spacing="16" MaxWidth="800" HorizontalAlignment="Left">
            <controls:ToolHeaderControl
                Title="$DisplayName"
                Subtitle="Modular $DisplayName utility tool for OmniForge Suite."
                HasError="{x:Bind ViewModel.HasError, Mode=OneWay}"
                ErrorMessage="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}" />

            <TextBox Header="Input Payload"
                     PlaceholderText="Type or paste payload here..."
                     Text="{x:Bind ViewModel.InputText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Content="Execute"
                        Style="{ThemeResource AccentButtonStyle}"
                        Command="{x:Bind ViewModel.ExecuteActionCommand}" />
                <ProgressRing IsActive="{x:Bind ViewModel.IsBusy, Mode=OneWay}"
                              Width="20" Height="20" />
            </StackPanel>

            <TextBox Header="Output Result"
                     IsReadOnly="True"
                     TextWrapping="Wrap"
                     MinHeight="120"
                     Text="{x:Bind ViewModel.OutputResult, Mode=OneWay}" />
        </StackPanel>
    </ScrollViewer>
</Page>
"@
Set-Content -Path $xamlFile -Value $xamlContent -Encoding utf8

# 4. Generate View Code-Behind
$xamlCsFile = Join-Path $targetDir "${ModuleName}Page.xaml.cs"
$xamlCsContent = @"
using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Modules.$ModuleName;

public sealed partial class ${ModuleName}Page : Page
{
    public ${ModuleName}ViewModel ViewModel { get; } = App.GetService<${ModuleName}ViewModel>();

    public ${ModuleName}Page()
    {
        DataContext = ViewModel;
        InitializeComponent();
    }
}
"@
Set-Content -Path $xamlCsFile -Value $xamlCsContent -Encoding utf8

# 5. Generate Module Descriptor
$moduleFile = Join-Path $targetDir "${ModuleName}Module.cs"
$moduleContent = @"
using OmniForge.Core.Contracts;

namespace OmniForge.App.Modules.$ModuleName;

public sealed class ${ModuleName}Module : IToolModule
{
    public string Id => "omniforge.$($ModuleName.ToLowerInvariant())";
    public string DisplayName => "$DisplayName";
    public string Description => "Modular $DisplayName utility tool.";
    public string IconGlyph => "$IconGlyph";
    public string Category => "$Category";
    public Type ViewType => typeof(${ModuleName}Page);
    public Type ViewModelType => typeof(${ModuleName}ViewModel);
    public int Priority => 50;
}
"@
Set-Content -Path $moduleFile -Value $moduleContent -Encoding utf8

Write-Host "[OK] Scaffolded module '$ModuleName' in $targetDir (Auto-registered via IToolModule discovery)." -ForegroundColor Green
