namespace BlazorBaseUI.Menu;

public interface IMenuRadioGroupContext
{
    object? Value { get; }
    bool Disabled { get; }
    Task SetValueAsync(object? newValue, MenuRadioGroupChangeEventArgs eventArgs);
}

public sealed record MenuRadioGroupContext : IMenuRadioGroupContext
{
    private readonly Func<object?> getValue;
    private readonly Func<object?, MenuRadioGroupChangeEventArgs, Task> setValue;

    public MenuRadioGroupContext(
        bool disabled,
        Func<object?> getValue,
        Func<object?, MenuRadioGroupChangeEventArgs, Task> setValue)
    {
        Disabled = disabled;
        this.getValue = getValue;
        this.setValue = setValue;
    }

    public bool Disabled { get; set; }

    public object? Value => getValue();

    public async Task SetValueAsync(object? newValue, MenuRadioGroupChangeEventArgs eventArgs)
    {
        await setValue(newValue, eventArgs);
    }
}
