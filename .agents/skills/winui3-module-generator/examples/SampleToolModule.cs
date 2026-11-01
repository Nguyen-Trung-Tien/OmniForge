using OmniForge.Core.Contracts;

namespace OmniForge.App.Modules.SampleTool;

/// <summary>
/// Reference production implementation of <see cref="IToolModule"/> demonstrating metadata declaration.
/// </summary>
public sealed class SampleToolModule : IToolModule
{
    public string Id => "omniforge.sampletool";

    public string DisplayName => "Sample Utility";

    public string Description => "Demonstration sub-tool illustrating standard MVVM and Clean Architecture patterns.";

    public string IconGlyph => "\uE8B7"; // Tool / hammer icon in Segoe Fluent Icons

    public string Category => "Developer";

    public Type ViewType => typeof(SampleToolPage);

    public Type ViewModelType => typeof(SampleToolViewModel);

    public int Priority => 20;

    public bool IsEnabled => true;
}

public sealed class SampleToolPage : Microsoft.UI.Xaml.Controls.Page
{
    // Production view code-behind (pure initialization only)
}

public sealed class SampleToolViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    // Production viewmodel definition
}

