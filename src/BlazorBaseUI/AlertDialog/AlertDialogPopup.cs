using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Dialog;

namespace BlazorBaseUI.AlertDialog;

public sealed class AlertDialogPopup : ComponentBase, IReferencableComponent
{
    private DialogPopup? innerComponent;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<DialogPopupState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<DialogPopupState, string>? StyleValue { get; set; }

    [Parameter]
    public ElementReference? InitialFocus { get; set; }

    [Parameter]
    public ElementReference? FinalFocus { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element => innerComponent?.Element;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<DialogPopup>(0);
        builder.AddAttribute(1, "As", As);
        builder.AddAttribute(2, "RenderAs", RenderAs);
        builder.AddAttribute(3, "ClassValue", ClassValue);
        builder.AddAttribute(4, "StyleValue", StyleValue);
        builder.AddAttribute(5, "InitialFocus", InitialFocus);
        builder.AddAttribute(6, "FinalFocus", FinalFocus);
        builder.AddAttribute(7, "ChildContent", ChildContent);
        builder.AddMultipleAttributes(8, AdditionalAttributes);
        builder.AddComponentReferenceCapture(9, component => innerComponent = (DialogPopup)component);
        builder.CloseComponent();
    }
}
