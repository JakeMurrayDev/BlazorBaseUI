namespace BlazorBaseUI.Field;

public interface IFieldItemContext
{
    bool Disabled { get; }
}

public record FieldItemContext(bool Disabled) : IFieldItemContext
{
    public static FieldItemContext Default { get; } = new(Disabled: false);
}