using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace OmniForge.Core.Common;

/// <summary>
/// High-performance <see cref="ObservableCollection{T}"/> that supports batch insertions and replacements
/// with a single <see cref="NotifyCollectionChangedAction.Reset"/> notification, preventing WinUI 3 UI thread freezes and layout crashes.
/// </summary>
public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification;

    public BulkObservableCollection()
    {
    }

    public BulkObservableCollection(IEnumerable<T> collection) : base(collection)
    {
    }

    /// <summary>
    /// Adds a batch of items and raises a single collection reset notification upon completion.
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _suppressNotification = true;
        bool addedAny = false;
        try
        {
            foreach (var item in items)
            {
                Items.Add(item);
                addedAny = true;
            }
        }
        finally
        {
            _suppressNotification = false;
        }

        if (addedAny)
        {
            RaiseReset();
        }
    }

    /// <summary>
    /// Clears existing items, populates the collection with <paramref name="items"/>, and fires a single UI update.
    /// </summary>
    public void ReplaceAll(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _suppressNotification = true;
        try
        {
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            _suppressNotification = false;
        }

        RaiseReset();
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnPropertyChanged(e);
        }
    }

    private void RaiseReset()
    {
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}

