namespace BlazorBaseUI.Field;

public interface IFieldItemContext
{
    bool Disabled { get; }
}

public sealed record FieldItemContext(bool Disabled) : IFieldItemContext
{
    internal static FieldItemContext Default { get; } = new(Disabled: false);
}