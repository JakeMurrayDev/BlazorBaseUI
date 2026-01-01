using BlazorBaseUI.Form;

namespace BlazorBaseUI.Field;

public interface IFieldStateSubscriber
{
    void NotifyStateChanged();
}

public interface IFieldRootContext
{
    bool? Invalid { get; }
    string? Name { get; }
    FieldValidityData ValidityData { get; }
    bool Disabled { get; }
    bool Touched { get; }
    bool Dirty { get; }
    bool Filled { get; }
    bool Focused { get; }
    ValidationMode ValidationMode { get; }
    FieldRootState State { get; }
    FieldValidation Validation { get; }

    void SetTouched(bool value);
    void SetDirty(bool value);
    void SetFilled(bool value);
    void SetFocused(bool value);
    bool ShouldValidateOnChange();
    void RegisterFocusHandler(Func<ValueTask> handler);
    void Subscribe(IFieldStateSubscriber subscriber);
    void Unsubscribe(IFieldStateSubscriber subscriber);
}

public record FieldRootContext(
    bool? Invalid,
    string? Name,
    FieldValidityData ValidityData,
    Action<FieldValidityData> SetValidityData,
    bool Disabled,
    bool Touched,
    Action<bool> SetTouched,
    bool Dirty,
    Action<bool> SetDirty,
    bool Filled,
    Action<bool> SetFilled,
    bool Focused,
    Action<bool> SetFocused,
    ValidationMode ValidationMode,
    int ValidationDebounceTime,
    Func<bool> ShouldValidateOnChangeFunc,
    Action<Func<ValueTask>> RegisterFocusHandlerFunc,
    Action<IFieldStateSubscriber> SubscribeFunc,
    Action<IFieldStateSubscriber> UnsubscribeFunc,
    FieldRootState State,
    FieldValidation Validation) : IFieldRootContext
{
    public static FieldRootContext Default { get; } = new(
        Invalid: null,
        Name: null,
        ValidityData: FieldValidityData.Default,
        SetValidityData: _ => { },
        Disabled: false,
        Touched: false,
        SetTouched: _ => { },
        Dirty: false,
        SetDirty: _ => { },
        Filled: false,
        SetFilled: _ => { },
        Focused: false,
        SetFocused: _ => { },
        ValidationMode: ValidationMode.OnSubmit,
        ValidationDebounceTime: 0,
        ShouldValidateOnChangeFunc: () => false,
        RegisterFocusHandlerFunc: _ => { },
        SubscribeFunc: _ => { },
        UnsubscribeFunc: _ => { },
        State: FieldRootState.Default,
        Validation: null!);

    void IFieldRootContext.SetTouched(bool value) => SetTouched(value);
    void IFieldRootContext.SetDirty(bool value) => SetDirty(value);
    void IFieldRootContext.SetFilled(bool value) => SetFilled(value);
    void IFieldRootContext.SetFocused(bool value) => SetFocused(value);
    bool IFieldRootContext.ShouldValidateOnChange() => ShouldValidateOnChangeFunc();
    void IFieldRootContext.RegisterFocusHandler(Func<ValueTask> handler) => RegisterFocusHandlerFunc(handler);
    void IFieldRootContext.Subscribe(IFieldStateSubscriber subscriber) => SubscribeFunc(subscriber);
    void IFieldRootContext.Unsubscribe(IFieldStateSubscriber subscriber) => UnsubscribeFunc(subscriber);
}