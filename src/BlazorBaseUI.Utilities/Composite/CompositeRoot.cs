using BlazorBaseUI.Utilities.Direction;
using BlazorBaseUI.Utilities.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace BlazorBaseUI.Utilities.Composite;

public class CompositeRoot<TMetadata, TState> : ComponentBase
{
    private Lazy<Task<IJSObjectReference>> moduleTask = null!;
    private CompositeRootContext rootContext = null!;
    private int highlightedIndex;

    [Inject]
    private IDirectionService DirectionService { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

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
    public CompositeOrientation Orientation { get; set; } = CompositeOrientation.Both;

    [Parameter]
    public int Cols { get; set; } = 1;

    [Parameter]
    public bool LoopFocus { get; set; } = true;

    [Parameter]
    public int HighlightedIndex { get; set; }

    [Parameter]
    public EventCallback<int> HighlightedIndexChanged { get; set; }

    [Parameter]
    public bool EnableHomeAndEndKeys { get; set; }

    [Parameter]
    public bool StopEventPropagation { get; set; } = true;

    [Parameter]
    public IReadOnlyList<int>? DisabledIndices { get; set; }

    [Parameter]
    public bool HighlightItemOnHover { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyDictionary<int, TMetadata?>> OnMapChange { get; set; }

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

        rootContext = new CompositeRootContext
        {
            HighlightedIndex = HighlightedIndex,
            HighlightItemOnHover = HighlightItemOnHover,
            OnHighlightedIndexChange = HandleHighlightedIndexChange
        };
    }

    protected override void OnParametersSet()
    {
        highlightedIndex = HighlightedIndex;
        rootContext.HighlightedIndex = HighlightedIndex;
        rootContext.HighlightItemOnHover = HighlightItemOnHover;
    }

    private void HandleHighlightedIndexChange(int index, bool scrollIntoView)
    {
        highlightedIndex = index;
        rootContext.HighlightedIndex = index;
        HighlightedIndexChanged.InvokeAsync(index);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (!CompositeKeyboardNavigation.IsRelevantKey(e.Key, Orientation, EnableHomeAndEndKeys))
        {
            return;
        }

        if (CompositeKeyboardNavigation.ModifierKeys.Contains(e.Key))
        {
            return;
        }

        if (e.CtrlKey || e.AltKey || e.MetaKey || e.ShiftKey)
        {
            return;
        }

        var isRtl = DirectionService.Direction == TextDirection.Rtl;
        var itemCount = GetItemCount();

        var nextIndex = CompositeKeyboardNavigation.GetNextIndex(
            e.Key,
            highlightedIndex,
            itemCount,
            Orientation,
            LoopFocus,
            isRtl,
            Cols,
            EnableHomeAndEndKeys,
            DisabledIndices);

        if (nextIndex != highlightedIndex && nextIndex >= 0 && nextIndex < itemCount)
        {
            HandleHighlightedIndexChange(nextIndex, true);

            if (StopEventPropagation)
            {
                try
                {
                    var module = await moduleTask.Value;
                    await module.InvokeVoidAsync("stopEvent", e);
                }
                catch (Exception ex) when (
                    ex is JSDisconnectedException or
                    TaskCanceledException)
                {
                }
            }
        }
    }

    private int GetItemCount()
    {
        return 0;
    }

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

        if (Orientation != CompositeOrientation.Both)
        {
            attributes["aria-orientation"] = Orientation.ToString().ToLowerInvariant();
        }

        attributes["onkeydown"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown);

        return attributes;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var attributes = BuildAttributes();

        builder.OpenComponent<CascadingValue<CompositeRootContext>>(0);
        builder.AddComponentParameter(1, "Value", rootContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(cascadeBuilder =>
        {
            cascadeBuilder.OpenComponent<CompositeList<TMetadata>>(4);
            cascadeBuilder.AddComponentParameter(5, "OnMapChange", OnMapChange);
            cascadeBuilder.AddComponentParameter(6, "ChildContent", (RenderFragment)(listBuilder =>
            {
                if (Render != null)
                {
                    var props = new RenderProps(attributes, State);
                    listBuilder.AddContent(7, Render(props));
                }
                else if (Type != null)
                {
                    listBuilder.OpenComponent(8, Type);
                    foreach (var attr in attributes)
                    {
                        if (attr.Value != null)
                        {
                            listBuilder.AddComponentParameter(9, attr.Key, attr.Value);
                        }
                    }
                    listBuilder.AddComponentParameter(10, "ChildContent", ChildContent);
                    listBuilder.AddElementReferenceCapture(11, elemRef => Element = elemRef);
                    listBuilder.CloseComponent();
                }
                else
                {
                    listBuilder.OpenElement(12, Tag);
                    listBuilder.AddMultipleAttributes(13, attributes!);
                    listBuilder.AddElementReferenceCapture(14, elemRef => Element = elemRef);
                    listBuilder.AddContent(15, ChildContent);
                    listBuilder.CloseElement();
                }
            }));
            cascadeBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }

    public async ValueTask DisposeAsync()
    {
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
