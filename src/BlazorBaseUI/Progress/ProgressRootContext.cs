namespace BlazorBaseUI.Progress;

public interface IProgressRootContext
{
    string FormattedValue { get; }
    double Max { get; }
    double Min { get; }
    double? Value { get; }
    ProgressRootState State { get; }
    ProgressStatus Status { get; }
    void SetLabelId(string? id);
}

public sealed record ProgressRootContext(
    string FormattedValue,
    double Max,
    double Min,
    double? Value,
    ProgressRootState State,
    ProgressStatus Status,
    Action<string?> SetLabelIdAction) : IProgressRootContext
{
    internal static ProgressRootContext Default { get; } = new(
        FormattedValue: string.Empty,
        Max: 100,
        Min: 0,
        Value: null,
        State: ProgressRootState.Default,
        Status: ProgressStatus.Indeterminate,
        SetLabelIdAction: _ => { });

    void IProgressRootContext.SetLabelId(string? id) => SetLabelIdAction(id);
}
