using Microsoft.AspNetCore.Components;
using BlazorBaseUI.Field;

namespace BlazorBaseUI.RadioGroup;

/// <summary>
/// Defines the cascading contract consumed by child <see cref="Radio.RadioRoot{TValue}"/> components.
/// </summary>
/// <typeparam name="TValue">The type of value each radio button represents.</typeparam>
internal interface IRadioGroupContext<TValue>
{
    /// <summary>
    /// Gets whether the radio group is disabled.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    /// Gets whether the radio group is read-only.
    /// </summary>
    bool ReadOnly { get; }

    /// <summary>
    /// Gets whether the radio group is required for form submission.
    /// </summary>
    bool Required { get; }

    /// <summary>
    /// Gets the name that identifies the field when a form is submitted.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the field validation instance associated with this group.
    /// </summary>
    FieldValidation? Validation { get; }

    /// <summary>
    /// Gets the element reference for the group's root element.
    /// </summary>
    ElementReference? GroupElement { get; }

    /// <summary>
    /// Gets the currently selected value in the radio group.
    /// </summary>
    TValue? CheckedValue { get; }

    /// <summary>
    /// Sets the selected value in the radio group.
    /// </summary>
    /// <param name="value">The value to select.</param>
    Task SetCheckedValueAsync(TValue value);
}

/// <summary>
/// Provides cascading state and callbacks shared between radio group sub-components.
/// </summary>
/// <typeparam name="TValue">The type of value each radio button represents.</typeparam>
internal sealed class RadioGroupContext<TValue> : IRadioGroupContext<TValue>
{
    /// <summary>
    /// Gets or sets whether the radio group is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets whether the radio group is read-only.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets whether the radio group is required for form submission.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the name that identifies the field when a form is submitted.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the field validation instance associated with this group.
    /// </summary>
    public FieldValidation? Validation { get; set; }

    /// <summary>
    /// Gets or sets the callback that returns the currently selected value.
    /// </summary>
    public Func<TValue?> GetCheckedValueFunc { get; set; } = null!;

    /// <summary>
    /// Gets or sets the callback that sets the selected value.
    /// </summary>
    public Func<TValue, Task> SetCheckedValueFunc { get; set; } = null!;

    /// <summary>
    /// Gets or sets the callback that returns the group's root element reference.
    /// </summary>
    public Func<ElementReference?> GetGroupElementFunc { get; set; } = null!;

    /// <inheritdoc />
    public TValue? CheckedValue => GetCheckedValueFunc();

    /// <inheritdoc />
    public ElementReference? GroupElement => GetGroupElementFunc();

    /// <inheritdoc />
    public Task SetCheckedValueAsync(TValue value) => SetCheckedValueFunc(value);
}
