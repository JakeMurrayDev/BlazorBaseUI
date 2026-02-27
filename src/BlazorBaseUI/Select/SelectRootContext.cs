using BlazorBaseUI.Field;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Select;

/// <summary>
/// Provides strongly-typed context for the select root, implementing <see cref="ISelectRootContext"/>
/// with value-based selection management.
/// </summary>
/// <typeparam name="TValue">The type of value used to identify items.</typeparam>
internal sealed class SelectRootContext<TValue> : ISelectRootContext
{
    private readonly Dictionary<TValue, string> _itemLabels = new();

    /// <inheritdoc />
    public string RootId { get; init; } = string.Empty;

    /// <inheritdoc />
    public bool Open { get; set; }

    /// <inheritdoc />
    public bool Mounted { get; set; }

    /// <inheritdoc />
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public bool ReadOnly { get; set; }

    /// <inheritdoc />
    public bool Required { get; set; }

    /// <inheritdoc />
    public bool Multiple { get; set; }

    /// <inheritdoc />
    public bool HighlightItemOnHover { get; set; }

    /// <inheritdoc />
    public SelectOpenChangeReason OpenChangeReason { get; set; }

    /// <inheritdoc />
    public TransitionStatus TransitionStatus { get; set; }

    /// <inheritdoc />
    public InstantType InstantType { get; set; }

    /// <inheritdoc />
    public int ActiveIndex { get; set; }

    /// <inheritdoc />
    public string? ListId { get; set; }

    /// <inheritdoc />
    public bool HasList => !string.IsNullOrEmpty(ListId);

    /// <inheritdoc />
    public bool ScrollUpArrowVisible { get; set; }

    /// <inheritdoc />
    public bool ScrollDownArrowVisible { get; set; }

    /// <inheritdoc />
    public Func<bool> GetOpen { get; init; } = null!;

    /// <inheritdoc />
    public Func<bool> GetMounted { get; init; } = null!;

    /// <inheritdoc />
    public Func<ElementReference?> GetTriggerElement { get; init; } = null!;

    /// <inheritdoc />
    public Action<ElementReference?> SetTriggerElement { get; init; } = null!;

    /// <inheritdoc />
    public Action<ElementReference?> SetPopupElement { get; init; } = null!;

    /// <inheritdoc />
    public Action<ElementReference?> SetListElement { get; init; } = null!;

    /// <inheritdoc />
    public Action<int> SetActiveIndex { get; init; } = null!;

