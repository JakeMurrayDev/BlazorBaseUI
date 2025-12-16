using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace BlazorBaseUI.Avatar;

public sealed class AvatarRoot : ComponentBase
{
    private const string DefaultTag = "span";

    private ImageLoadingStatus imageLoadingStatus = ImageLoadingStatus.Idle;
    private AvatarRootContext context = null!;
    private AvatarRootState state = null!;
    private ElementReference element;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<AvatarRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<AvatarRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element => element;

    protected override void OnInitialized()
    {
        state = new AvatarRootState(imageLoadingStatus);
        context = new AvatarRootContext(imageLoadingStatus, SetImageLoadingStatus, state);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        builder.OpenComponent<CascadingValue<AvatarRootContext>>(0);
        builder.AddAttribute(1, "Value", context);
        builder.AddAttribute(2, "IsFixed", false);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(cascadingBuilder =>
        {
            if (RenderAs is not null)
            {
                cascadingBuilder.OpenComponent(5, RenderAs);
                cascadingBuilder.AddAttribute(6, "class", resolvedClass);
                cascadingBuilder.AddAttribute(7, "style", resolvedStyle);
                cascadingBuilder.AddMultipleAttributes(8, AdditionalAttributes);
                cascadingBuilder.AddAttribute(9, "ChildContent", ChildContent);
                cascadingBuilder.AddComponentReferenceCapture(10, obj => { });
                cascadingBuilder.CloseComponent();
            }
            else
            {
                var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
                cascadingBuilder.OpenElement(11, tag);
                cascadingBuilder.AddAttribute(12, "class", resolvedClass);
                cascadingBuilder.AddAttribute(13, "style", resolvedStyle);
                cascadingBuilder.AddMultipleAttributes(14, AdditionalAttributes);
                cascadingBuilder.AddElementReferenceCapture(15, e => element = e);
                cascadingBuilder.AddContent(16, ChildContent);
                cascadingBuilder.CloseElement();
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
            context = new AvatarRootContext(imageLoadingStatus, SetImageLoadingStatus, state);
            StateHasChanged();
        }
    }
}
