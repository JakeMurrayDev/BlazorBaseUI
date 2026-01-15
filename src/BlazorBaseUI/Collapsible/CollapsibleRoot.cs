using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Collapsible;

public sealed class CollapsibleRoot : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isOpen;
    private string panelId = string.Empty;
    private bool isComponentRenderAs;
    private CollapsibleRootState state = new(false, false);
    private CollapsibleRootContext context = null!;

    [Parameter]
    public bool? Open { get; set; }

    [Parameter]
    public bool DefaultOpen { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public EventCallback<CollapsibleOpenChangeEventArgs> OnOpenChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<CollapsibleRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<CollapsibleRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool IsControlled => Open.HasValue;

    private bool CurrentOpen => IsControlled ? Open!.Value : isOpen;

    protected override void OnInitialized()
    {
        isOpen = DefaultOpen;
        context = new CollapsibleRootContext(CurrentOpen, Disabled, panelId, HandleTrigger, SetPanelId);
        state = new CollapsibleRootState(CurrentOpen, Disabled);
    }

    

    internal void SetPanelId(string id)
    {
        panelId = id;
        context = context with { PanelId = id  };
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (string.IsNullOrEmpty(panelId))
        {
            var id = AttributeUtilities.GetAttributeStringValue(AdditionalAttributes, "id");
            if (!string.IsNullOrEmpty(id))
            {
                SetPanelId(id);
            }
        }

        var currentOpen = CurrentOpen;
        if (state.Open != currentOpen || state.Disabled != Disabled)
        {
            state = state with { Open = currentOpen, Disabled = Disabled };
            context = context with { Open = currentOpen, Disabled = Disabled };
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        builder.OpenComponent<CascadingValue<CollapsibleRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(innerBuilder =>
        {
            if (isComponentRenderAs)
            {
                innerBuilder.OpenComponent(0, RenderAs!);
            }
            else
            {
                innerBuilder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            }

            innerBuilder.AddMultipleAttributes(1, AdditionalAttributes);

            if (state.Open)
            {
                innerBuilder.AddAttribute(2, "data-open", string.Empty);
            }
            else
            {
                innerBuilder.AddAttribute(3, "data-closed", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                innerBuilder.AddAttribute(4, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                innerBuilder.AddAttribute(5, "style", resolvedStyle);
            }

            if (isComponentRenderAs)
            {
                innerBuilder.AddAttribute(6, "ChildContent", ChildContent);
                innerBuilder.AddComponentReferenceCapture(7, component => { Element = ((IReferencableComponent)component).Element; });
                innerBuilder.CloseComponent();
            }
            else
            {
                innerBuilder.AddElementReferenceCapture(8, elementReference => Element = elementReference);
                innerBuilder.AddContent(9, ChildContent);
                innerBuilder.CloseElement();
            }
        }));
        builder.CloseComponent();
    }

    private void HandleTrigger()
    {
        if (Disabled)
        {
            return;
        }

        var nextOpen = !CurrentOpen;
        var args = new CollapsibleOpenChangeEventArgs(nextOpen);

        _ = OnOpenChange.InvokeAsync(args);

        if (args.Canceled)
        {
            return;
        }

        if (!IsControlled)
        {
            isOpen = nextOpen;
        }

        _ = OpenChanged.InvokeAsync(nextOpen);
        state = state with { Open = nextOpen };
        context = context with { Open = nextOpen };
        StateHasChanged();
    }
}
