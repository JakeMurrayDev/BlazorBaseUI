namespace BlazorBaseUI.Progress;

public sealed record ProgressRootState(ProgressStatus Status)
{
    internal static ProgressRootState Default { get; } = new(ProgressStatus.Indeterminate);
}
