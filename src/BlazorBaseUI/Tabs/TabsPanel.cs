using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tabs;

public sealed class TabsPanel<TValue> : ComponentBase, IDisposable
{
    private const string DefaultTag = "div";

    private string? defaultId;
    private string panelId = null!;
    private bool previousHidden;
    private TabsPanelState? cachedState;
    private bool stateDirty = true;
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
                return true;

            return !EqualityComparer<TValue>.Default.Equals(RootContext.Value, Value);
        }
    }

    private Orientation Orientation => RootContext?.Orientation ?? Orientation.Horizontal;

    private ActivationDirection ActivationDirection => RootContext?.ActivationDirection ?? ActivationDirection.None;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    private TabsPanelState State
    {
        get
        {
            if (stateDirty || cachedState is null)
            {
                cachedState = new TabsPanelState(IsHidden, Orientation, ActivationDirection);
                stateDirty = false;
            }
            return cachedState;
        }
    }

    protected override void OnInitialized()
    {
        if (RootContext is null)
            throw new InvalidOperationException("TabsPanel must be used within a TabsRoot component.");

        panelId = ResolvedId;
        previousHidden = IsHidden;
    }

    protected override void OnParametersSet()
    {
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
        if (currentHidden != previousHidden)
        {
            stateDirty = true;
            previousHidden = currentHidden;
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
        var shouldRender = !IsHidden || KeepMounted;
        if (!shouldRender)
            return;

        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (RenderAs is not null)
        {
            if (!typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
            {
                throw new InvalidOperationException($"Type {RenderAs.Name} must implement IReferencableComponent.");
            }
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, BuildAttributes(state, resolvedClass, resolvedStyle));
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(3, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, BuildAttributes(state, resolvedClass, resolvedStyle));
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    public void Dispose()
    {
        if (isRegistered)
        {
            RootContext?.UnregisterPanel(Value, panelId);
            isRegistered = false;
        }
    }

    private Dictionary<string, object> BuildAttributes(TabsPanelState state, string? resolvedClass, string? resolvedStyle)
    {
        var attributes = new Dictionary<string, object>();

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key is not "class" and not "style")
                    attributes[attr.Key] = attr.Value;
            }
        }

        attributes["id"] = panelId;
        attributes["role"] = "tabpanel";
        attributes["tabindex"] = IsHidden ? -1 : 0;

        if (IsHidden)
            attributes["hidden"] = true;

        var tabId = RootContext?.GetTabIdByPanelValue(Value);
        if (tabId is not null)
            attributes["aria-labelledby"] = tabId;

        state.WriteDataAttributes(attributes);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        return attributes;
    }
}
