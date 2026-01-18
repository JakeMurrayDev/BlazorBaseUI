using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Dialog;

namespace BlazorBaseUI.AlertDialog;

public sealed class AlertDialogBackdrop : ComponentBase, IReferencableComponent
{
    private DialogBackdrop? innerComponent;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<DialogBackdropState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<DialogBackdropState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element => innerComponent?.Element;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<DialogBackdrop>(0);
        builder.AddAttribute(1, "As", As);
        builder.AddAttribute(2, "RenderAs", RenderAs);
        builder.AddAttribute(3, "ClassValue", ClassValue);
        builder.AddAttribute(4, "StyleValue", StyleValue);
        builder.AddAttribute(5, "ChildContent", ChildContent);
        builder.AddMultipleAttributes(6, AdditionalAttributes);
        builder.AddComponentReferenceCapture(7, component => innerComponent = (DialogBackdrop)component);
        builder.CloseComponent();
    }
}
