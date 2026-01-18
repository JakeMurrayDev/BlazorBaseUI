using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Dialog;

namespace BlazorBaseUI.AlertDialog;

public sealed class AlertDialogTitle : ComponentBase, IReferencableComponent
{
    private DialogTitle? innerComponent;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element => innerComponent?.Element;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<DialogTitle>(0);
        builder.AddAttribute(1, "As", As);
        builder.AddAttribute(2, "RenderAs", RenderAs);
        builder.AddAttribute(3, "ChildContent", ChildContent);
        builder.AddMultipleAttributes(4, AdditionalAttributes);
        builder.AddComponentReferenceCapture(5, component => innerComponent = (DialogTitle)component);
        builder.CloseComponent();
    }
}
