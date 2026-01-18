using BlazorBaseUI.Popover;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tooltip;

internal sealed class TooltipRootContext
{
    public TooltipRootContext(
        string rootId,
        string popupId,
        bool open,
        bool mounted,
        bool disabled,
        TooltipOpenChangeReason openChangeReason,
        Popover.TransitionStatus transitionStatus,
        TooltipInstantType instantType,
        TrackCursorAxis trackCursorAxis,
        bool disableHoverablePopup,
        string? activeTriggerId,
        object? payload,
        Func<bool> getOpen,
        Func<bool> getMounted,
        Func<object?> getPayload,
        Func<ElementReference?> getTriggerElement,
        Action<string, ElementReference?> registerTriggerElement,
        Action<string> unregisterTriggerElement,
        Action<ElementReference?> setPositionerElement,
        Action<ElementReference?> setPopupElement,
        Func<bool, TooltipOpenChangeReason, string?, Task> setOpenAsync,
        Action<string, object?> setTriggerPayload,
        Action forceUnmount)
    {
        RootId = rootId;
        PopupId = popupId;
        Open = open;
        Mounted = mounted;
        Disabled = disabled;
        OpenChangeReason = openChangeReason;
        TransitionStatus = transitionStatus;
        InstantType = instantType;
        TrackCursorAxis = trackCursorAxis;
        DisableHoverablePopup = disableHoverablePopup;
        ActiveTriggerId = activeTriggerId;
        Payload = payload;
        GetOpen = getOpen;
        GetMounted = getMounted;
        GetPayload = getPayload;
        GetTriggerElement = getTriggerElement;
        RegisterTriggerElement = registerTriggerElement;
        UnregisterTriggerElement = unregisterTriggerElement;
        SetPositionerElement = setPositionerElement;
        SetPopupElement = setPopupElement;
        SetOpenAsync = setOpenAsync;
        SetTriggerPayload = setTriggerPayload;
        ForceUnmount = forceUnmount;
    }

    public string RootId { get; }

    public string PopupId { get; }

    public bool Open { get; set; }

    public bool Mounted { get; set; }

    public bool Disabled { get; set; }

    public TooltipOpenChangeReason OpenChangeReason { get; set; }

    public Popover.TransitionStatus TransitionStatus { get; set; }

    public TooltipInstantType InstantType { get; set; }

    public TrackCursorAxis TrackCursorAxis { get; set; }

    public bool DisableHoverablePopup { get; set; }

    public string? ActiveTriggerId { get; set; }

    public object? Payload { get; set; }

    public Func<bool> GetOpen { get; }

    public Func<bool> GetMounted { get; }

    public Func<object?> GetPayload { get; }

    public Func<ElementReference?> GetTriggerElement { get; }

    public Action<string, ElementReference?> RegisterTriggerElement { get; }

    public Action<string> UnregisterTriggerElement { get; }

    public Action<ElementReference?> SetPositionerElement { get; }

    public Action<ElementReference?> SetPopupElement { get; }

    public Func<bool, TooltipOpenChangeReason, string?, Task> SetOpenAsync { get; }

    public Action<string, object?> SetTriggerPayload { get; }

    public Action ForceUnmount { get; }
}
