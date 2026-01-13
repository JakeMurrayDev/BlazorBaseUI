using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.NumberField;

public sealed class NumberFieldIncrement : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "button";

    private bool isComponentRenderAs;
    private bool isTouchingButton;
    private string pointerType = string.Empty;

    [CascadingParameter]
    private INumberFieldRootContext? RootContext { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool NativeButton { get; set; } = true;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<NumberFieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<NumberFieldRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private NumberFieldRootState State => RootContext?.State ?? NumberFieldRootState.Default;

    private bool IsDisabled => Disabled || (RootContext?.Disabled ?? false);

    private bool IsAtMax => RootContext?.Value.HasValue == true && RootContext.Value.Value >= RootContext.MaxWithDefault;

    private bool ResolvedDisabled => IsDisabled || IsAtMax;

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }
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

        if (NativeButton || string.IsNullOrEmpty(As))
        {
            builder.AddAttribute(2, "type", "button");
        }

        builder.AddAttribute(3, "disabled", ResolvedDisabled);
        builder.AddAttribute(4, "tabindex", -1);
        builder.AddAttribute(5, "aria-label", "Increase");

        if (!string.IsNullOrEmpty(RootContext?.Id))
        {
            builder.AddAttribute(6, "aria-controls", RootContext.Id);
        }

        if (RootContext?.ReadOnly == true)
        {
            builder.AddAttribute(7, "aria-readonly", "true");
        }

        builder.AddAttribute(8, "style", "user-select:none;-webkit-user-select:none;" + (resolvedStyle ?? string.Empty));

        builder.AddAttribute(9, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));
        builder.AddAttribute(10, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerDown));
        builder.AddAttribute(11, "onpointerup", EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerUp));
        builder.AddAttribute(12, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnter));
        builder.AddAttribute(13, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeave));
        builder.AddAttribute(14, "ontouchstart", EventCallback.Factory.Create<TouchEventArgs>(this, HandleTouchStart));
        builder.AddAttribute(15, "ontouchend", EventCallback.Factory.Create<TouchEventArgs>(this, HandleTouchEnd));

        if (state.Scrubbing)
        {
            builder.AddAttribute(16, "data-scrubbing", string.Empty);
        }

        if (ResolvedDisabled)
        {
            builder.AddAttribute(17, "data-disabled", string.Empty);
        }

        if (state.ReadOnly)
        {
            builder.AddAttribute(18, "data-readonly", string.Empty);
        }

        if (state.Required)
        {
            builder.AddAttribute(19, "data-required", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(20, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(21, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(22, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(23, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(24, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(25, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(26, "class", resolvedClass);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(27, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(28, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(29, elementReference => Element = elementReference);
            builder.AddContent(30, ChildContent);
            builder.CloseElement();
        }
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (ResolvedDisabled || RootContext?.ReadOnly == true)
            return;

        if (pointerType == "touch")
            return;

        if (e.Detail != 0)
            return;

        var amount = RootContext?.GetStepAmount(e.AltKey, e.ShiftKey) ?? 1;
        RootContext?.IncrementValue(amount, 1, NumberFieldChangeReason.IncrementPress);
        RootContext?.OnValueCommitted(RootContext?.Value, NumberFieldChangeReason.IncrementPress);
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private void HandlePointerDown(PointerEventArgs e)
    {
        if (ResolvedDisabled || RootContext?.ReadOnly == true)
            return;

        if (e.Button != 0)
            return;

        pointerType = e.PointerType;

        if (e.PointerType != "touch")
        {
            RootContext?.FocusInput();
            RootContext?.StartAutoChange(true);
        }
    }

    private void HandlePointerUp(PointerEventArgs e)
    {
    }

    private void HandleMouseEnter(MouseEventArgs e)
    {
        if (ResolvedDisabled || RootContext?.ReadOnly == true || isTouchingButton || pointerType == "touch")
            return;
    }

    private void HandleMouseLeave(MouseEventArgs e)
    {
        if (isTouchingButton)
            return;

        RootContext?.StopAutoChange();
    }

    private void HandleTouchStart(TouchEventArgs e)
    {
        isTouchingButton = true;
    }

    private void HandleTouchEnd(TouchEventArgs e)
    {
        isTouchingButton = false;
    }
}
