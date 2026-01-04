namespace BlazorBaseUI.Progress;

public sealed record ProgressRootState(ProgressStatus Status)
{
    public static ProgressRootState Default { get; } = new(ProgressStatus.Indeterminate);

    public IEnumerable<KeyValuePair<string, object>> GetDataAttributes()
    {
        yield return new KeyValuePair<string, object>($"data-{Status.ToDataAttributeString()}", "");
    }
}
