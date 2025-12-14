using BlazorBaseUI.Utilities.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace BlazorBaseUI.Utilities.Composite;

public class CompositeItem<TMetadata, TState> : ComponentBase, IAsyncDisposable
{
    private Lazy<Task<IJSObjectReference>> moduleTask = null!;
    private string compositeId = Guid.NewGuid().ToString("N");
    private int index = -1;

    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    [CascadingParameter]
    private CompositeRootContext? RootContext { get; set; }

    [CascadingParameter]
    private ICompositeListContext? ListContext { get; set; }

    [Parameter]
    public string Tag { get; set; } = "div";

    [Parameter]
    public Type? Type { get; set; }

    [Parameter]
    public RenderFragment<RenderProps>? Render { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public TState? State { get; set; }

    [Parameter]
    public TMetadata? Metadata { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; protected set; }

    protected override void OnInitialized()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/BaseUI.Blazor.Internal/composite.js").AsTask());

        if (ListContext != null)
        {
            ListContext.MapChanged += HandleMapChanged;
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && Element.HasValue && ListContext != null)
        {
            ListContext.Register(Element.Value, Metadata);
            index = ListContext.GetIndex(Element.Value);
        }
    }

    private void HandleMapChanged()
    {
        if (Element.HasValue && ListContext != null)
        {
            var newIndex = ListContext.GetIndex(Element.Value);
            if (newIndex != index)
            {
                index = newIndex;
                InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task HandleFocus()
    {
        if (RootContext != null && index >= 0)
        {
            RootContext.SetHighlightedIndex(index);
        }
    }

    private async Task HandleMouseMove()
    {
        if (RootContext == null || !RootContext.HighlightItemOnHover || index < 0)
        {
            return;
        }

        if (RootContext.HighlightedIndex != index)
        {
            try
            {
                var module = await moduleTask.Value;
                if (Element.HasValue)
                {
                    await module.InvokeVoidAsync("focusElement", Element.Value);
                }
            }
            catch (Exception ex) when (
                ex is JSDisconnectedException or
                TaskCanceledException)
            {
            }
        }
    }

    private bool IsHighlighted => RootContext != null && RootContext.HighlightedIndex == index;

    private IReadOnlyDictionary<string, object?> BuildAttributes()
    {
        var attributes = new Dictionary<string, object?>();

        if (AdditionalAttributes != null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                attributes[attr.Key] = attr.Value;
            }
        }

        if (!string.IsNullOrEmpty(Class))
        {
            attributes["class"] = Class;
        }

        attributes["tabindex"] = IsHighlighted ? 0 : -1;
        attributes["data-composite-id"] = compositeId;
        attributes["onfocus"] = EventCallback.Factory.Create(this, HandleFocus);
        attributes["onmousemove"] = EventCallback.Factory.Create(this, HandleMouseMove);

        return attributes;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var attributes = BuildAttributes();

        if (Render != null)
        {
            var props = new RenderProps(attributes, State);
            builder.AddContent(0, Render(props));
        }
        else if (Type != null)
        {
            builder.OpenComponent(1, Type);
            foreach (var attr in attributes)
            {
                if (attr.Value != null)
                {
                    builder.AddComponentParameter(2, attr.Key, attr.Value);
                }
            }
            builder.AddComponentParameter(3, "ChildContent", ChildContent);
            builder.AddElementReferenceCapture(4, elemRef => Element = elemRef);
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(5, Tag);
            builder.AddMultipleAttributes(6, attributes!);
            builder.AddElementReferenceCapture(7, elemRef => Element = elemRef);
            builder.AddContent(8, ChildContent);
            builder.CloseElement();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (ListContext != null)
        {
            ListContext.MapChanged -= HandleMapChanged;

            if (Element.HasValue)
            {
                ListContext.Unregister(Element.Value);
            }
        }

        if (moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
            catch (Exception ex) when (
                ex is JSDisconnectedException or
                TaskCanceledException)
            {
            }
        }
    }
}
