using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Dialog;

namespace BlazorBaseUI.AlertDialog;

public sealed class AlertDialogTrigger : ComponentBase, IReferencableComponent
{
    private DialogTrigger? innerComponent;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public Func<DialogTriggerState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<DialogTriggerState, string>? StyleValue { get; set; }

    [Parameter]
    public object? Payload { get; set; }

    [Parameter]
    public string? TriggerId { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element => innerComponent?.Element;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<DialogTrigger>(0);
        builder.AddAttribute(1, "As", As);
        builder.AddAttribute(2, "RenderAs", RenderAs);
        builder.AddAttribute(3, "Disabled", Disabled);
        builder.AddAttribute(4, "ClassValue", ClassValue);
        builder.AddAttribute(5, "StyleValue", StyleValue);
        builder.AddAttribute(6, "Payload", Payload);
        builder.AddAttribute(7, "TriggerId", TriggerId);
        builder.AddAttribute(8, "ChildContent", ChildContent);
        builder.AddMultipleAttributes(9, AdditionalAttributes);
        builder.AddComponentReferenceCapture(10, component => innerComponent = (DialogTrigger)component);
        builder.CloseComponent();
    }
}
