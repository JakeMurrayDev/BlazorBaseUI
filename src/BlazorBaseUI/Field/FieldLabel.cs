using BlazorBaseUI.Utilities.LabelableProvider;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Field;

public sealed class FieldLabel : ComponentBase, IReferencableComponent, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "label";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-label.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private string? defaultId;
    private string labelId = null!;
    private bool isComponentRenderAs;

    private FieldRootState State => FieldContext?.State ?? FieldRootState.Default;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [Parameter]
    public bool NativeLabel { get; set; } = true;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    public FieldLabel()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        labelId = ResolvedId;
        LabelableContext?.SetLabelId(labelId);
        FieldContext?.Subscribe(this);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "id", labelId);

            if (NativeLabel)
            {
                if (!string.IsNullOrEmpty(LabelableContext?.ControlId))
                {
                    builder.AddAttribute(3, "for", LabelableContext.ControlId);
                }
            }
            else
            {
                builder.AddAttribute(4, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
            }

            if (state.Disabled)
            {
                builder.AddAttribute(5, "data-disabled", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(6, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(7, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(8, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(9, "data-dirty", string.Empty);
            }

            if (state.Filled)
            {
                builder.AddAttribute(10, "data-filled", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(11, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(12, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(13, "style", resolvedStyle);
            }

            builder.AddAttribute(14, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(15, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "id", labelId);

            if (NativeLabel)
            {
                if (!string.IsNullOrEmpty(LabelableContext?.ControlId))
                {
                    builder.AddAttribute(3, "for", LabelableContext.ControlId);
                }
            }
            else
            {
                builder.AddAttribute(4, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
            }

            if (state.Disabled)
            {
                builder.AddAttribute(5, "data-disabled", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(6, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(7, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(8, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(9, "data-dirty", string.Empty);
            }

            if (state.Filled)
            {
                builder.AddAttribute(10, "data-filled", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(11, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(12, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(13, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(14, elementReference => Element = elementReference);
            builder.AddContent(15, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && NativeLabel)
        {
            try
            {
                var module = await moduleTask.Value;

                if (Element.HasValue)
                {
                    await module.InvokeVoidAsync("addLabelMouseDownListener", Element.Value);
                }
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        LabelableContext?.SetLabelId(null);
        FieldContext?.Unsubscribe(this);

        if (NativeLabel && moduleTask.IsValueCreated && Element.HasValue)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("removeLabelMouseDownListener", Element.Value);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    void IFieldStateSubscriber.NotifyStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task HandleClick(MouseEventArgs e)
    {
        if (!NativeLabel && !string.IsNullOrEmpty(LabelableContext?.ControlId))
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("focusControlById", LabelableContext.ControlId);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }

        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }
}
