namespace BlazorBaseUI.Menu;

public interface IMenuGroupContext
{
    void SetLabelId(string? id);
}

internal sealed record MenuGroupContext(Action<string?> SetLabelIdAction) : IMenuGroupContext
{
    public void SetLabelId(string? id) => SetLabelIdAction(id);
}
