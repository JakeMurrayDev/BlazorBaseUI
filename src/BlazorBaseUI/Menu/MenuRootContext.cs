using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Menu;

internal sealed record MenuRootContext
{
    public MenuRootContext(
        string rootId,
        bool open,
        bool mounted,
        bool disabled,
        MenuParentType parentType,
        MenuOrientation orientation,
        bool highlightItemOnHover,
        OpenChangeReason openChangeReason,
        TransitionStatus transitionStatus,
        InstantType instantType,
        int activeIndex,
        Func<bool> getOpen,
        Func<bool> getMounted,
        Func<ElementReference?> getTriggerElement,
        Action<ElementReference?> setTriggerElement,
        Action<ElementReference?> setPositionerElement,
        Action<ElementReference?> setPopupElement,
        Action<int> setActiveIndex,
        Func<bool, OpenChangeReason, object?, Task> setOpenAsync,
        Action<OpenChangeReason, object?> emitClose)
    {
        RootId = rootId;
        Open = open;
        Mounted = mounted;
        Disabled = disabled;
        ParentType = parentType;
        Orientation = orientation;
        HighlightItemOnHover = highlightItemOnHover;
        OpenChangeReason = openChangeReason;
        TransitionStatus = transitionStatus;
        InstantType = instantType;
        ActiveIndex = activeIndex;
        GetOpen = getOpen;
        GetMounted = getMounted;
        GetTriggerElement = getTriggerElement;
        SetTriggerElement = setTriggerElement;
        SetPositionerElement = setPositionerElement;
        SetPopupElement = setPopupElement;
        SetActiveIndex = setActiveIndex;
        SetOpenAsync = setOpenAsync;
        EmitClose = emitClose;
    }

    public string RootId { get; }

    public bool Open { get; set; }

    public bool Mounted { get; set; }

    public bool Disabled { get; set; }

    public MenuParentType ParentType { get; set; }

    public MenuOrientation Orientation { get; }

    public bool HighlightItemOnHover { get; }

    public OpenChangeReason OpenChangeReason { get; set; }

    public TransitionStatus TransitionStatus { get; set; }

    public InstantType InstantType { get; set; }

    public int ActiveIndex { get; set; }

    public Func<bool> GetOpen { get; }

    public Func<bool> GetMounted { get; }

    public Func<ElementReference?> GetTriggerElement { get; }

    public Action<ElementReference?> SetTriggerElement { get; }

    public Action<ElementReference?> SetPositionerElement { get; }

    public Action<ElementReference?> SetPopupElement { get; }

    public Action<int> SetActiveIndex { get; }

    public Func<bool, OpenChangeReason, object?, Task> SetOpenAsync { get; }

    public Action<OpenChangeReason, object?> EmitClose { get; }
}
