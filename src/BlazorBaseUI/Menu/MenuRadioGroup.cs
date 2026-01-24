using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Menu;

public sealed class MenuRadioGroup : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private object? internalValue;
    private MenuRadioGroupContext? groupContext;
    private bool contextDisabled;

    private bool IsControlled => ValueChanged.HasDelegate;

    private object? CurrentValue => IsControlled ? Value : internalValue;

    [Parameter]
    public object? Value { get; set; }

    [Parameter]
    public object? DefaultValue { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback<object?> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<MenuRadioGroupChangeEventArgs> OnValueChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuRadioGroupState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuRadioGroupState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        internalValue = DefaultValue;
        contextDisabled = Disabled;
        groupContext = CreateContext();
    }

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
        var needsContextUpdate = groupContext is null || contextDisabled != Disabled;

        if (needsContextUpdate)
        {
            contextDisabled = Disabled;

            if (groupContext is null)
            {
                groupContext = CreateContext();
            }
            else
            {
                groupContext.Disabled = contextDisabled;
            }
        }

        builder.OpenComponent<CascadingValue<IMenuRadioGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", groupContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderContent);
        builder.CloseComponent();
    }

    private void RenderContent(RenderTreeBuilder builder)
    {
        var state = new MenuRadioGroupState(Disabled);
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "group");

            if (Disabled)
            {
                builder.AddAttribute(3, "aria-disabled", "true");
            }

            if (Disabled)
            {
                builder.AddAttribute(4, "data-disabled", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(5, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(6, "style", resolvedStyle);
            }

            builder.AddComponentParameter(7, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(8, component => Element = ((IReferencableComponent)component).Element);
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "group");

            if (Disabled)
            {
                builder.AddAttribute(3, "aria-disabled", "true");
            }

            if (Disabled)
            {
                builder.AddAttribute(4, "data-disabled", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(5, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(6, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(7, elementReference => Element = elementReference);
            builder.AddContent(8, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private MenuRadioGroupContext CreateContext() => new(
        disabled: Disabled,
        getValue: () => CurrentValue,
        setValue: SetValueInternalAsync);

    private async Task SetValueInternalAsync(object? newValue, MenuRadioGroupChangeEventArgs eventArgs)
    {
        if (OnValueChange.HasDelegate)
        {
            await OnValueChange.InvokeAsync(eventArgs);

            if (eventArgs.IsCanceled)
            {
                StateHasChanged();
                return;
            }
        }

        if (!IsControlled)
        {
            internalValue = newValue;
        }

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(newValue);
        }

        StateHasChanged();
    }
}
