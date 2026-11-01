using CommunityToolkit.Mvvm.ComponentModel;
using OmniForge.Core.Contracts;

namespace OmniForge.App.ViewModels;

/// <summary>
/// Observable item representing a tool or view entry in the NavigationView.
/// </summary>
public partial class NavigationItemViewModel : ObservableObject
{
    public IToolModule Module { get; }

    public string Id => Module.Id;
    public string Title => Module.Title;
    public string Description => Module.Description;
    public string IconGlyph => Module.IconGlyph;
    public string Category => Module.Category;
    public Type ViewType => Module.ViewType;
    public Type ViewModelType => Module.ViewModelType;

    public NavigationItemViewModel(IToolModule module)
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
    }
}
