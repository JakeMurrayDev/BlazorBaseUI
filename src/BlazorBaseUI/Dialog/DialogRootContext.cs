using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Dialog;

/// <summary>
/// Provides the cascading context for dialog and alert dialog root components.
/// </summary>
internal sealed class DialogRootContext
{
    public string RootId { get; set; } = string.Empty;

    public bool Nested { get; set; }

    public bool Open { get; set; }

    public bool Mounted { get; set; }

    public ModalMode Modal { get; set; }

    public DialogRole Role { get; set; }

    public bool DismissOnEscape { get; set; }

    public bool DismissOnOutsidePress { get; set; }

    public int NestedDialogCount { get; set; }

    public OpenChangeReason OpenChangeReason { get; set; }

    public TransitionStatus TransitionStatus { get; set; }

    public InstantType InstantType { get; set; }

    public string? TitleId { get; set; }

    public string? DescriptionId { get; set; }

    public string? ActiveTriggerId { get; set; }

    public object? Payload { get; set; }

    public Func<bool> GetOpen { get; set; } = null!;

    public Func<bool> GetMounted { get; set; } = null!;

    public Func<object?> GetPayload { get; set; } = null!;

    public Func<ElementReference?> GetTriggerElement { get; set; } = null!;

    public Func<ElementReference?> GetPopupElement { get; set; } = null!;

    public Action<string> SetTitleId { get; set; } = null!;

    public Action<string> SetDescriptionId { get; set; } = null!;

    public Action<string, ElementReference?> RegisterTriggerElement { get; set; } = null!;

    public Action<string> UnregisterTriggerElement { get; set; } = null!;

    public Action<ElementReference?> SetPopupElement { get; set; } = null!;

    public Func<bool, OpenChangeReason, Task> SetOpenAsync { get; set; } = null!;

    public Func<object?, OpenChangeReason, Task> SetOpenWithPayloadAsync { get; set; } = null!;

    public Func<string?, object?, OpenChangeReason, Task> SetOpenWithTriggerIdAsync { get; set; } = null!;

    public Action<string, object?> SetTriggerPayload { get; set; } = null!;

    public Action Close { get; set; } = null!;

    public Action ForceUnmount { get; set; } = null!;
}
