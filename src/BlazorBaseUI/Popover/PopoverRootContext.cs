using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Popover;

internal sealed class PopoverRootContext
{
    public PopoverRootContext(
        string rootId,
        bool open,
        bool mounted,
        ModalMode modal,
        OpenChangeReason openChangeReason,
        TransitionStatus transitionStatus,
        InstantType instantType,
        string? titleId,
        string? descriptionId,
        string? activeTriggerId,
        Func<bool> getOpen,
        Func<bool> getMounted,
        Func<ElementReference?> getTriggerElement,
        Func<ElementReference?> getPositionerElement,
        Func<ElementReference?> getPopupElement,
        Action<string> setTitleId,
        Action<string> setDescriptionId,
        Action<ElementReference?> setTriggerElement,
        Action<ElementReference?> setPositionerElement,
        Action<ElementReference?> setPopupElement,
        Func<bool, OpenChangeReason, Task> setOpenAsync,
        Action close,
        Action forceUnmount)
    {
        RootId = rootId;
        Open = open;
        Mounted = mounted;
        Modal = modal;
        OpenChangeReason = openChangeReason;
        TransitionStatus = transitionStatus;
        InstantType = instantType;
        TitleId = titleId;
        DescriptionId = descriptionId;
        ActiveTriggerId = activeTriggerId;
        GetOpen = getOpen;
        GetMounted = getMounted;
        GetTriggerElement = getTriggerElement;
        GetPositionerElement = getPositionerElement;
        GetPopupElement = getPopupElement;
        SetTitleId = setTitleId;
        SetDescriptionId = setDescriptionId;
        SetTriggerElement = setTriggerElement;
        SetPositionerElement = setPositionerElement;
        SetPopupElement = setPopupElement;
        SetOpenAsync = setOpenAsync;
        Close = close;
        ForceUnmount = forceUnmount;
    }

    public string RootId { get; }

    public bool Open { get; set; }

    public bool Mounted { get; set; }

    public ModalMode Modal { get; set; }

    public OpenChangeReason OpenChangeReason { get; set; }

    public TransitionStatus TransitionStatus { get; set; }

    public InstantType InstantType { get; set; }

    public string? TitleId { get; set; }

    public string? DescriptionId { get; set; }

    public string? ActiveTriggerId { get; set; }

    public Func<bool> GetOpen { get; }

    public Func<bool> GetMounted { get; }

    public Func<ElementReference?> GetTriggerElement { get; }

    public Func<ElementReference?> GetPositionerElement { get; }

    public Func<ElementReference?> GetPopupElement { get; }

    public Action<string> SetTitleId { get; }

    public Action<string> SetDescriptionId { get; }

    public Action<ElementReference?> SetTriggerElement { get; }

    public Action<ElementReference?> SetPositionerElement { get; }

    public Action<ElementReference?> SetPopupElement { get; }

    public Func<bool, OpenChangeReason, Task> SetOpenAsync { get; }

    public Action Close { get; }

    public Action ForceUnmount { get; }
}
