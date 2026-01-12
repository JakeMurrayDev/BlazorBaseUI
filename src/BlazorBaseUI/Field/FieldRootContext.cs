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
    int ValidationDebounceTime { get; }
    FieldRootState State { get; }
    FieldValidation Validation { get; }

    void SetValidityData(FieldValidityData data);
    void SetTouched(bool value);
    void SetDirty(bool value);
    void SetFilled(bool value);
    void SetFocused(bool value);
    bool ShouldValidateOnChange();
    void RegisterFocusHandler(Func<ValueTask> handler);
    void Subscribe(IFieldStateSubscriber subscriber);
    void Unsubscribe(IFieldStateSubscriber subscriber);
}

public sealed class FieldRootContext : IFieldRootContext
{
    internal static FieldRootContext Default { get; } = new();

    public bool? Invalid { get; private set; }
    public string? Name { get; private set; }
    public FieldValidityData ValidityData { get; private set; } = FieldValidityData.Default;
    public bool Disabled { get; private set; }
    public bool Touched { get; private set; }
    public bool Dirty { get; private set; }
    public bool Filled { get; private set; }
    public bool Focused { get; private set; }
    public ValidationMode ValidationMode { get; private set; } = ValidationMode.OnSubmit;
    public int ValidationDebounceTime { get; private set; }
    public FieldRootState State { get; private set; } = FieldRootState.Default;
    public FieldValidation Validation { get; private set; } = null!;

    private Action<FieldValidityData>? setValidityDataCallback;
    private Action<bool>? setTouchedCallback;
    private Action<bool>? setDirtyCallback;
    private Action<bool>? setFilledCallback;
    private Action<bool>? setFocusedCallback;
    private Func<bool>? shouldValidateOnChangeCallback;
    private Action<Func<ValueTask>>? registerFocusHandlerCallback;
    private Action<IFieldStateSubscriber>? subscribeCallback;
    private Action<IFieldStateSubscriber>? unsubscribeCallback;

    private FieldRootContext() { }

    public FieldRootContext(
        Action<FieldValidityData> setValidityData,
        Action<bool> setTouched,
        Action<bool> setDirty,
        Action<bool> setFilled,
        Action<bool> setFocused,
        Func<bool> shouldValidateOnChange,
        Action<Func<ValueTask>> registerFocusHandler,
        Action<IFieldStateSubscriber> subscribe,
        Action<IFieldStateSubscriber> unsubscribe,
        FieldValidation validation)
    {
        setValidityDataCallback = setValidityData;
        setTouchedCallback = setTouched;
        setDirtyCallback = setDirty;
        setFilledCallback = setFilled;
        setFocusedCallback = setFocused;
        shouldValidateOnChangeCallback = shouldValidateOnChange;
        registerFocusHandlerCallback = registerFocusHandler;
        subscribeCallback = subscribe;
        unsubscribeCallback = unsubscribe;
        Validation = validation;
    }

    internal void Update(
        bool? invalid,
        string? name,
        FieldValidityData validityData,
        bool disabled,
        bool touched,
        bool dirty,
        bool filled,
        bool focused,
        ValidationMode validationMode,
        int validationDebounceTime,
        FieldRootState state)
    {
        Invalid = invalid;
        Name = name;
        ValidityData = validityData;
        Disabled = disabled;
        Touched = touched;
        Dirty = dirty;
        Filled = filled;
        Focused = focused;
        ValidationMode = validationMode;
        ValidationDebounceTime = validationDebounceTime;
        State = state;
    }

    public void SetValidityData(FieldValidityData data) => setValidityDataCallback?.Invoke(data);
    public void SetTouched(bool value) => setTouchedCallback?.Invoke(value);
    public void SetDirty(bool value) => setDirtyCallback?.Invoke(value);
    public void SetFilled(bool value) => setFilledCallback?.Invoke(value);
    public void SetFocused(bool value) => setFocusedCallback?.Invoke(value);
    public bool ShouldValidateOnChange() => shouldValidateOnChangeCallback?.Invoke() ?? false;
    public void RegisterFocusHandler(Func<ValueTask> handler) => registerFocusHandlerCallback?.Invoke(handler);
    public void Subscribe(IFieldStateSubscriber subscriber) => subscribeCallback?.Invoke(subscriber);
    public void Unsubscribe(IFieldStateSubscriber subscriber) => unsubscribeCallback?.Invoke(subscriber);

    internal Func<bool> ShouldValidateOnChangeFunc => ShouldValidateOnChange;
    internal Action<Func<ValueTask>> RegisterFocusHandlerFunc => handler => RegisterFocusHandler(handler);
    internal Action<IFieldStateSubscriber> SubscribeFunc => Subscribe;
    internal Action<IFieldStateSubscriber> UnsubscribeFunc => Unsubscribe;
}
