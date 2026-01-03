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

public sealed class FieldControl<TValue> : ControlBase<TValue>, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "input";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-field.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private string? defaultId;
    private string resolvedControlId = null!;
    private bool hasRendered;

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

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private string ResolvedName => Name ?? FieldContext?.Name ?? NameAttributeValue;

    private bool ResolvedDisabled => Disabled || (FieldContext?.Disabled ?? false);

    private string ResolvedControlId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

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

        FieldContext?.RegisterFocusHandlerFunc(FocusAsync);
        FieldContext?.SubscribeFunc(this);
    }

    protected override void OnParametersSet()
    {
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
        var state = FieldContext?.State ?? FieldRootState.Default;
        var resolvedClass = AttributeUtilities.CombineClassNames(
            AdditionalAttributes,
            string.Join(' ', ClassValue?.Invoke(state), CssClass));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        var attributes = BuildAttributes(state);
        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        if (RenderAs is not null)
        {
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, attributes);
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(2, tag);
        builder.AddMultipleAttributes(3, attributes);
        builder.AddElementReferenceCapture(4, e => Element = e);
        builder.CloseElement();
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
        if (!hasRendered)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("focusElement", Element);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        LabelableContext?.SetControlId(null);
        FieldContext?.UnsubscribeFunc(this);

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

    private Dictionary<string, object> BuildAttributes(FieldRootState state)
    {
        var attributes = new Dictionary<string, object>();

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key is not "class" and not "style" and not "Value" and not "DefaultValue")
                    attributes[attr.Key] = attr.Value;
            }
        }

        attributes["id"] = resolvedControlId;

        attributes["name"] = ResolvedName;

        if (ResolvedDisabled)
            attributes["Disabled"] = true;

        attributes["Value"] = CurrentValueAsString ?? string.Empty;

        if (LabelableContext?.LabelId is not null)
            attributes["aria-labelledby"] = LabelableContext.LabelId;

        var ariaDescribedBy = LabelableContext?.GetAriaDescribedBy();
        if (ariaDescribedBy is not null)
            attributes["aria-describedby"] = ariaDescribedBy;

        if (state.Valid == false)
            attributes["aria-invalid"] = true;

        attributes["onfocus"] = EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus);
        attributes["onblur"] = EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlur);
        attributes["oninput"] = EventCallback.Factory.Create<ChangeEventArgs>(this, HandleInput);

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
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

        if (FieldContext?.ShouldValidateOnChangeFunc() == true)
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