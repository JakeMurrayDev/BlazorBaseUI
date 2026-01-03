using BlazorBaseUI.Field;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Slider;

public interface ISliderRootContext
{
    int ActiveThumbIndex { get; }
    int LastUsedThumbIndex { get; }
    ElementReference ControlElement { get; }
    bool Dragging { get; }
    bool Disabled { get; }
    bool ReadOnly { get; }
    double LargeStep { get; }
    double Max { get; }
    double Min { get; }
    int MinStepsBetweenValues { get; }
    string? Name { get; }
    Orientation Orientation { get; }
    double Step { get; }
    ThumbCollisionBehavior ThumbCollisionBehavior { get; }
    ThumbAlignment ThumbAlignment { get; }
    double[] Values { get; }
    SliderRootState State { get; }
    string? LabelId { get; }
    NumberFormatOptions? FormatOptions { get; }
    string? Locale { get; }
    FieldValidation? Validation { get; }

    void SetActiveThumbIndex(int index);
    void SetDragging(bool dragging);
    void SetValue(double[] newValues, SliderChangeReason reason, int activeThumbIndex);
    void SetValueSilent(double[] newValues);
    void CommitValue(double[] values, SliderChangeReason reason);
    void HandleInputChange(double value, int index, SliderChangeReason reason);
    void RegisterThumb(int index, ThumbMetadata metadata);
    void UnregisterThumb(int index);
    ThumbMetadata? GetThumbMetadata(int index);
    IReadOnlyDictionary<int, ThumbMetadata> GetAllThumbMetadata();
    void SetControlElement(ElementReference element);
    void SetIndicatorElement(ElementReference element);
    ElementReference? GetIndicatorElement();
    void SetIndicatorPosition(double? start, double? end);
    (double? Start, double? End) GetIndicatorPosition();
}

public record SliderRootContext(
    int ActiveThumbIndex,
    int LastUsedThumbIndex,
    ElementReference ControlElement,
    bool Dragging,
    bool Disabled,
    bool ReadOnly,
    double LargeStep,
    double Max,
    double Min,
    int MinStepsBetweenValues,
    string? Name,
    Orientation Orientation,
    double Step,
    ThumbCollisionBehavior ThumbCollisionBehavior,
    ThumbAlignment ThumbAlignment,
    double[] Values,
    SliderRootState State,
    string? LabelId,
    NumberFormatOptions? FormatOptions,
    string? Locale,
    FieldValidation? Validation,
    Action<int> SetActiveThumbIndexAction,
    Action<bool> SetDraggingAction,
    Action<double[], SliderChangeReason, int> SetValueAction,
    Action<double[]> SetValueSilentAction,
    Action<double[], SliderChangeReason> CommitValueAction,
    Action<double, int, SliderChangeReason> HandleInputChangeAction,
    Action<int, ThumbMetadata> RegisterThumbAction,
    Action<int> UnregisterThumbAction,
    Func<int, ThumbMetadata?> GetThumbMetadataFunc,
    Func<IReadOnlyDictionary<int, ThumbMetadata>> GetAllThumbMetadataFunc,
    Action<ElementReference> SetControlElementAction,
    Action<ElementReference> SetIndicatorElementAction,
    Func<ElementReference?> GetIndicatorElementFunc,
    Action<double?, double?> SetIndicatorPositionAction,
    Func<(double? Start, double? End)> GetIndicatorPositionFunc) : ISliderRootContext
{
    public static SliderRootContext Default { get; } = new(
        ActiveThumbIndex: -1,
        LastUsedThumbIndex: -1,
        ControlElement: default,
        Dragging: false,
        Disabled: false,
        ReadOnly: false,
        LargeStep: 10,
        Max: 100,
        Min: 0,
        MinStepsBetweenValues: 0,
        Name: null,
        Orientation: Orientation.Horizontal,
        Step: 1,
        ThumbCollisionBehavior: ThumbCollisionBehavior.Push,
        ThumbAlignment: ThumbAlignment.Center,
        Values: [0],
        State: SliderRootState.Default,
        LabelId: null,
        FormatOptions: null,
        Locale: null,
        Validation: null,
        SetActiveThumbIndexAction: _ => { },
        SetDraggingAction: _ => { },
        SetValueAction: (_, _, _) => { },
        SetValueSilentAction: _ => { },
        CommitValueAction: (_, _) => { },
        HandleInputChangeAction: (_, _, _) => { },
        RegisterThumbAction: (_, _) => { },
        UnregisterThumbAction: _ => { },
        GetThumbMetadataFunc: _ => null,
        GetAllThumbMetadataFunc: () => new Dictionary<int, ThumbMetadata>(),
        SetControlElementAction: _ => { },
        SetIndicatorElementAction: _ => { },
        GetIndicatorElementFunc: () => null,
        SetIndicatorPositionAction: (_, _) => { },
        GetIndicatorPositionFunc: () => (null, null));

    void ISliderRootContext.SetActiveThumbIndex(int index) => SetActiveThumbIndexAction(index);
    void ISliderRootContext.SetDragging(bool dragging) => SetDraggingAction(dragging);
    void ISliderRootContext.SetValue(double[] newValues, SliderChangeReason reason, int activeThumbIndex) =>
        SetValueAction(newValues, reason, activeThumbIndex);
    void ISliderRootContext.SetValueSilent(double[] newValues) =>
        SetValueSilentAction(newValues);
    void ISliderRootContext.CommitValue(double[] values, SliderChangeReason reason) =>
        CommitValueAction(values, reason);
    void ISliderRootContext.HandleInputChange(double value, int index, SliderChangeReason reason) =>
        HandleInputChangeAction(value, index, reason);
    void ISliderRootContext.RegisterThumb(int index, ThumbMetadata metadata) => RegisterThumbAction(index, metadata);
    void ISliderRootContext.UnregisterThumb(int index) => UnregisterThumbAction(index);
    ThumbMetadata? ISliderRootContext.GetThumbMetadata(int index) => GetThumbMetadataFunc(index);
    IReadOnlyDictionary<int, ThumbMetadata> ISliderRootContext.GetAllThumbMetadata() => GetAllThumbMetadataFunc();
    void ISliderRootContext.SetControlElement(ElementReference element) => SetControlElementAction(element);
    void ISliderRootContext.SetIndicatorElement(ElementReference element) => SetIndicatorElementAction(element);
    ElementReference? ISliderRootContext.GetIndicatorElement() => GetIndicatorElementFunc();
    void ISliderRootContext.SetIndicatorPosition(double? start, double? end) => SetIndicatorPositionAction(start, end);
    (double? Start, double? End) ISliderRootContext.GetIndicatorPosition() => GetIndicatorPositionFunc();
}

public record NumberFormatOptions(
    string? Style = null,
    string? Currency = null,
    int? MinimumFractionDigits = null,
    int? MaximumFractionDigits = null,
    int? MinimumIntegerDigits = null,
    int? MinimumSignificantDigits = null,
    int? MaximumSignificantDigits = null,
    bool? UseGrouping = null);
