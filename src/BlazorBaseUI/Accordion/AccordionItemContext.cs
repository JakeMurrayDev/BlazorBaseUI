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
    void SetPanelId(string id);
    void SetTriggerId(string id);
    void HandleTrigger();
}

public sealed record AccordionItemContext<TValue>(
    AccordionRootContext<TValue> RootContext,
    TValue Value,
    int Index,
    bool Disabled,
    Action TriggerHandler,
    Action<string> PanelIdSetter,
    Action<string> TriggerIdSetter)
    : IAccordionItemContext
{
    public bool Open => RootContext.IsValueOpen(Value!);
    public string PanelId { get; private set; } = string.Empty;
    public string? TriggerId { get; private set; }
    public string StringValue { get; } = Value?.ToString() ?? string.Empty;
    public Orientation Orientation => RootContext.Orientation;

    public void SetPanelId(string id)
    {
        PanelId = id;
        PanelIdSetter(id);
    }

    public void SetTriggerId(string id)
    {
        TriggerId = id;
        TriggerIdSetter(id);
    }

    public void HandleTrigger() => TriggerHandler();
}
