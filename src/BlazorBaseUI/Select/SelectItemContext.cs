namespace BlazorBaseUI.Select;

/// <summary>
/// Provides shared state for a <see cref="SelectItem{TValue}"/> and its descendant components.
/// </summary>
internal sealed class SelectItemContext
{
    /// <summary>
    /// Gets or sets whether the parent item is selected.
    /// </summary>
    public bool Selected { get; set; }
}
