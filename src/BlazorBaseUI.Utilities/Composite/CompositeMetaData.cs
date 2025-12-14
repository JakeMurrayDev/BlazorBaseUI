namespace BlazorBaseUI.Utilities.Composite;

public sealed class CompositeMetadata<TMetadata>
{
    public int Index { get; set; } = -1;
    public TMetadata? Data { get; set; }
}