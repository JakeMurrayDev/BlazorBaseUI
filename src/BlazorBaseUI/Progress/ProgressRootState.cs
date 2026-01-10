namespace BlazorBaseUI.Progress;

public sealed record ProgressRootState(ProgressStatus Status)
{
    public static ProgressRootState Default { get; } = new(ProgressStatus.Indeterminate);
}
