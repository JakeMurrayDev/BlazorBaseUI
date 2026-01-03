namespace BlazorBaseUI.Utilities.LabelableProvider;

public sealed record LabelableContext(
    string? ControlId,
    Action<string?> SetControlId,
    string? LabelId,
    Action<string?> SetLabelId,
    List<string> MessageIds,
    Action<string, bool> UpdateMessageIds)
{
    public static LabelableContext Default { get; } = new(
        ControlId: null,
        SetControlId: _ => { },
        LabelId: null,
        SetLabelId: _ => { },
        MessageIds: [],
        UpdateMessageIds: (_, _) => { });

    public string? GetAriaDescribedBy() =>
        MessageIds.Count > 0 ? string.Join(" ", MessageIds) : null;
}