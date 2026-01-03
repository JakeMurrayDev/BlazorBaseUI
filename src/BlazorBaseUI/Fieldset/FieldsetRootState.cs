namespace BlazorBaseUI.Fieldset;

public sealed record FieldsetRootState(bool Disabled)
{
    public static FieldsetRootState Default { get; } = new(Disabled: false);

    public IEnumerable<KeyValuePair<string, object>> GetDataAttributes()
    {
        if (Disabled)
            yield return new KeyValuePair<string, object>("data-Disabled", "");
    }
}
