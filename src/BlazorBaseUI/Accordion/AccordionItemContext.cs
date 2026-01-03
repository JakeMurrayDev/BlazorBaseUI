namespace BlazorBaseUI.Accordion;

public interface IAccordionItemContext
{
    bool Open { get; }
    bool Disabled { get; }
    int Index { get; }
    string PanelId { get; }
    string? TriggerId { get; }
    string StringValue { get; }
    Orientation Orientation { get; }
    void SetTriggerId(string? id);
    void HandleTrigger();
}

public sealed record AccordionItemContext<TValue>(
    AccordionRootContext<TValue> RootContext,
    TValue Value,
    int Index,
    bool Disabled,
    string PanelId,
    Action TriggerHandler)
    : IAccordionItemContext
{
    public bool Open => RootContext.IsValueOpen(Value!);
    public string? TriggerId { get; private set; }
    public string StringValue { get; } = Value?.ToString() ?? string.Empty;
    public Orientation Orientation => RootContext.Orientation;

    public void SetTriggerId(string? id) => TriggerId = id;

    public void HandleTrigger() => TriggerHandler();

    public AccordionItemState<TValue> GetState() => new(
        RootContext.Value,
        Disabled,
        RootContext.Orientation,
        Index,
        Open);
}