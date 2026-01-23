namespace BlazorBaseUI.Progress;

public sealed record ProgressRootContext(
    string FormattedValue,
    double Max,
    double Min,
    double? Value,
    ProgressRootState State,
    ProgressStatus Status,
    Action<string?> SetLabelIdAction);
