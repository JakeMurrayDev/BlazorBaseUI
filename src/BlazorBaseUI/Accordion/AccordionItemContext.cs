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

public sealed class AccordionItemContext<TValue> : IAccordionItemContext
{
    private readonly AccordionRootContext<TValue> rootContext;
    private readonly TValue value;
    private readonly Action triggerHandler;

    public AccordionItemContext(
        AccordionRootContext<TValue> rootContext,
        TValue value,
        int index,
        bool disabled,
        string panelId,
        Action triggerHandler)
    {
        this.rootContext = rootContext;
        this.value = value;
        this.triggerHandler = triggerHandler;
        Index = index;
        Disabled = disabled;
        PanelId = panelId;
        StringValue = value?.ToString() ?? string.Empty;
    }

    public bool Open => rootContext.IsValueOpen(value!);
    public bool Disabled { get; }
    public int Index { get; }
    public string PanelId { get; }
    public string? TriggerId { get; private set; }
    public string StringValue { get; }
    public Orientation Orientation => rootContext.Orientation;

    public void SetTriggerId(string? id) => TriggerId = id;

    public void HandleTrigger() => triggerHandler();

    public AccordionItemState<TValue> GetState() => new(
        rootContext.Value,
        Disabled,
        rootContext.Orientation,
        Index,
        Open);
}