using CommunityToolkit.Mvvm.ComponentModel;
using OmniForge.Core.Contracts;

namespace OmniForge.Core.Models;

/// <summary>
/// Convenient abstract base class for tool modules providing observable property support.
/// </summary>
public abstract partial class ToolModuleBase : ObservableObject, IToolModule
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public virtual string Title => DisplayName;
    public abstract string Description { get; }
    public abstract string IconGlyph { get; }
    public virtual string Category => "General";
    public abstract Type ViewType { get; }
    public abstract Type ViewModelType { get; }

    [ObservableProperty]
    private int _priority;

    public int Order => Priority;

    [ObservableProperty]
    private bool _isEnabled = true;

    public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
