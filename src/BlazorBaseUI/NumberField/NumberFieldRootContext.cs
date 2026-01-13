using Microsoft.AspNetCore.Components;
using BlazorBaseUI.Slider;

namespace BlazorBaseUI.NumberField;

public interface INumberFieldRootContext
{
    string InputValue { get; }
    double? Value { get; }
    double MinWithDefault { get; }
    double MaxWithDefault { get; }
    double? Min { get; }
    double? Max { get; }
    bool Disabled { get; }
    bool ReadOnly { get; }
    string? Id { get; }
    string? Name { get; }
    bool Required { get; }
    bool? Invalid { get; }
    string InputMode { get; }
    bool IsScrubbing { get; }
    string? Locale { get; }
    NumberFormatOptions? FormatOptions { get; }
    NumberFieldRootState State { get; }
    ElementReference? InputElement { get; }

    void SetValue(double? value, NumberFieldChangeReason reason, int? direction = null);
    void IncrementValue(double amount, int direction, NumberFieldChangeReason reason);
    double GetStepAmount(bool altKey, bool shiftKey);
    void StartAutoChange(bool isIncrement);
    void StopAutoChange();
    void SetInputValue(string value);
    void SetIsScrubbing(bool value);
    void OnValueCommitted(double? value, NumberFieldChangeReason reason);
    void SetInputElement(ElementReference element);
    void FocusInput();
}

public sealed class NumberFieldRootContext : INumberFieldRootContext
{
    internal static NumberFieldRootContext Default { get; } = new();

    public string InputValue { get; private set; } = string.Empty;
    public double? Value { get; private set; }
    public double MinWithDefault { get; private set; } = double.MinValue;
    public double MaxWithDefault { get; private set; } = double.MaxValue;
    public double? Min { get; private set; }
    public double? Max { get; private set; }
    public bool Disabled { get; private set; }
    public bool ReadOnly { get; private set; }
    public string? Id { get; private set; }
    public string? Name { get; private set; }
    public bool Required { get; private set; }
    public bool? Invalid { get; private set; }
    public string InputMode { get; private set; } = "numeric";
    public bool IsScrubbing { get; private set; }
    public string? Locale { get; private set; }
    public NumberFormatOptions? FormatOptions { get; private set; }
    public NumberFieldRootState State { get; private set; } = NumberFieldRootState.Default;
    public ElementReference? InputElement { get; private set; }

    private Action<double?, NumberFieldChangeReason, int?>? setValueCallback;
    private Action<double, int, NumberFieldChangeReason>? incrementValueCallback;
    private Func<bool, bool, double>? getStepAmountCallback;
    private Action<bool>? startAutoChangeCallback;
    private Action? stopAutoChangeCallback;
    private Action<string>? setInputValueCallback;
    private Action<bool>? setIsScrubbingCallback;
    private Action<double?, NumberFieldChangeReason>? onValueCommittedCallback;
    private Action<ElementReference>? setInputElementCallback;
    private Action? focusInputCallback;

    private NumberFieldRootContext() { }

    public NumberFieldRootContext(
        Action<double?, NumberFieldChangeReason, int?> setValue,
        Action<double, int, NumberFieldChangeReason> incrementValue,
        Func<bool, bool, double> getStepAmount,
        Action<bool> startAutoChange,
        Action stopAutoChange,
        Action<string> setInputValue,
        Action<bool> setIsScrubbing,
        Action<double?, NumberFieldChangeReason> onValueCommitted,
        Action<ElementReference> setInputElement,
        Action focusInput)
    {
        setValueCallback = setValue;
        incrementValueCallback = incrementValue;
        getStepAmountCallback = getStepAmount;
        startAutoChangeCallback = startAutoChange;
        stopAutoChangeCallback = stopAutoChange;
        setInputValueCallback = setInputValue;
        setIsScrubbingCallback = setIsScrubbing;
        onValueCommittedCallback = onValueCommitted;
        setInputElementCallback = setInputElement;
        focusInputCallback = focusInput;
    }

    internal void Update(
        string inputValue,
        double? value,
        double minWithDefault,
        double maxWithDefault,
        double? min,
        double? max,
        bool disabled,
        bool readOnly,
        string? id,
        string? name,
        bool required,
        bool? invalid,
        string inputMode,
        bool isScrubbing,
        string? locale,
        NumberFormatOptions? formatOptions,
        NumberFieldRootState state,
        ElementReference? inputElement)
    {
        InputValue = inputValue;
        Value = value;
        MinWithDefault = minWithDefault;
        MaxWithDefault = maxWithDefault;
        Min = min;
        Max = max;
        Disabled = disabled;
        ReadOnly = readOnly;
        Id = id;
        Name = name;
        Required = required;
        Invalid = invalid;
        InputMode = inputMode;
        IsScrubbing = isScrubbing;
        Locale = locale;
        FormatOptions = formatOptions;
        State = state;
        InputElement = inputElement;
    }

    public void SetValue(double? value, NumberFieldChangeReason reason, int? direction = null) =>
        setValueCallback?.Invoke(value, reason, direction);

    public void IncrementValue(double amount, int direction, NumberFieldChangeReason reason) =>
        incrementValueCallback?.Invoke(amount, direction, reason);

    public double GetStepAmount(bool altKey, bool shiftKey) =>
        getStepAmountCallback?.Invoke(altKey, shiftKey) ?? 1;

    public void StartAutoChange(bool isIncrement) =>
        startAutoChangeCallback?.Invoke(isIncrement);

    public void StopAutoChange() =>
        stopAutoChangeCallback?.Invoke();

    public void SetInputValue(string value) =>
        setInputValueCallback?.Invoke(value);

    public void SetIsScrubbing(bool value) =>
        setIsScrubbingCallback?.Invoke(value);

    public void OnValueCommitted(double? value, NumberFieldChangeReason reason) =>
        onValueCommittedCallback?.Invoke(value, reason);

    public void SetInputElement(ElementReference element) =>
        setInputElementCallback?.Invoke(element);

    public void FocusInput() =>
        focusInputCallback?.Invoke();
}
