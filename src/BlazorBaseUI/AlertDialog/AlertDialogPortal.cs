using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Dialog;

namespace BlazorBaseUI.AlertDialog;

public sealed class AlertDialogPortal : ComponentBase
{
    [Parameter]
    public bool KeepMounted { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<DialogPortal>(0);
        builder.AddAttribute(1, "KeepMounted", KeepMounted);
        builder.AddAttribute(2, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}
