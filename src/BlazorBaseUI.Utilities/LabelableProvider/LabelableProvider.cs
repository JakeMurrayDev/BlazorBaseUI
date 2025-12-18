using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Utilities.LabelableProvider;

public sealed class LabelableProvider : ComponentBase
{
    private string? controlId;
    private string? labelId;
    private List<string> messageIds = [];
    private LabelableContext context = null!;

    [Parameter]
    public string? InitialControlId { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        controlId = InitialControlId ?? Guid.NewGuid().ToString("N")[..8];
        context = CreateContext();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<LabelableContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    private LabelableContext CreateContext() => new(
        ControlId: controlId,
        SetControlId: SetControlId,
        LabelId: labelId,
        SetLabelId: SetLabelId,
        MessageIds: messageIds,
        UpdateMessageIds: UpdateMessageIds);

    private void SetControlId(string? id)
    {
        if (controlId == id) return;
        controlId = id;
        context = CreateContext();
        _ = InvokeAsync(StateHasChanged);
    }

    private void SetLabelId(string? id)
    {
        if (labelId == id) return;
        labelId = id;
        context = CreateContext();
        _ = InvokeAsync(StateHasChanged);
    }

    private void UpdateMessageIds(string id, bool add)
    {
        if (add)
        {
            if (messageIds.Contains(id)) return;
            messageIds = [.. messageIds, id];
        }
        else
        {
            if (!messageIds.Contains(id)) return;
            messageIds = messageIds.Where(m => m != id).ToList();
        }
        context = CreateContext();
        _ = InvokeAsync(StateHasChanged);
    }
}