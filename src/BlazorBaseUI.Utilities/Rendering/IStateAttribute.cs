namespace BlazorBaseUI.Utilities.Rendering;

public interface IStateAttributes
{
    IReadOnlyDictionary<string, object?> GetStateAttributes();
}