using System.Collections;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Extensions;

/// <summary>
/// Provides attached properties for WinUI 3 <see cref="TreeView"/> controls.
/// Bridges MVVM observable collections to <see cref="TreeView.RootNodes"/> while maintaining zero code-behind.
/// </summary>
public static class TreeViewExtensions
{
    public static readonly DependencyProperty RootNodesSourceProperty =
        DependencyProperty.RegisterAttached(
            "RootNodesSource",
            typeof(IEnumerable),
            typeof(TreeViewExtensions),
            new PropertyMetadata(null, OnRootNodesSourceChanged));

    public static IEnumerable? GetRootNodesSource(TreeView treeView)
    {
        ArgumentNullException.ThrowIfNull(treeView);
        return (IEnumerable?)treeView.GetValue(RootNodesSourceProperty);
    }

    public static void SetRootNodesSource(TreeView treeView, IEnumerable? value)
    {
        ArgumentNullException.ThrowIfNull(treeView);
        treeView.SetValue(RootNodesSourceProperty, value);
    }

    private static readonly DependencyProperty CollectionHandlerProperty =
        DependencyProperty.RegisterAttached(
            "CollectionHandler",
            typeof(NotifyCollectionChangedEventHandler),
            typeof(TreeViewExtensions),
            null);

    private static void OnRootNodesSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TreeView treeView) return;

        // Unsubscribe from previous observable collection if any
        if (e.OldValue is INotifyCollectionChanged oldObservable)
        {
            if (treeView.GetValue(CollectionHandlerProperty) is NotifyCollectionChangedEventHandler oldHandler)
            {
                oldObservable.CollectionChanged -= oldHandler;
                treeView.ClearValue(CollectionHandlerProperty);
            }
        }

        // Re-sync RootNodes
        SyncRootNodes(treeView, e.NewValue as IEnumerable);

        // Subscribe to new observable collection if supported
        if (e.NewValue is INotifyCollectionChanged newObservable)
        {
            NotifyCollectionChangedEventHandler handler = (_, _) =>
            {
                SyncRootNodes(treeView, e.NewValue as IEnumerable);
            };

            treeView.SetValue(CollectionHandlerProperty, handler);
            newObservable.CollectionChanged += handler;
        }
    }

    private static void SyncRootNodes(TreeView treeView, IEnumerable? source)
    {
        if (treeView.DispatcherQueue.HasThreadAccess)
        {
            PopulateNodes(treeView, source);
        }
        else
        {
            treeView.DispatcherQueue.TryEnqueue(() => PopulateNodes(treeView, source));
        }
    }

    private static void PopulateNodes(TreeView treeView, IEnumerable? source)
    {
        treeView.RootNodes.Clear();
        if (source is null) return;

        foreach (var item in source)
        {
            if (item is TreeViewNode node)
            {
                treeView.RootNodes.Add(node);
            }
        }
    }
}

