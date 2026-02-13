namespace BlazorBaseUI.Field;

/// <summary>
/// Provides contextual information for an individual <see cref="FieldItem"/> within a field.
/// </summary>
public interface IFieldItemContext
{
    /// <summary>
    /// Gets whether the field item is disabled.
    /// </summary>
    bool Disabled { get; }
}

/// <summary>
/// Default implementation of <see cref="IFieldItemContext"/>.
/// </summary>
public sealed class FieldItemContext : IFieldItemContext
{
    /// <summary>
    /// Gets or sets whether the field item is disabled.
    /// </summary>
    public bool Disabled { get; set; }
}
