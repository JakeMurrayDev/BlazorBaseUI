using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorBaseUI.Utilities.Composite;

public sealed class CompositeRootContext
{
    public int HighlightedIndex { get; set; } = -1;
    public bool HighlightItemOnHover { get; set; }
    public Action<int, bool>? OnHighlightedIndexChange { get; set; }

    public void SetHighlightedIndex(int index, bool scrollIntoView = false)
    {
        HighlightedIndex = index;
        OnHighlightedIndexChange?.Invoke(index, scrollIntoView);
    }
}
