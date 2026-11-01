<#
.SYNOPSIS
    Automated scaffolding script for generating new OmniForge WinUI 3 tool modules.
.DESCRIPTION
    Generates a production-ready module following Clean Architecture, MVVM source generators,
    and IToolModule registration standards.
.PARAMETER ModuleName
    PascalCase name for the module (e.g. TextDiff, HashValidator).
.PARAMETER DisplayName
    Human-readable title displayed in the NavigationView.
.PARAMETER Category
    Logical category (e.g. FileSystem, Developer, Graphics, System).
.PARAMETER IconGlyph
    Segoe Fluent Icon Unicode hex code (e.g. \uE8B7).
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ModuleName,

    [Parameter(Mandatory = $false)]
    [string]$DisplayName,

    [Parameter(Mandatory = $false)]
    [string]$Category = "Developer",

    [Parameter(Mandatory = $false)]
    [string]$IconGlyph = "\uE8B7"
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
Write-Host "Creating module folder: $targetDir" -ForegroundColor Cyan

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
            return Result<string>.Failure(new Error("${ModuleName}.InvalidInput", "Input payload cannot be empty."));
        }

        await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        return Result<string>.Success($"Processed '${input}' successfully at {DateTimeOffset.UtcNow:u}.");
    }
}
"@
Set-Content -Path $serviceFile -Value $serviceContent -Encoding utf8
Write-Host "Created: $serviceFile" -ForegroundColor Green

# 2. Generate ViewModel
$viewModelFile = Join-Path $targetDir "${ModuleName}ViewModel.cs"
$viewModelContent = @"
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;

namespace OmniForge.App.Modules.$ModuleName;

public partial class ${ModuleName}ViewModel : ObservableObject
{
    private readonly I${ModuleName}Service _service;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteActionCommand))]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OutputResult { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public ${ModuleName}ViewModel(I${ModuleName}Service service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    private bool CanExecuteAction => !string.IsNullOrWhiteSpace(InputText) && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanExecuteAction))]
    private async Task ExecuteActionAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _service.ProcessAsync(InputText, cancellationToken);
            if (result.IsSuccess)
            {
                OutputResult = result.Value;
            }
            else
            {
                ErrorMessage = result.Error.Message;
            }
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Operation was cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unhandled failure: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
"@
Set-Content -Path $viewModelFile -Value $viewModelContent -Encoding utf8
Write-Host "Created: $viewModelFile" -ForegroundColor Green

# 3. Generate View XAML
$xamlFile = Join-Path $targetDir "${ModuleName}Page.xaml"
$xamlContent = @"
<?xml version="1.0" encoding="utf-8" ?>
<Page
    x:Class="OmniForge.App.Modules.$ModuleName.${ModuleName}Page"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <ScrollViewer VerticalScrollBarVisibility="Auto" Padding="32,24,32,24">
        <StackPanel Spacing="16" MaxWidth="800" HorizontalAlignment="Left">
            <TextBlock Text="$DisplayName" Style="{ThemeResource TitleTextBlockStyle}" />
            <TextBlock Text="Modular utility tool for OmniForge Suite."
                       Style="{ThemeResource BodyTextBlockStyle}"
                       Foreground="{ThemeResource TextFillColorSecondaryBrush}" />

            <InfoBar IsOpen="{x:Bind ViewModel.ErrorMessage, Mode=OneWay, Converter={StaticResource StringToBoolConverter}}"
                     Severity="Error"
                     Message="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}" />

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
Write-Host "Created: $xamlFile" -ForegroundColor Green

# 4. Generate View Code-Behind
$xamlCsFile = Join-Path $targetDir "${ModuleName}Page.xaml.cs"
$xamlCsContent = @"
using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Modules.$ModuleName;

public sealed partial class ${ModuleName}Page : Page
{
    public ${ModuleName}ViewModel ViewModel { get; }

    public ${ModuleName}Page()
    {
        ViewModel = App.GetService<${ModuleName}ViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }
}
"@
Set-Content -Path $xamlCsFile -Value $xamlCsContent -Encoding utf8
Write-Host "Created: $xamlCsFile" -ForegroundColor Green

# 5. Generate Module Descriptor
$moduleFile = Join-Path $targetDir "${ModuleName}Module.cs"
$moduleContent = @"
using OmniForge.Core.Contracts;

namespace OmniForge.App.Modules.$ModuleName;

public sealed class ${ModuleName}Module : IToolModule
{
    public string Id => "omniforge.${ModuleName.ToLowerInvariant()}";
    public string DisplayName => "$DisplayName";
    public string Description => "Modular $DisplayName utility tool.";
    public string IconGlyph => "$IconGlyph";
    public string Category => "$Category";
    public Type ViewType => typeof(${ModuleName}Page);
    public Type ViewModelType => typeof(${ModuleName}ViewModel);
    public int Priority => 50;
    public bool IsEnabled => true;
}
"@
Set-Content -Path $moduleFile -Value $moduleContent -Encoding utf8
Write-Host "Created: $moduleFile" -ForegroundColor Green

Write-Host "Scaffolding complete! Don't forget to register the services in ServiceCollectionExtensions.cs." -ForegroundColor Yellow

