using OmniForge.Core.Common;

namespace OmniForge.Core.Contracts;

/// <summary>
/// Decoupled abstraction for WinUI 3 native file and folder pickers with automatic HWND initialization.
/// </summary>
public interface IFilePickerService
{
    /// <summary>
    /// Opens a native file dialog to pick a single file matching the specified extensions (e.g. ".json", ".txt", "*").
    /// </summary>
    Task<Result<string>> PickSingleFileAsync(IReadOnlyList<string>? fileTypeFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a native file dialog to pick multiple files matching the specified extensions.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> PickMultipleFilesAsync(IReadOnlyList<string>? fileTypeFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a native folder picker dialog and returns the selected directory path.
    /// </summary>
    Task<Result<string>> PickFolderAsync(CancellationToken cancellationToken = default);
}

