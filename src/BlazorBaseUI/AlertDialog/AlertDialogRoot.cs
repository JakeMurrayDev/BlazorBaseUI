using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Dialog;

namespace BlazorBaseUI.AlertDialog;

/// <summary>
/// Groups all parts of the alert dialog.
/// Does not render its own element.
/// </summary>
public sealed class AlertDialogRoot : ComponentBase
{
    /// <summary>
    /// Gets or sets whether the alert dialog is currently open.
    /// When set, the component operates in controlled mode.
    /// </summary>
    [Parameter]
    public bool? Open { get; set; }

    /// <summary>
    /// Determines whether the alert dialog is initially open.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>
    /// Determines whether the alert dialog should close when the Escape key is pressed.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    [Parameter]
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>
    /// Gets or sets a reference to imperative actions for controlling the alert dialog programmatically.
    /// </summary>
    [Parameter]
    public DialogRootActions? ActionsRef { get; set; }

    /// <summary>
    /// Gets or sets a handle to associate the alert dialog with external triggers.
    /// Allows detached trigger components to control the alert dialog's open state.
    /// </summary>
    [Parameter]
    public IDialogHandle? Handle { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the open state changes, for two-way binding.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the alert dialog is opened or closed.
    /// </summary>
    [Parameter]
    public EventCallback<DialogOpenChangeEventArgs> OnOpenChange { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked after any animations complete when the alert dialog is opened or closed.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnOpenChangeComplete { get; set; }

    /// <summary>
    /// Gets or sets the ID of the trigger that the alert dialog is associated with.
    /// This is useful in conjunction with the <see cref="Open"/> property to create a controlled alert dialog.
    /// </summary>
    [Parameter]
    public string? TriggerId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the trigger that the alert dialog is initially associated with.
    /// This is useful in conjunction with the <see cref="DefaultOpen"/> property to create an initially open alert dialog.
    /// </summary>
    [Parameter]
    public string? DefaultTriggerId { get; set; }

    /// <summary>
    /// Defines the child components of this instance.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the child content render fragment that receives the payload context from the active trigger.
    /// </summary>
    [Parameter]
    public RenderFragment<DialogRootPayloadContext>? ChildContentWithPayload { get; set; }

    /// <inheritdoc />
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
        builder.AddAttribute(8, "Handle", Handle);
        builder.AddAttribute(9, "OpenChanged", OpenChanged);
        builder.AddAttribute(10, "OnOpenChange", OnOpenChange);
        builder.AddAttribute(11, "OnOpenChangeComplete", OnOpenChangeComplete);
        builder.AddAttribute(12, "TriggerId", TriggerId);
        builder.AddAttribute(13, "DefaultTriggerId", DefaultTriggerId);
        builder.AddAttribute(14, "ChildContent", ChildContent);
        builder.AddAttribute(15, "ChildContentWithPayload", ChildContentWithPayload);
        builder.CloseComponent();
    }
}
