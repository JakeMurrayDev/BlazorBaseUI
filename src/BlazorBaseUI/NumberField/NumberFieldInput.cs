using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using BlazorBaseUI.Field;
using BlazorBaseUI.Utilities.LabelableProvider;

namespace BlazorBaseUI.NumberField;

public sealed class NumberFieldInput : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "input";

    private bool isComponentRenderAs;
    private bool hasTouchedInput;
    private ElementReference inputElementRef;

    [CascadingParameter]
    private INumberFieldRootContext? RootContext { get; set; }

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [Parameter]
    public string AriaRoledescription { get; set; } = "Number field";

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<NumberFieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<NumberFieldRootState, string>? StyleValue { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private NumberFieldRootState State => RootContext?.State ?? NumberFieldRootState.Default;

    private CultureInfo ResolvedCulture
    {
        get
        {
            var locale = RootContext?.Locale;
            if (!string.IsNullOrEmpty(locale))
            {
                try { return CultureInfo.GetCultureInfo(locale); }
                catch { return CultureInfo.CurrentCulture; }
            }
            return CultureInfo.CurrentCulture;
        }
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && Element.HasValue)
        {
            RootContext?.SetInputElement(Element.Value);
        }
    }

    public void Dispose()
    {
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        if (!string.IsNullOrEmpty(RootContext?.Id))
        {
            builder.AddAttribute(2, "id", RootContext.Id);
        }

        builder.AddAttribute(3, "type", "text");
        builder.AddAttribute(4, "inputmode", RootContext?.InputMode ?? "numeric");
        builder.AddAttribute(5, "autocomplete", "off");
        builder.AddAttribute(6, "autocorrect", "off");
        builder.AddAttribute(7, "spellcheck", "false");
        builder.AddAttribute(8, "aria-roledescription", AriaRoledescription);

        if (RootContext?.Invalid == true)
        {
            builder.AddAttribute(9, "aria-invalid", "true");
        }

        if (!string.IsNullOrEmpty(LabelableContext?.LabelId))
        {
            builder.AddAttribute(10, "aria-labelledby", LabelableContext.LabelId);
        }

        builder.AddAttribute(11, "value", RootContext?.InputValue ?? string.Empty);
        builder.AddAttribute(12, "disabled", RootContext?.Disabled ?? false);
        builder.AddAttribute(13, "readonly", RootContext?.ReadOnly ?? false);
        builder.AddAttribute(14, "required", RootContext?.Required ?? false);

        builder.AddAttribute(15, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus));
        builder.AddAttribute(16, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlur));
        builder.AddAttribute(17, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, HandleInput));
        builder.AddAttribute(18, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        builder.AddAttribute(19, "onpaste", EventCallback.Factory.Create<ClipboardEventArgs>(this, HandlePaste));

        if (state.Scrubbing)
        {
            builder.AddAttribute(20, "data-scrubbing", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(21, "data-disabled", string.Empty);
        }

        if (state.ReadOnly)
        {
            builder.AddAttribute(22, "data-readonly", string.Empty);
        }

        if (state.Required)
        {
            builder.AddAttribute(23, "data-required", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(24, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(25, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(26, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(27, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(28, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(29, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(30, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(31, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentReferenceCapture(32, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(33, elementReference =>
            {
                Element = elementReference;
                inputElementRef = elementReference;
            });
            builder.CloseElement();
        }
    }

    private void HandleFocus(FocusEventArgs e)
    {
        if (RootContext?.ReadOnly == true || RootContext?.Disabled == true)
            return;

        if (!hasTouchedInput)
        {
            hasTouchedInput = true;
        }

        FieldContext?.SetFocused(true);
    }

    private void HandleBlur(FocusEventArgs e)
    {
        if (RootContext?.ReadOnly == true || RootContext?.Disabled == true)
            return;

        FieldContext?.SetTouched(true);
        FieldContext?.SetFocused(false);

        var inputValue = RootContext?.InputValue ?? string.Empty;

        if (string.IsNullOrWhiteSpace(inputValue))
        {
            RootContext?.SetValue(null, NumberFieldChangeReason.InputClear, null);
            RootContext?.OnValueCommitted(null, NumberFieldChangeReason.InputClear);
            return;
        }

        var parsedValue = ParseNumber(inputValue);
        if (parsedValue.HasValue)
        {
            var currentValue = RootContext?.Value;
            if (currentValue != parsedValue.Value)
            {
                RootContext?.SetValue(parsedValue.Value, NumberFieldChangeReason.InputBlur, null);
            }
            RootContext?.OnValueCommitted(parsedValue.Value, NumberFieldChangeReason.InputBlur);

            var formattedValue = FormatNumber(parsedValue.Value);
            if (inputValue != formattedValue)
            {
                RootContext?.SetInputValue(formattedValue);
            }
        }
    }

    private void HandleInput(ChangeEventArgs e)
    {
        var newValue = e.Value?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(newValue))
        {
            RootContext?.SetInputValue(newValue);
            RootContext?.SetValue(null, NumberFieldChangeReason.InputClear, null);
            return;
        }

        if (!IsValidInputCharacters(newValue))
            return;

        RootContext?.SetInputValue(newValue);

        var parsedValue = ParseNumber(newValue);
        if (parsedValue.HasValue)
        {
            RootContext?.SetValue(parsedValue.Value, NumberFieldChangeReason.InputChange, null);
        }
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (RootContext?.ReadOnly == true || RootContext?.Disabled == true)
            return;

        var inputValue = RootContext?.InputValue ?? string.Empty;
        var amount = RootContext?.GetStepAmount(e.AltKey, e.ShiftKey) ?? 1;

        switch (e.Key)
        {
            case "ArrowUp":
                RootContext?.IncrementValue(amount, 1, NumberFieldChangeReason.Keyboard);
                RootContext?.OnValueCommitted(RootContext?.Value, NumberFieldChangeReason.Keyboard);
                break;

            case "ArrowDown":
                RootContext?.IncrementValue(amount, -1, NumberFieldChangeReason.Keyboard);
                RootContext?.OnValueCommitted(RootContext?.Value, NumberFieldChangeReason.Keyboard);
                break;

            case "Home":
                if (RootContext?.Min.HasValue == true)
                {
                    RootContext?.SetValue(RootContext.Min.Value, NumberFieldChangeReason.Keyboard, null);
                    RootContext?.OnValueCommitted(RootContext.Min.Value, NumberFieldChangeReason.Keyboard);
                }
                break;

            case "End":
                if (RootContext?.Max.HasValue == true)
                {
                    RootContext?.SetValue(RootContext.Max.Value, NumberFieldChangeReason.Keyboard, null);
                    RootContext?.OnValueCommitted(RootContext.Max.Value, NumberFieldChangeReason.Keyboard);
                }
                break;
        }
    }

    private void HandlePaste(ClipboardEventArgs e)
    {
        if (RootContext?.ReadOnly == true || RootContext?.Disabled == true)
            return;
    }

    private bool IsValidInputCharacters(string input)
    {
        var culture = ResolvedCulture;
        var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
        var groupSeparator = culture.NumberFormat.NumberGroupSeparator;

        foreach (var ch in input)
        {
            var isDigit = char.IsDigit(ch);
            var isDecimal = decimalSeparator.Contains(ch);
            var isGroup = groupSeparator.Contains(ch);
            var isMinus = ch == '-' || ch == '\u2212';
            var isPlus = ch == '+';
            var isSpace = char.IsWhiteSpace(ch);

            if (!isDigit && !isDecimal && !isGroup && !isMinus && !isPlus && !isSpace)
            {
                return false;
            }
        }

        return true;
    }

    private double? ParseNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var culture = ResolvedCulture;

        input = input.Replace('\u2212', '-').Trim();

        if (double.TryParse(input, NumberStyles.Number, culture, out var result))
        {
            return result;
        }

        if (double.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        return null;
    }

    private string FormatNumber(double value)
    {
        var culture = ResolvedCulture;
        var format = RootContext?.FormatOptions;

        if (format is not null)
        {
            var style = format.Style?.ToLowerInvariant();
            var minFrac = format.MinimumFractionDigits ?? 0;

            return style switch
            {
                "currency" => value.ToString($"C{minFrac}", culture),
                "percent" => value.ToString($"P{minFrac}", culture),
                _ => value.ToString("G", culture)
            };
        }

        return value.ToString("G", culture);
    }
}