    /// <inheritdoc />
    public Func<bool, SelectOpenChangeReason, Task> SetOpenAsync { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that returns the current value.
    /// </summary>
    public Func<TValue?> GetValue { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that returns the current values for multi-select.
    /// </summary>
    public Func<IReadOnlyList<TValue>> GetValues { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that selects a value.
    /// </summary>
    public Func<TValue?, Task> SelectValueAsync { get; init; } = null!;

    /// <summary>
    /// Gets or sets the custom equality comparer for item values.
    /// </summary>
    public Func<TValue, TValue, bool>? IsItemEqualToValue { get; set; }

    /// <summary>
    /// Gets or sets the function to convert a value to its string label.
    /// </summary>
    public Func<TValue?, string?>? ItemToStringLabel { get; set; }

    /// <summary>
    /// Gets or sets the function to convert a value to its form submission string.
    /// </summary>
    public Func<TValue?, string?>? ItemToStringValue { get; set; }

    /// <summary>
    /// Gets or sets the static list of options for pre-mount label resolution.
    /// When provided, labels can be resolved before <see cref="SelectItem{TValue}"/>
    /// components mount (e.g., for default value display).
    /// </summary>
    public IReadOnlyList<SelectOption<TValue>>? Items { get; set; }

    /// <inheritdoc />
    public string? PopupId { get; set; }

    /// <inheritdoc />
    public Func<Task> ClearHighlightsAsync { get; init; } = null!;

    /// <inheritdoc />
    public FieldRootState FieldState { get; set; } = FieldRootState.Default;

    /// <summary>
    /// Gets or sets the action delegate invoked when the trigger receives focus.
    /// </summary>
    public Action? OnTriggerFocusAction { get; init; }

    /// <summary>
    /// Gets or sets the action delegate invoked when the trigger loses focus.
    /// </summary>
    public Action? OnTriggerBlurAction { get; init; }

    /// <inheritdoc />
    public void OnTriggerFocus() => OnTriggerFocusAction?.Invoke();

    /// <inheritdoc />
    public void OnTriggerBlur() => OnTriggerBlurAction?.Invoke();

    /// <inheritdoc />
    public event Action? StateChanged;

    /// <inheritdoc />
    public event Action? HighlightCleared;

    /// <summary>
    /// Raises the <see cref="StateChanged"/> event.
    /// </summary>
    public void NotifyStateChanged() => StateChanged?.Invoke();

    /// <summary>
    /// Raises the <see cref="HighlightCleared"/> event.
    /// </summary>
    public void NotifyHighlightCleared() => HighlightCleared?.Invoke();

    /// <inheritdoc />
    public bool IsPlaceholder
    {
        get
        {
            if (Multiple)
            {
                return GetValues().Count == 0;
            }

            return GetValue() is null;
        }
    }

    /// <inheritdoc />
    public bool IsValueSelected(object? value)
    {
        if (value is not TValue typedValue)
        {
            return false;
        }

        if (Multiple)
        {
            var values = GetValues();
            foreach (var v in values)
            {
                if (AreEqual(v, typedValue))
                {
                    return true;
                }
            }
            return false;
        }

        return AreEqual(GetValue(), typedValue);
    }

    /// <summary>
    /// Registers a display label for the given value, used by <see cref="GetLabel"/> when
    /// <see cref="ItemToStringLabel"/> is not provided.
    /// Only notifies subscribers when the registration would change the resolved label
    /// for a currently selected value.
    /// </summary>
    public void RegisterItemLabel(TValue value, string label)
    {
        if (_itemLabels.TryGetValue(value, out var existing) && existing == label)
        {
            return;
        }

        _itemLabels[value] = label;

        if (ItemToStringLabel is not null)
        {
            return;
        }

        if (Items is not null)
        {
            return;
        }

        if (Multiple)
        {
            var values = GetValues();
            foreach (var v in values)
            {
                if (AreEqual(v, value))
                {
                    NotifyStateChanged();
                    return;
                }
            }
        }
        else if (AreEqual(GetValue(), value))
        {
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Called when a <see cref="SelectItem{TValue}"/> disposes. Labels are intentionally
    /// retained so <see cref="GetLabel"/> can resolve display text after popup close
    /// without requiring items to re-register on reopen.
    /// </summary>
    public void UnregisterItemLabel(TValue value)
    {
    }

    /// <summary>
    /// Gets the label for a given value using <see cref="ItemToStringLabel"/> if available,
    /// then the item label registry, otherwise falls back to <see cref="object.ToString"/>.
    /// </summary>
    public string? GetLabel(TValue? value)
    {
        if (ItemToStringLabel is not null)
        {
            return ItemToStringLabel(value);
        }

        if (value is not null && _itemLabels.TryGetValue(value, out var label))
        {
            return label;
        }

        if (value is not null && Items is not null)
        {
            foreach (var item in Items)
            {
                if (AreEqual(item.Value, value))
                {
                    return item.Label;
                }
            }
        }

        return value?.ToString();
    }

    /// <summary>
    /// Gets the form submission string for a given value using <see cref="ItemToStringValue"/> if available,
    /// otherwise falls back to <see cref="object.ToString"/>.
    /// </summary>
    public string? GetFormValue(TValue? value)
    {
        if (ItemToStringValue is not null)
        {
            return ItemToStringValue(value);
        }

        return value?.ToString();
    }

    /// <summary>
    /// Compares two values for equality using <see cref="IsItemEqualToValue"/> if available,
    /// otherwise falls back to <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    internal bool AreEqual(TValue? a, TValue? b)
    {
        if (IsItemEqualToValue is not null && a is not null && b is not null)
        {
            return IsItemEqualToValue(a, b);
        }

        return EqualityComparer<TValue>.Default.Equals(a, b);
    }
}
