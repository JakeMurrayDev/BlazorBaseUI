using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.Form;
using BlazorBaseUI.Base;
using BlazorBaseUI.Utilities.LabelableProvider;

namespace BlazorBaseUI.Field;

public sealed class FieldControl<TValue> : ControlBase<TValue>, IReferencableComponent, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "input";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-field.js";

    private static readonly object EmptyValue = string.Empty;

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private string? defaultId;
    private string resolvedControlId = null!;
    private bool hasRendered;
    private bool isComponentRenderAs;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [CascadingParameter]
    private FormContext? FormContext { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? StyleValue { get; set; }

    public ElementReference? Element { get; private set; }

    private string ResolvedName => Name ?? FieldContext?.Name ?? NameAttributeValue;

    private bool ResolvedDisabled => Disabled || (FieldContext?.Disabled ?? false);

    private string ResolvedControlId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    private FieldRootState State => FieldContext?.State ?? FieldRootState.Default;

    public FieldControl()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        resolvedControlId = ResolvedControlId;
        LabelableContext?.SetControlId(resolvedControlId);

        var initialValue = IsControlled ? Value : DefaultValue;
        FieldContext?.Validation.SetInitialValue(initialValue);

        if (initialValue is not null && !IsEmpty(initialValue))
            FieldContext?.SetFilled(true);

        FieldContext?.RegisterFocusHandler(FocusAsync);
        FieldContext?.Subscribe(this);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var newResolvedId = ResolvedControlId;
        if (newResolvedId != resolvedControlId)
        {
            resolvedControlId = newResolvedId;
            LabelableContext?.SetControlId(resolvedControlId);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(
            AdditionalAttributes,
            string.Join(' ', ClassValue?.Invoke(state), CssClass));
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
        builder.AddAttribute(2, "id", resolvedControlId);
        builder.AddAttribute(3, "name", ResolvedName);

        if (ResolvedDisabled)
        {
            builder.AddAttribute(4, "disabled", true);
        }

        builder.AddAttribute(5, "value", CurrentValueAsString ?? EmptyValue);

        if (!string.IsNullOrEmpty(LabelableContext?.LabelId))
        {
            builder.AddAttribute(6, "aria-labelledby", LabelableContext.LabelId);
        }

        var ariaDescribedBy = LabelableContext?.GetAriaDescribedBy();
        if (!string.IsNullOrEmpty(ariaDescribedBy))
        {
            builder.AddAttribute(7, "aria-describedby", ariaDescribedBy);
        }

        if (state.Valid == false)
        {
            builder.AddAttribute(8, "aria-invalid", "true");
        }

        builder.AddAttribute(9, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus));
        builder.AddAttribute(10, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlur));
        builder.AddAttribute(11, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, HandleInput));

        if (state.Disabled)
        {
            builder.AddAttribute(12, "data-disabled", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(13, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(14, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(15, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(16, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(17, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(18, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(19, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(20, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentReferenceCapture(21, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(22, elementReference => Element = elementReference);
            builder.CloseElement();
        }
    }

    protected override bool TryParseValueFromString(
        string? value,
        [MaybeNullWhen(false)] out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

        if (targetType == typeof(string))
        {
            result = (TValue?)(object?)value ?? default!;
            validationErrorMessage = null;
            return true;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            result = default!;
            validationErrorMessage = null;
            return true;
        }

        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)))
        {
            try
            {
                result = (TValue?)converter.ConvertFromInvariantString(value) ?? default!;
                validationErrorMessage = null;
                return true;
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or ArgumentException)
            {
                result = default;
                validationErrorMessage = $"The Value '{value}' is not valid for {DisplayName ?? "this field"}.";
                return false;
            }
        }

        result = default;
        validationErrorMessage = $"Cannot convert '{value}' to type {typeof(TValue).Name}.";
        return false;
    }

    protected override string? FormatValueAsString(TValue? value)
    {
        return value switch
        {
            null => null,
            string s => s,
            IFormattable f => f.ToString(null, null),
            _ => value.ToString()
        };
    }

    void IFieldStateSubscriber.NotifyStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public async ValueTask FocusAsync()
    {
        if (!hasRendered || !Element.HasValue)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("focusElement", Element.Value);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        LabelableContext?.SetControlId(null);
        FieldContext?.Unsubscribe(this);

        if (moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private void HandleFocus(FocusEventArgs e)
    {
        FieldContext?.SetFocused(true);
    }

    private async Task HandleBlur(FocusEventArgs e)
    {
        FieldContext?.SetTouched(true);
        FieldContext?.SetFocused(false);

        if (FieldContext?.ValidationMode == ValidationMode.OnBlur)
        {
            await (FieldContext?.Validation.CommitAsync(CurrentValue) ?? Task.CompletedTask);
        }
    }

    private async Task HandleInput(ChangeEventArgs e)
    {
        var stringValue = e.Value?.ToString() ?? string.Empty;

        CurrentValueAsString = stringValue;

        FormContext?.ClearErrors(ResolvedName);

        var initialValue = FieldContext?.ValidityData.InitialValue;
        var isDirty = !EqualityComparer<TValue>.Default.Equals(CurrentValue, (TValue?)initialValue);
        FieldContext?.SetDirty(isDirty);
        FieldContext?.SetFilled(!IsEmpty(CurrentValue));

        if (FieldContext?.ShouldValidateOnChange() == true)
        {
            if (FieldContext.ValidationDebounceTime > 0)
                FieldContext.Validation.CommitDebounced(CurrentValue);
            else
                await FieldContext.Validation.CommitAsync(CurrentValue);
        }
    }

    private static bool IsEmpty(TValue? value)
    {
        return value switch
        {
            null => true,
            string s => string.IsNullOrEmpty(s),
            _ => false
        };
    }
}
