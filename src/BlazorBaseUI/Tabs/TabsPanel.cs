using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tabs;

public sealed class TabsPanel<TValue> : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "div";

    private string? defaultId;
    private string panelId = null!;
    private bool previousHidden;
    private Orientation previousOrientation;
    private ActivationDirection previousActivationDirection;
    private TabsPanelState state = TabsPanelState.Default;
    private bool isComponentRenderAs;
    private bool isRegistered;

    [CascadingParameter]
    private ITabsRootContext<TValue>? RootContext { get; set; }

    [Parameter, EditorRequired]
    public TValue Value { get; set; } = default!;

    [Parameter]
    public bool KeepMounted { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<TabsPanelState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<TabsPanelState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool IsHidden
    {
        get
        {
            if (RootContext is null)
            {
                return true;
            }

            return !EqualityComparer<TValue>.Default.Equals(RootContext.Value, Value);
        }
    }

    private Orientation Orientation => RootContext?.Orientation ?? Orientation.Horizontal;

    private ActivationDirection ActivationDirection => RootContext?.ActivationDirection ?? ActivationDirection.None;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    protected override void OnInitialized()
    {
        if (RootContext is null)
        {
            throw new InvalidOperationException("TabsPanel must be used within a TabsRoot component.");
        }

        panelId = ResolvedId;
        previousHidden = IsHidden;
        previousOrientation = Orientation;
        previousActivationDirection = ActivationDirection;
        state = new TabsPanelState(previousHidden, previousOrientation, previousActivationDirection);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var newId = ResolvedId;
        if (newId != panelId)
        {
            if (isRegistered)
            {
                RootContext?.UnregisterPanel(Value, panelId);
            }
            panelId = newId;
            isRegistered = false;
        }

        var currentHidden = IsHidden;
        var currentOrientation = Orientation;
        var currentActivationDirection = ActivationDirection;

        if (currentHidden != previousHidden ||
            currentOrientation != previousOrientation ||
            currentActivationDirection != previousActivationDirection)
        {
            state = new TabsPanelState(currentHidden, currentOrientation, currentActivationDirection);
            previousHidden = currentHidden;
            previousOrientation = currentOrientation;
            previousActivationDirection = currentActivationDirection;
        }

        var shouldBeRegistered = !currentHidden || KeepMounted;

        if (shouldBeRegistered && !isRegistered)
        {
            RootContext?.RegisterPanel(Value, panelId);
            isRegistered = true;
        }
        else if (!shouldBeRegistered && isRegistered)
        {
            RootContext?.UnregisterPanel(Value, panelId);
            isRegistered = false;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var isHidden = IsHidden;
        var shouldRender = !isHidden || KeepMounted;

        if (!shouldRender)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationValue = Orientation.ToDataAttributeString();
        var tabId = RootContext?.GetTabIdByPanelValue(Value);

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "id", panelId);
        builder.AddAttribute(3, "role", "tabpanel");
        builder.AddAttribute(4, "tabindex", isHidden ? -1 : 0);

        if (isHidden)
        {
            builder.AddAttribute(5, "hidden", true);
        }

        if (tabId is not null)
        {
            builder.AddAttribute(6, "aria-labelledby", tabId);
        }

        if (orientationValue is not null)
        {
            builder.AddAttribute(7, "data-orientation", orientationValue);
        }
        builder.AddAttribute(8, "data-activation-direction", ActivationDirection.ToDataAttributeString());

        if (isHidden)
        {
            builder.AddAttribute(9, "data-hidden", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(10, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(11, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(12, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(13, component =>
            {
                Element = ((IReferencableComponent)component).Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(12, elementReference => Element = elementReference);
            builder.AddContent(13, ChildContent);
            builder.CloseElement();
        }
    }

    public void Dispose()
    {
        if (isRegistered)
        {
            RootContext?.UnregisterPanel(Value, panelId);
            isRegistered = false;
        }
    }
}
