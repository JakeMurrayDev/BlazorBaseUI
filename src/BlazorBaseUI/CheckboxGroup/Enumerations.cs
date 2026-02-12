namespace BlazorBaseUI.CheckboxGroup;

/// <summary>
/// Represents the cycling status of a parent checkbox within a <see cref="CheckboxGroup"/>.
/// </summary>
public enum ParentCheckboxStatus
{
    /// <summary>
    /// All child checkboxes are checked.
    /// </summary>
    On,

    /// <summary>
    /// No child checkboxes are checked.
    /// </summary>
    Off,

    /// <summary>
    /// Some child checkboxes are checked.
    /// </summary>
    Mixed
}
