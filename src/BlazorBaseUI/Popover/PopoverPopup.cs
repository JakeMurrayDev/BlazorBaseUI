using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Popover;

public sealed class PopoverPopup : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "div";

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool hasRendered;
    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private PopoverPopupState state;
    private DotNetObjectReference<PopoverPopup>? dotNetRef;

    private Lazy<Task<IJSObjectReference>> ModuleTask => moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
        JSRuntime!.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorBaseUI/blazor-baseui-popover.js").AsTask());

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [CascadingParameter]
    private PopoverRootContext? RootContext { get; set; }

    [CascadingParameter]
    private PopoverPositionerContext? PositionerContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public object? InitialFocus { get; set; }

    [Parameter]
    public object? FinalFocus { get; set; }

    [Parameter]
    public Func<PopoverPopupState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<PopoverPopupState, string>? StyleValue { get; set; }

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

        var open = RootContext?.GetOpen() ?? false;
        var side = PositionerContext?.Side ?? Side.Bottom;
        var align = PositionerContext?.Align ?? Align.Center;
        var instant = RootContext?.InstantType ?? InstantType.None;
        var transitionStatus = RootContext?.TransitionStatus ?? TransitionStatus.None;
        state = new PopoverPopupState(open, side, align, instant, transitionStatus);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);
            RootContext?.SetPopupElement(Element);
            await InitializePopupAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null)
        {
            return;
        }

        var open = RootContext.GetOpen();
        var transitionStatus = RootContext.TransitionStatus;
        var instantType = RootContext.InstantType;
        var titleId = RootContext.TitleId;
        var descriptionId = RootContext.DescriptionId;
        var side = PositionerContext?.Side ?? Side.Bottom;
        var align = PositionerContext?.Align ?? Align.Center;
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
        builder.AddAttribute(2, "role", "dialog");
        builder.AddAttribute(3, "tabindex", "-1");

        if (!string.IsNullOrEmpty(titleId))
        {
            builder.AddAttribute(4, "aria-labelledby", titleId);
        }

        if (!string.IsNullOrEmpty(descriptionId))
        {
            builder.AddAttribute(5, "aria-describedby", descriptionId);
        }

        builder.AddAttribute(6, "data-side", side.ToDataAttributeString());
        builder.AddAttribute(7, "data-align", align.ToDataAttributeString());

        if (open)
        {
            builder.AddAttribute(8, "data-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(9, "data-closed", string.Empty);
        }

        var instantAttr = instantType.ToDataAttributeString();
        if (!string.IsNullOrEmpty(instantAttr))
        {
            builder.AddAttribute(10, "data-instant", instantAttr);
        }

        if (transitionStatus == TransitionStatus.Starting)
        {
            builder.AddAttribute(11, "data-starting-style", string.Empty);
        }
        else if (transitionStatus == TransitionStatus.Ending)
        {
            builder.AddAttribute(12, "data-ending-style", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(13, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(14, "style", resolvedStyle);
        }

        builder.AddAttribute(15, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));

        if (isComponentRenderAs)
        {
            builder.AddAttribute(16, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(17, component =>
            {
                componentReference = (IReferencableComponent)component;
                var newElement = componentReference.Element;
                if (!Nullable.Equals(Element, newElement))
                {
                    Element = newElement;
                    RootContext?.SetPopupElement(Element);
                }
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(18, ChildContent);
            builder.AddElementReferenceCapture(19, elementReference =>
            {
                if (!Nullable.Equals(Element, elementReference))
                {
                    Element = elementReference;
                    RootContext?.SetPopupElement(Element);
                }
            });
            builder.CloseElement();
        }
    }

    private async Task InitializePopupAsync()
    {
        if (!Element.HasValue || RootContext is null)
        {
            return;
        }

        try
        {
            var module = await ModuleTask.Value;
            await module.InvokeVoidAsync("initializePopup", Element.Value, dotNetRef);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (RootContext is null)
        {
            return;
        }

        if (e.Key == "Escape")
        {
            await RootContext.SetOpenAsync(false, OpenChangeReason.EscapeKey, null);
        }

        await EventUtilities.InvokeOnKeyDownAsync(AdditionalAttributes, e);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask?.IsValueCreated == true && hasRendered && Element.HasValue)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("disposePopup", Element.Value);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }

        dotNetRef?.Dispose();
    }
}
