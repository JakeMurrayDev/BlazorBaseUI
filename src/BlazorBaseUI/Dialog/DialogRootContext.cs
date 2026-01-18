using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Dialog;

internal sealed class DialogRootContext
{
    public DialogRootContext(
        string rootId,
        bool open,
        bool mounted,
        bool nested,
        ModalMode modal,
        DialogRole role,
        bool dismissOnEscape,
        bool dismissOnOutsidePress,
        int nestedDialogCount,
        OpenChangeReason openChangeReason,
        TransitionStatus transitionStatus,
        InstantType instantType,
        string? titleId,
        string? descriptionId,
        string? activeTriggerId,
        object? payload,
        Func<bool> getOpen,
        Func<bool> getMounted,
        Func<object?> getPayload,
        Func<ElementReference?> getTriggerElement,
        Func<ElementReference?> getPopupElement,
        Action<string> setTitleId,
        Action<string> setDescriptionId,
        Action<string, ElementReference?> registerTriggerElement,
        Action<string> unregisterTriggerElement,
        Action<ElementReference?> setPopupElement,
        Func<bool, OpenChangeReason, Task> setOpenAsync,
        Func<object?, OpenChangeReason, Task> setOpenWithPayloadAsync,
        Func<string?, object?, OpenChangeReason, Task> setOpenWithTriggerIdAsync,
        Action<string, object?> setTriggerPayload,
        Action close,
        Action forceUnmount)
    {
        RootId = rootId;
        Open = open;
        Mounted = mounted;
        Nested = nested;
        Modal = modal;
        Role = role;
        DismissOnEscape = dismissOnEscape;
        DismissOnOutsidePress = dismissOnOutsidePress;
        NestedDialogCount = nestedDialogCount;
        OpenChangeReason = openChangeReason;
        TransitionStatus = transitionStatus;
        InstantType = instantType;
        TitleId = titleId;
        DescriptionId = descriptionId;
        ActiveTriggerId = activeTriggerId;
        Payload = payload;
        GetOpen = getOpen;
        GetMounted = getMounted;
        GetPayload = getPayload;
        GetTriggerElement = getTriggerElement;
        GetPopupElement = getPopupElement;
        SetTitleId = setTitleId;
        SetDescriptionId = setDescriptionId;
        RegisterTriggerElement = registerTriggerElement;
        UnregisterTriggerElement = unregisterTriggerElement;
        SetPopupElement = setPopupElement;
        SetOpenAsync = setOpenAsync;
        SetOpenWithPayloadAsync = setOpenWithPayloadAsync;
        SetOpenWithTriggerIdAsync = setOpenWithTriggerIdAsync;
        SetTriggerPayload = setTriggerPayload;
        Close = close;
        ForceUnmount = forceUnmount;
    }

    public string RootId { get; }

    public bool Open { get; set; }

    public bool Mounted { get; set; }

    public bool Nested { get; }

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

    public Func<bool> GetOpen { get; }

    public Func<bool> GetMounted { get; }

    public Func<object?> GetPayload { get; }

    public Func<ElementReference?> GetTriggerElement { get; }

    public Func<ElementReference?> GetPopupElement { get; }

    public Action<string> SetTitleId { get; }

    public Action<string> SetDescriptionId { get; }

    public Action<string, ElementReference?> RegisterTriggerElement { get; }

    public Action<string> UnregisterTriggerElement { get; }

    public Action<ElementReference?> SetPopupElement { get; }

    public Func<bool, OpenChangeReason, Task> SetOpenAsync { get; }

    public Func<object?, OpenChangeReason, Task> SetOpenWithPayloadAsync { get; }

    public Func<string?, object?, OpenChangeReason, Task> SetOpenWithTriggerIdAsync { get; }

    public Action<string, object?> SetTriggerPayload { get; }

    public Action Close { get; }

    public Action ForceUnmount { get; }
}
