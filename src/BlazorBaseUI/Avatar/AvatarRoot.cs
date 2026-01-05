using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Avatar;

public sealed class AvatarRoot : ComponentBase
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
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        builder.OpenComponent<CascadingValue<AvatarRootContext>>(0);
        builder.AddAttribute(1, "Value", context);
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
        {
            if (isComponentRenderAs)
            {
                innerBuilder.OpenComponent(3, RenderAs!);
            }
            else
            {
                innerBuilder.OpenElement(4, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            }

            innerBuilder.AddMultipleAttributes(5, AdditionalAttributes);

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                innerBuilder.AddAttribute(6, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                innerBuilder.AddAttribute(7, "style", resolvedStyle);
            }

            if (isComponentRenderAs)
            {
                innerBuilder.AddAttribute(8, "ChildContent", ChildContent);
                innerBuilder.AddComponentReferenceCapture(9, component => { Element = ((IReferencableComponent)component).Element; });
                innerBuilder.CloseComponent();
            }
            else
            {
                innerBuilder.AddElementReferenceCapture(10, elementReference => Element = elementReference);
                innerBuilder.AddContent(11, ChildContent);
                innerBuilder.CloseElement();
            }
        }));
        builder.CloseComponent();
    }

    private void SetImageLoadingStatus(ImageLoadingStatus status)
    {
        if (imageLoadingStatus != status)
        {
            imageLoadingStatus = status;
            state = new AvatarRootState(imageLoadingStatus);
            context = new AvatarRootContext(imageLoadingStatus, SetImageLoadingStatus);
            StateHasChanged();
        }
    }
}
