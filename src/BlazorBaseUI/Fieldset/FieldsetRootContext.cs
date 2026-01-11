namespace BlazorBaseUI.Fieldset;

public interface IFieldsetRootContext
{
    string? LegendId { get; }
    bool Disabled { get; }
    void SetLegendId(string? id);
}

public sealed record FieldsetRootContext(
    string? LegendId,
    Action<string?> SetLegendId,
    bool Disabled) : IFieldsetRootContext
{
    internal static FieldsetRootContext Default { get; } = new(
        LegendId: null,
        SetLegendId: _ => { },
        Disabled: false);

    void IFieldsetRootContext.SetLegendId(string? id) => SetLegendId(id);
}
