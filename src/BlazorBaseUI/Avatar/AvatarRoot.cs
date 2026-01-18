using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Avatar;

public sealed class AvatarRoot : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "span";

    private ImageLoadingStatus imageLoadingStatus = ImageLoadingStatus.Idle;
    private bool isComponentRenderAs;
    private AvatarRootContext context = null!;
    private AvatarRootState state = new(ImageLoadingStatus.Idle);

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<AvatarRootState, string?>? ClassValue { get; set; }

    [Parameter]
    public Func<AvatarRootState, string?>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        context = new AvatarRootContext(imageLoadingStatus, SetImageLoadingStatus);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (state.ImageLoadingStatus != imageLoadingStatus)
        {
            state = new AvatarRootState(imageLoadingStatus);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<AvatarRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderChildContent);
        builder.CloseComponent();
    }

    private void RenderChildContent(RenderTreeBuilder innerBuilder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            innerBuilder.OpenRegion(0);
            innerBuilder.OpenComponent(0, RenderAs!);
            innerBuilder.AddMultipleAttributes(1, AdditionalAttributes);
            if (!string.IsNullOrEmpty(resolvedClass))
            {
                innerBuilder.AddAttribute(2, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                innerBuilder.AddAttribute(3, "style", resolvedStyle);
            }
            innerBuilder.AddAttribute(4, "ChildContent", ChildContent);
            innerBuilder.AddComponentReferenceCapture(5, component => { Element = ((IReferencableComponent)component).Element; });
            innerBuilder.CloseComponent();
            innerBuilder.CloseRegion();
        }
        else
        {
            innerBuilder.OpenRegion(1);
            innerBuilder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            innerBuilder.AddMultipleAttributes(1, AdditionalAttributes);
            if (!string.IsNullOrEmpty(resolvedClass))
            {
                innerBuilder.AddAttribute(2, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                innerBuilder.AddAttribute(3, "style", resolvedStyle);
            }
            innerBuilder.AddElementReferenceCapture(4, elementReference => Element = elementReference);
            innerBuilder.AddContent(5, ChildContent);
            innerBuilder.CloseElement();
            innerBuilder.CloseRegion();
        }
    }

    private void SetImageLoadingStatus(ImageLoadingStatus status)
    {
        if (imageLoadingStatus != status)
        {
            imageLoadingStatus = status;
            state = new AvatarRootState(imageLoadingStatus);
            context = new AvatarRootContext(imageLoadingStatus, SetImageLoadingStatus);
            _ = InvokeAsync(StateHasChanged);
        }
    }
}
