namespace BlazorBaseUI;

public readonly struct ClassValue<TState>
{
    private readonly string? staticValue;
    private readonly Func<TState?, string?>? valueFunc;
    private readonly bool isFunc;

    private ClassValue(string? value)
    {
        staticValue = value;
        valueFunc = null;
        isFunc = false;
    }

    private ClassValue(Func<TState?, string?> func)
    {
        staticValue = null;
        valueFunc = func;
        isFunc = true;
    }

    public static implicit operator ClassValue<TState>(string? value) => new(value);

    public static implicit operator ClassValue<TState>(Func<TState?, string?> func) => new(func);

    public string? Resolve(TState? state) => isFunc ? valueFunc?.Invoke(state) : staticValue;

    public bool HasValue => isFunc ? valueFunc is not null : staticValue is not null;
}

public readonly struct StyleValue<TState>
{
    private readonly string? staticValue;
    private readonly Func<TState?, string?>? valueFunc;
    private readonly bool isFunc;

    private StyleValue(string? value)
    {
        staticValue = value;
        valueFunc = null;
        isFunc = false;
    }

    private StyleValue(Func<TState?, string?> func)
    {
        staticValue = null;
        valueFunc = func;
        isFunc = true;
    }

    public static implicit operator StyleValue<TState>(string? value) => new(value);

    public static implicit operator StyleValue<TState>(Func<TState?, string?> func) => new(func);

    public string? Resolve(TState? state) => isFunc ? valueFunc?.Invoke(state) : staticValue;

    public bool HasValue => isFunc ? valueFunc is not null : staticValue is not null;
}