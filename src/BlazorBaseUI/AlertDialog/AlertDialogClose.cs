using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Dialog;

namespace BlazorBaseUI.AlertDialog;

public sealed class AlertDialogClose : ComponentBase, IReferencableComponent
{
    private DialogClose? innerComponent;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public Func<DialogCloseState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<DialogCloseState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element => innerComponent?.Element;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<DialogClose>(0);
        builder.AddAttribute(1, "As", As);
        builder.AddAttribute(2, "RenderAs", RenderAs);
        builder.AddAttribute(3, "Disabled", Disabled);
        builder.AddAttribute(4, "ClassValue", ClassValue);
        builder.AddAttribute(5, "StyleValue", StyleValue);
        builder.AddAttribute(6, "ChildContent", ChildContent);
        builder.AddMultipleAttributes(7, AdditionalAttributes);
        builder.AddComponentReferenceCapture(8, component => innerComponent = (DialogClose)component);
        builder.CloseComponent();
    }
}
