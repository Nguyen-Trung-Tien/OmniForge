using OmniForge.App.Views;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace OmniForge.App.Helpers;

/// <summary>
/// WinUI 3 implementation of <see cref="IFilePickerService"/> that automatically binds native dialogs
/// to the active <see cref="MainWindow"/> HWND via <see cref="InitializeWithWindow"/>.
/// </summary>
public sealed class FilePickerService : IFilePickerService
{
    public async Task<Result<string>> PickSingleFileAsync(
        IReadOnlyList<string>? fileTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var picker = CreateFileOpenPicker(fileTypeFilter);
            var file = await picker.PickSingleFileAsync().AsTask(cancellationToken);
            return file is not null
                ? Result.Success(file.Path)
                : Result.Failure<string>(Error.Cancelled);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<string>(Error.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>("Picker.FileFailed", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<string>>> PickMultipleFilesAsync(
        IReadOnlyList<string>? fileTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var picker = CreateFileOpenPicker(fileTypeFilter);
            var files = await picker.PickMultipleFilesAsync().AsTask(cancellationToken);
            if (files is null || files.Count == 0)
            {
                return Result.Failure<IReadOnlyList<string>>(Error.Cancelled);
            }

            var paths = files.Select(f => f.Path).ToList();
            return Result.Success<IReadOnlyList<string>>(paths);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<IReadOnlyList<string>>(Error.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<string>>("Picker.MultiFileFailed", ex.Message);
        }
    }

    public async Task<Result<string>> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");
            InitializePickerWindow(picker);

            var folder = await picker.PickSingleFolderAsync().AsTask(cancellationToken);
            return folder is not null
                ? Result.Success(folder.Path)
                : Result.Failure<string>(Error.Cancelled);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<string>(Error.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>("Picker.FolderFailed", ex.Message);
        }
    }

    private static FileOpenPicker CreateFileOpenPicker(IReadOnlyList<string>? fileTypeFilter)
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };

        if (fileTypeFilter is { Count: > 0 })
        {
            foreach (var ext in fileTypeFilter)
            {
                picker.FileTypeFilter.Add(ext);
            }
        }
        else
        {
            picker.FileTypeFilter.Add("*");
        }

        InitializePickerWindow(picker);
        return picker;
    }

    private static void InitializePickerWindow(object picker)
    {
        var mainWindow = App.GetService<MainWindow>();
        var hWnd = WindowNative.GetWindowHandle(mainWindow);
        InitializeWithWindow.Initialize(picker, hWnd);
    }
}

