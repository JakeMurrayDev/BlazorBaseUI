namespace BlazorBaseUI.Utilities.Rendering;

public sealed class RenderProps
{
    public RenderProps(
        IReadOnlyDictionary<string, object?> attributes,
        object? state = null)
    {
        Attributes = attributes;
        State = state;
    }

    public IReadOnlyDictionary<string, object?> Attributes { get; }
    public object? State { get; }
}
