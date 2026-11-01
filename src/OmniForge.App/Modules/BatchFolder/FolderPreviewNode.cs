using Microsoft.UI.Xaml.Controls;
using OmniForge.Core.Modules.FolderCreator;

namespace OmniForge.App.Modules.BatchFolder;

/// <summary>
/// TreeView node representation for the real-time folder hierarchy preview in WinUI 3.
/// Inherits directly from <see cref="TreeViewNode"/> for native WinUI tree rendering and expansion.
/// </summary>
public sealed class FolderPreviewNode : TreeViewNode
{
    public string Name { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public FolderItemStatus Status { get; set; } = FolderItemStatus.Pending;

    public string StatusBadge { get; set; } = "Pending";

    public string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage) || Status == FolderItemStatus.Invalid || Status == FolderItemStatus.Failed;

    public static FolderPreviewNode FromModel(FolderItemNode model)
    {
        ArgumentNullException.ThrowIfNull(model);

        string badge = model.Status switch
        {
            FolderItemStatus.PlannedNew => "New",
            FolderItemStatus.SkippedExisting => "Exists (Skip)",
            FolderItemStatus.WillOverwrite => "Overwrite",
            FolderItemStatus.SuffixAppended => "Renamed (Suffix)",
            FolderItemStatus.Success => "Created",
            FolderItemStatus.Failed => "Failed",
            FolderItemStatus.Invalid => "Invalid",
            _ => "Pending"
        };

        var node = new FolderPreviewNode
        {
            Name = model.EvaluatedName,
            RelativePath = model.RelativePath,
            FullPath = model.FullPath,
            Status = model.Status,
            StatusBadge = badge,
            ErrorMessage = model.ErrorMessage,
            IsExpanded = true
        };

        foreach (var child in model.Children)
        {
            node.Children.Add(FromModel(child));
        }

        return node;
    }
}

