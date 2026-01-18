using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Dialog;

namespace BlazorBaseUI.AlertDialog;

public sealed class AlertDialogRoot : ComponentBase
{
    [Parameter]
    public bool? Open { get; set; }

    [Parameter]
    public bool DefaultOpen { get; set; }

    [Parameter]
    public bool DismissOnEscape { get; set; } = true;

    [Parameter]
    public DialogRootActions? ActionsRef { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public EventCallback<DialogOpenChangeEventArgs> OnOpenChange { get; set; }

    [Parameter]
    public EventCallback<bool> OnOpenChangeComplete { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<DialogRoot>(0);
        builder.AddAttribute(1, "Open", Open);
        builder.AddAttribute(2, "DefaultOpen", DefaultOpen);
        builder.AddAttribute(3, "Modal", ModalMode.True);
        builder.AddAttribute(4, "Role", DialogRole.AlertDialog);
        builder.AddAttribute(5, "DismissOnEscape", DismissOnEscape);
        builder.AddAttribute(6, "DismissOnOutsidePress", false);
        builder.AddAttribute(7, "ActionsRef", ActionsRef);
        builder.AddAttribute(8, "OpenChanged", OpenChanged);
        builder.AddAttribute(9, "OnOpenChange", OnOpenChange);
        builder.AddAttribute(10, "OnOpenChangeComplete", OnOpenChangeComplete);
        builder.AddAttribute(11, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}
