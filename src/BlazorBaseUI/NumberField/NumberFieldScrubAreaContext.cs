using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.NumberField;

public interface INumberFieldScrubAreaContext
{
    bool IsScrubbing { get; }
    bool IsTouchInput { get; }
    bool IsPointerLockDenied { get; }
    ScrubDirection Direction { get; }
    int PixelSensitivity { get; }
    int? TeleportDistance { get; }

    void SetCursorElement(ElementReference? element);
    ElementReference? GetCursorElement();
    void SetScrubAreaElement(ElementReference? element);
    ElementReference? GetScrubAreaElement();
}

public sealed class NumberFieldScrubAreaContext : INumberFieldScrubAreaContext
{
    internal static NumberFieldScrubAreaContext Default { get; } = new();

    public bool IsScrubbing { get; private set; }
    public bool IsTouchInput { get; private set; }
    public bool IsPointerLockDenied { get; private set; }
    public ScrubDirection Direction { get; private set; } = ScrubDirection.Horizontal;
    public int PixelSensitivity { get; private set; } = 2;
    public int? TeleportDistance { get; private set; }

    private Action<ElementReference?>? setCursorElementCallback;
    private Func<ElementReference?>? getCursorElementCallback;
    private Action<ElementReference?>? setScrubAreaElementCallback;
    private Func<ElementReference?>? getScrubAreaElementCallback;

    private NumberFieldScrubAreaContext() { }

    public NumberFieldScrubAreaContext(
        Action<ElementReference?> setCursorElement,
        Func<ElementReference?> getCursorElement,
        Action<ElementReference?> setScrubAreaElement,
        Func<ElementReference?> getScrubAreaElement)
    {
        setCursorElementCallback = setCursorElement;
        getCursorElementCallback = getCursorElement;
        setScrubAreaElementCallback = setScrubAreaElement;
        getScrubAreaElementCallback = getScrubAreaElement;
    }

    internal void Update(
        bool isScrubbing,
        bool isTouchInput,
        bool isPointerLockDenied,
        ScrubDirection direction,
        int pixelSensitivity,
        int? teleportDistance)
    {
        IsScrubbing = isScrubbing;
        IsTouchInput = isTouchInput;
        IsPointerLockDenied = isPointerLockDenied;
        Direction = direction;
        PixelSensitivity = pixelSensitivity;
        TeleportDistance = teleportDistance;
    }

    public void SetCursorElement(ElementReference? element) =>
        setCursorElementCallback?.Invoke(element);

    public ElementReference? GetCursorElement() =>
        getCursorElementCallback?.Invoke();

    public void SetScrubAreaElement(ElementReference? element) =>
        setScrubAreaElementCallback?.Invoke(element);

    public ElementReference? GetScrubAreaElement() =>
        getScrubAreaElementCallback?.Invoke();
}
