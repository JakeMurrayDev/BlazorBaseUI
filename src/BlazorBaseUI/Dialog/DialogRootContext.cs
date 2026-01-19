using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Dialog;

internal sealed record class DialogRootContext(
    string RootId,
    bool Nested,
    Func<bool> GetOpen,
    Func<bool> GetMounted,
    Func<object?> GetPayload,
    Func<ElementReference?> GetTriggerElement,
    Func<ElementReference?> GetPopupElement,
    Action<string> SetTitleId,
    Action<string> SetDescriptionId,
    Action<string, ElementReference?> RegisterTriggerElement,
    Action<string> UnregisterTriggerElement,
    Action<ElementReference?> SetPopupElement,
    Func<bool, OpenChangeReason, Task> SetOpenAsync,
    Func<object?, OpenChangeReason, Task> SetOpenWithPayloadAsync,
    Func<string?, object?, OpenChangeReason, Task> SetOpenWithTriggerIdAsync,
    Action<string, object?> SetTriggerPayload,
    Action Close,
    Action ForceUnmount)
{
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
}
