using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Field;

public sealed class FieldValidation : IDisposable
{
    private readonly Func<FieldValidityData> getValidityData;
    private readonly Action<FieldValidityData> setValidityData;
    private readonly Func<object?, Task<string[]?>>? validate;
    private readonly Func<bool>? getInvalid;
    private readonly int debounceTime;
    private readonly Action requestStateChange;

    private CancellationTokenSource? debounceCts;

    public ElementReference? InputReference { get; set; }

    public FieldValidation(
        Func<FieldValidityData> getValidityData,
        Action<FieldValidityData> setValidityData,
        Func<object?, Task<string[]?>>? validate,
        Func<bool>? getInvalid,
        int debounceTime,
        Action requestStateChange)
    {
        this.getValidityData = getValidityData;
        this.setValidityData = setValidityData;
        this.validate = validate;
        this.getInvalid = getInvalid;
        this.debounceTime = debounceTime;
        this.requestStateChange = requestStateChange;
    }

    public async Task CommitAsync(object? value, bool revalidateOnly = false)
    {
        var currentData = getValidityData();

        if (revalidateOnly && currentData.State.Valid != false)
        {
            if (!ReferenceEquals(currentData.Value, value))
            {
                setValidityData(currentData with { Value = value });
            }
            return;
        }

        var errors = new List<string>();
        var validityState = currentData.State;

        if (validate is not null)
        {
            var customErrors = await validate(value);
            if (customErrors is { Length: > 0 })
            {
                errors.AddRange(customErrors);
            }
        }

        var isInvalid = getInvalid?.Invoke() ?? false;
        var hasErrors = errors.Count > 0 || isInvalid;

        var newValidityState = validityState with
        {
            Valid = !hasErrors,
            CustomError = errors.Count > 0
        };

        var newData = currentData with
        {
            State = newValidityState,
            Errors = [.. errors],
            Error = errors.FirstOrDefault() ?? string.Empty,
            Value = value
        };

        setValidityData(newData);
        requestStateChange();
    }

    public void CommitDebounced(object? value)
    {
        debounceCts?.Cancel();
        debounceCts = new CancellationTokenSource();
        var token = debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(debounceTime, token);
                if (!token.IsCancellationRequested)
                {
                    await CommitAsync(value);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }, token);
    }

    public void SetInitialValue(object? value)
    {
        var currentData = getValidityData();
        if (currentData.InitialValue is null)
        {
            setValidityData(currentData with { InitialValue = value });
        }
    }

    public void Dispose()
    {
        debounceCts?.Cancel();
        debounceCts?.Dispose();
    }
}