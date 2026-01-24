using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Menu;

public sealed class MenuPopup : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private MenuPopupState state;

    [CascadingParameter]
    private MenuRootContext? RootContext { get; set; }

    [CascadingParameter]
    private MenuPositionerContext? PositionerContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuPopupState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuPopupState, string>? StyleValue { get; set; }

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
        var nested = RootContext?.ParentType == MenuParentType.Menu;
        state = new MenuPopupState(open, side, align, instant, transitionStatus, nested);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            RootContext?.SetPopupElement(Element);
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
        var rootId = RootContext.RootId;
        var nested = RootContext.ParentType == MenuParentType.Menu;
        var side = PositionerContext?.Side ?? Side.Bottom;
        var align = PositionerContext?.Align ?? Align.Center;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "menu");
            builder.AddAttribute(3, "tabindex", "-1");
            builder.AddAttribute(4, "data-side", side.ToDataAttributeString());
            builder.AddAttribute(5, "data-align", align.ToDataAttributeString());

            if (open)
            {
                builder.AddAttribute(6, "data-open", string.Empty);
            }
            else
            {
                builder.AddAttribute(7, "data-closed", string.Empty);
            }

            var instantAttr = instantType.ToDataAttributeString();
            if (!string.IsNullOrEmpty(instantAttr))
            {
                builder.AddAttribute(8, "data-instant", instantAttr);
            }

            if (transitionStatus == TransitionStatus.Starting)
            {
                builder.AddAttribute(9, "data-starting-style", string.Empty);
            }
            else if (transitionStatus == TransitionStatus.Ending)
            {
                builder.AddAttribute(10, "data-ending-style", string.Empty);
            }

            if (nested)
            {
                builder.AddAttribute(11, "data-nested", string.Empty);
            }

            builder.AddAttribute(12, "data-rootownerid", rootId);

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(13, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(14, "style", resolvedStyle);
            }

            builder.AddAttribute(15, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
            builder.AddAttribute(16, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(17, component =>
            {
                var newElement = ((IReferencableComponent)component).Element;
                if (!Nullable.Equals(Element, newElement))
                {
                    Element = newElement;
                    RootContext?.SetPopupElement(Element);
                }
            });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "menu");
            builder.AddAttribute(3, "tabindex", "-1");
            builder.AddAttribute(4, "data-side", side.ToDataAttributeString());
            builder.AddAttribute(5, "data-align", align.ToDataAttributeString());

            if (open)
            {
                builder.AddAttribute(6, "data-open", string.Empty);
            }
            else
            {
                builder.AddAttribute(7, "data-closed", string.Empty);
            }

            var instantAttr = instantType.ToDataAttributeString();
            if (!string.IsNullOrEmpty(instantAttr))
            {
                builder.AddAttribute(8, "data-instant", instantAttr);
            }

            if (transitionStatus == TransitionStatus.Starting)
            {
                builder.AddAttribute(9, "data-starting-style", string.Empty);
            }
            else if (transitionStatus == TransitionStatus.Ending)
            {
                builder.AddAttribute(10, "data-ending-style", string.Empty);
            }

            if (nested)
            {
                builder.AddAttribute(11, "data-nested", string.Empty);
            }

            builder.AddAttribute(12, "data-rootownerid", rootId);

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(13, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(14, "style", resolvedStyle);
            }

            builder.AddAttribute(15, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
            builder.AddContent(16, ChildContent);
            builder.AddElementReferenceCapture(17, elementReference =>
            {
                if (!Nullable.Equals(Element, elementReference))
                {
                    Element = elementReference;
                    RootContext?.SetPopupElement(Element);
                }
            });
            builder.CloseElement();
            builder.CloseRegion();
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
            RootContext.EmitClose(OpenChangeReason.EscapeKey, null);
        }

        await EventUtilities.InvokeOnKeyDownAsync(AdditionalAttributes, e);
    }
}
