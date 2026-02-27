namespace BlazorBaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectItemIndicator"/> component.
/// </summary>
/// <param name="Selected">Whether the parent item is selected.</param>
public readonly record struct SelectItemIndicatorState(bool Selected);
