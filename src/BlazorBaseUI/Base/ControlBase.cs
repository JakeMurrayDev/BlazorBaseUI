using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorBaseUI.Base;

public abstract class ControlBase<TValue> : ComponentBase, IDisposable
{
    private readonly EventHandler<ValidationStateChangedEventArgs> validationStateChangedHandler;
    private bool hasInitializedParameters;
    private bool parsingFailed;
    private string? incomingValueBeforeParsing;
    private bool previousParsingAttemptFailed;
    private ValidationMessageStore? parsingValidationMessages;
    private Type? nullableUnderlyingType;
    private TValue? currentValue;
    private bool hasFieldIdentifier;
    private string? formattedValueExpression;

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<TValue>>? ValueExpression { get; set; }

    [Parameter]
    public TValue? DefaultValue { get; set; }

    [Parameter]
    public string? DisplayName { get; set; }

    protected EditContext? EditContext { get; set; }

    protected FieldIdentifier FieldIdentifier { get; set; }

    protected bool IsControlled => ValueChanged.HasDelegate;

    protected string NameAttributeValue
    {
        get
        {
            if (AdditionalAttributes?.TryGetValue("name", out var nameAttributeValue) ?? false)
            {
                return Convert.ToString(nameAttributeValue, CultureInfo.InvariantCulture) ?? string.Empty;
            }

            if (hasFieldIdentifier && ValueExpression is not null)
            {
                formattedValueExpression ??= ExpressionFormatter.FormatLambda(ValueExpression);
                return formattedValueExpression;
            }

            return string.Empty;
        }
    }

    protected TValue? CurrentValue
    {
        get => IsControlled ? Value : currentValue;
        set
        {
            var previousValue = IsControlled ? Value : currentValue;
            var hasChanged = !EqualityComparer<TValue>.Default.Equals(value, previousValue);
            if (hasChanged)
            {
                parsingFailed = false;

                if (IsControlled)
                {
                    _ = ValueChanged.InvokeAsync(value);
                    if (hasFieldIdentifier)
                    {
                        EditContext?.NotifyFieldChanged(FieldIdentifier);
                    }
                }
                else
                {
                    currentValue = value;
                    StateHasChanged();
                }
            }
        }
    }

    protected string? CurrentValueAsString
    {
        get => parsingFailed ? incomingValueBeforeParsing : FormatValueAsString(CurrentValue);
        set
        {
            incomingValueBeforeParsing = value;
            parsingValidationMessages?.Clear();

            if (nullableUnderlyingType is not null && string.IsNullOrEmpty(value))
            {
                parsingFailed = false;
                CurrentValue = default!;
            }
            else if (TryParseValueFromString(value, out var parsedValue, out var validationErrorMessage))
            {
                parsingFailed = false;
                CurrentValue = parsedValue!;
            }
            else
            {
                parsingFailed = true;

                if (EditContext is not null && hasFieldIdentifier)
                {
                    parsingValidationMessages ??= new ValidationMessageStore(EditContext);
                    parsingValidationMessages.Add(FieldIdentifier, validationErrorMessage);
                    EditContext.NotifyFieldChanged(FieldIdentifier);
                }
            }

            if (parsingFailed || previousParsingAttemptFailed)
            {
                if (hasFieldIdentifier)
                {
                    EditContext?.NotifyValidationStateChanged();
                }
                previousParsingAttemptFailed = parsingFailed;
            }
        }
    }

    protected ControlBase()
    {
        validationStateChangedHandler = OnValidateStateChanged;
    }

    protected virtual string? FormatValueAsString(TValue? value) => value?.ToString();

    protected abstract bool TryParseValueFromString(
        string? value,
        [MaybeNullWhen(false)] out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage);

    protected string CssClass
    {
        get
        {
            if (!hasFieldIdentifier)
                return AttributeUtilities.CombineClassNames(AdditionalAttributes, null) ?? string.Empty;

            var fieldClass = EditContext?.FieldCssClass(FieldIdentifier);
            return AttributeUtilities.CombineClassNames(AdditionalAttributes, fieldClass) ?? string.Empty;
        }
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (!hasInitializedParameters)
        {
            if (CascadedEditContext is not null)
            {
                EditContext = CascadedEditContext;
                EditContext.OnValidationStateChanged += validationStateChangedHandler;
            }

            if (ValueExpression is not null)
            {
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
                hasFieldIdentifier = true;
            }

            nullableUnderlyingType = Nullable.GetUnderlyingType(typeof(TValue));

            if (!IsControlled && DefaultValue is not null)
            {
                currentValue = DefaultValue;
            }

            hasInitializedParameters = true;
        }
        else if (CascadedEditContext != EditContext)
        {
            throw new InvalidOperationException(
                $"{GetType()} does not support changing the EditContext dynamically.");
        }

        UpdateAdditionalValidationAttributes();

        return base.SetParametersAsync(ParameterView.Empty);
    }

    private void OnValidateStateChanged(object? sender, ValidationStateChangedEventArgs eventArgs)
    {
        UpdateAdditionalValidationAttributes();
        StateHasChanged();
    }

    private void UpdateAdditionalValidationAttributes()
    {
        if (EditContext is null || !hasFieldIdentifier)
            return;

        var hasAriaInvalidAttribute = AdditionalAttributes?.ContainsKey("aria-invalid") ?? false;

        if (EditContext.GetValidationMessages(FieldIdentifier).Any())
        {
            if (hasAriaInvalidAttribute)
                return;

            if (ConvertToDictionary(AdditionalAttributes, out var additionalAttributes))
                AdditionalAttributes = additionalAttributes;

            additionalAttributes["aria-invalid"] = "true";
        }
        else if (hasAriaInvalidAttribute)
        {
            if (AdditionalAttributes!.Count == 1)
            {
                AdditionalAttributes = null;
            }
            else
            {
                if (ConvertToDictionary(AdditionalAttributes, out var additionalAttributes))
                    AdditionalAttributes = additionalAttributes;

                additionalAttributes.Remove("aria-invalid");
            }
        }
    }

    private static bool ConvertToDictionary(
        IReadOnlyDictionary<string, object>? source,
        out Dictionary<string, object> result)
    {
        var newDictionaryCreated = true;
        if (source is null)
        {
            result = new Dictionary<string, object>();
        }
        else if (source is Dictionary<string, object> currentDictionary)
        {
            result = currentDictionary;
            newDictionaryCreated = false;
        }
        else
        {
            result = new Dictionary<string, object>(source);
        }

        return newDictionaryCreated;
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    void IDisposable.Dispose()
    {
        if (EditContext is not null)
        {
            EditContext.OnValidationStateChanged -= validationStateChangedHandler;
        }

        if (parsingValidationMessages is not null && hasFieldIdentifier)
        {
            parsingValidationMessages.Clear();
            EditContext?.NotifyValidationStateChanged();
        }

        Dispose(disposing: true);
    }
}