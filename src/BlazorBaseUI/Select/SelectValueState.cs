namespace BlazorBaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectValue{TValue}"/> component.
/// </summary>
/// <param name="Placeholder">Whether no value is selected and a placeholder is shown.</param>
public readonly record struct SelectValueState(bool Placeholder);
