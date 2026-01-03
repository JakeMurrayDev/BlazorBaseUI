namespace BlazorBaseUI.Fieldset;

public sealed record FieldsetLegendState(bool Disabled)
{
    public static FieldsetLegendState Default { get; } = new(Disabled: false);

    public IEnumerable<KeyValuePair<string, object>> GetDataAttributes()
    {
        if (Disabled)
            yield return new KeyValuePair<string, object>("data-Disabled", "");
    }
}
