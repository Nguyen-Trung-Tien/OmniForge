using CommunityToolkit.Mvvm.ComponentModel;
using OmniForge.Core.Contracts;

namespace OmniForge.App.ViewModels;

/// <summary>
/// Observable item representing a tool or view entry in the NavigationView with dynamic localization support.
/// </summary>
public partial class NavigationItemViewModel : ObservableObject
{
    private readonly ILocalizationService? _localizationService;

    public IToolModule Module { get; }

    public string Id => Module.Id;

    public string Title
    {
        get
        {
            if (_localizationService is not null)
            {
                var loc = _localizationService.GetString($"{Module.Id}.Title");
                if (!string.IsNullOrEmpty(loc) && !loc.StartsWith('[')) return loc;
            }
            return Module.Title;
        }
    }

    public string Description
    {
        get
        {
            if (_localizationService is not null)
            {
                var loc = _localizationService.GetString($"{Module.Id}.Desc");
                if (!string.IsNullOrEmpty(loc) && !loc.StartsWith('[')) return loc;
            }
            return Module.Description;
        }
    }

    public string IconGlyph => Module.IconGlyph;
    public string Category => Module.Category;
    public Type ViewType => Module.ViewType;
    public Type ViewModelType => Module.ViewModelType;

    public NavigationItemViewModel(IToolModule module, ILocalizationService? localizationService = null)
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        _localizationService = localizationService;
    }

    public void RefreshLocalizedText()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Description));
    }
}
