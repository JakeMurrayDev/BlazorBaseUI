using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.NumberField;

public sealed class NumberFieldDecrement : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "button";

    private bool isComponentRenderAs;
    private bool isTouchingButton;
    private bool isPressed;
    private string pointerType = string.Empty;

    private NumberFieldRootState State => RootContext?.State ?? NumberFieldRootState.Default;

    private bool IsDisabled => Disabled || (RootContext?.Disabled ?? false);

    private bool IsAtMin => RootContext?.Value.HasValue == true && RootContext.Value.Value <= RootContext.MinWithDefault;

    private bool ResolvedDisabled => IsDisabled || IsAtMin;

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
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (NativeButton || string.IsNullOrEmpty(As))
            {
                builder.AddAttribute(2, "type", "button");
            }

            builder.AddAttribute(3, "disabled", ResolvedDisabled);
            builder.AddAttribute(4, "tabindex", -1);
            builder.AddAttribute(5, "aria-label", "Decrease");

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
            builder.AddAttribute(16, "onmouseup", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseUp));

            if (state.Scrubbing)
            {
                builder.AddAttribute(17, "data-scrubbing", string.Empty);
            }

            if (ResolvedDisabled)
            {
                builder.AddAttribute(18, "data-disabled", string.Empty);
            }

            if (state.ReadOnly)
            {
                builder.AddAttribute(19, "data-readonly", string.Empty);
            }

            if (state.Required)
            {
                builder.AddAttribute(20, "data-required", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(21, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(22, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(23, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(24, "data-dirty", string.Empty);
            }

            if (state.Filled)
            {
                builder.AddAttribute(25, "data-filled", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(26, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(27, "class", resolvedClass);
            }

            builder.AddComponentParameter(28, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(29, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (NativeButton || string.IsNullOrEmpty(As))
            {
                builder.AddAttribute(2, "type", "button");
            }

            builder.AddAttribute(3, "disabled", ResolvedDisabled);
            builder.AddAttribute(4, "tabindex", -1);
            builder.AddAttribute(5, "aria-label", "Decrease");

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
            builder.AddAttribute(16, "onmouseup", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseUp));

            if (state.Scrubbing)
            {
                builder.AddAttribute(17, "data-scrubbing", string.Empty);
            }

            if (ResolvedDisabled)
            {
                builder.AddAttribute(18, "data-disabled", string.Empty);
            }

            if (state.ReadOnly)
            {
                builder.AddAttribute(19, "data-readonly", string.Empty);
            }

            if (state.Required)
            {
                builder.AddAttribute(20, "data-required", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(21, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(22, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(23, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(24, "data-dirty", string.Empty);
            }

            if (state.Filled)
            {
                builder.AddAttribute(25, "data-filled", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(26, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(27, "class", resolvedClass);
            }

            builder.AddElementReferenceCapture(28, elementReference => Element = elementReference);
            builder.AddContent(29, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (ResolvedDisabled || RootContext?.ReadOnly == true)
            return;

        if (pointerType == "touch")
            return;

        // Only handle keyboard/virtual clicks (Detail == 0)
        // Mouse clicks (Detail != 0) are handled by onPointerDown -> startAutoChange
        if (e.Detail != 0)
            return;

        var amount = RootContext?.GetStepAmount(e.AltKey, e.ShiftKey) ?? 1;
        RootContext?.IncrementValue(amount, -1, NumberFieldChangeReason.DecrementPress);
        RootContext?.OnValueCommitted(RootContext?.Value, NumberFieldChangeReason.DecrementPress);
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private void HandlePointerDown(PointerEventArgs e)
    {
        if (ResolvedDisabled || RootContext?.ReadOnly == true)
            return;

        if (e.Button != 0)
            return;

        pointerType = e.PointerType;
        isPressed = true;

        if (e.PointerType != "touch")
        {
            RootContext?.FocusInput();
            RootContext?.StartAutoChange(false);
        }
    }

    private void HandlePointerUp(PointerEventArgs e)
    {
        if (e.PointerType == "touch")
        {
            isPressed = false;
        }
    }

    private void HandleMouseEnter(MouseEventArgs e)
    {
        if (ResolvedDisabled || RootContext?.ReadOnly == true || !isPressed || isTouchingButton || pointerType == "touch")
            return;

        RootContext?.StartAutoChange(false);
    }

    private void HandleMouseLeave(MouseEventArgs e)
    {
        if (isTouchingButton)
            return;

        RootContext?.StopAutoChange();
    }

    private void HandleMouseUp(MouseEventArgs e)
    {
        if (isTouchingButton)
            return;

        isPressed = false;
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
