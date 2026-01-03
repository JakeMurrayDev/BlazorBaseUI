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
    public ElementReference? Element { get; private set; }

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
                cascadingBuilder.CloseComponent();
            }
            else
            {
                var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
                cascadingBuilder.OpenElement(10, tag);
                cascadingBuilder.AddAttribute(11, "class", resolvedClass);
                cascadingBuilder.AddAttribute(12, "style", resolvedStyle);
                cascadingBuilder.AddMultipleAttributes(13, AdditionalAttributes);
                cascadingBuilder.AddElementReferenceCapture(14, e => Element = e);
                cascadingBuilder.AddContent(15, ChildContent);
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
