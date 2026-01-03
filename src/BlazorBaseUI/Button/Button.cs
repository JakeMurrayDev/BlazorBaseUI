using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Button;

public class Button : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "button";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;
    private bool hasRendered;
    private bool previousDisabled;
    private bool previousFocusableWhenDisabled;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool FocusableWhenDisabled { get; set; }

    [Parameter]
    public bool NativeButton { get; set; } = true;

    [Parameter]
    public int TabIndex { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<ButtonState?, string>? ClassValue { get; set; }

    [Parameter]
    public Func<ButtonState?, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private ButtonState State => new(Disabled);

    public Button()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/BlazorBaseUI/blazor-baseui-button.js").AsTask());
    }

    protected override void OnParametersSet()
    {
        if (hasRendered && !NativeButton)
        {
            if (Disabled != previousDisabled || FocusableWhenDisabled != previousFocusableWhenDisabled)
            {
                previousDisabled = Disabled;
                previousFocusableWhenDisabled = FocusableWhenDisabled;
                _ = UpdateJsStateAsync();
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = ResolveClass(State);
        var resolvedStyle = ResolveStyle(State);
        var attributes = BuildAttributes();

        var attributesWithClassAndStyle = new Dictionary<string, object>(attributes);
        if (!string.IsNullOrEmpty(resolvedClass))
        {
            attributesWithClassAndStyle["class"] = resolvedClass;
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            attributesWithClassAndStyle["style"] = resolvedStyle;
        }

        if (RenderAs is not null)
        {
            builder.OpenComponent(1, RenderAs);
            builder.AddMultipleAttributes(2, attributesWithClassAndStyle);
            builder.AddAttribute(3, "ChildContent", ChildContent);
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(4, tag);
        builder.AddMultipleAttributes(5, attributesWithClassAndStyle);
        builder.AddElementReferenceCapture(6, SetElementReference);
        builder.AddContent(7, ChildContent);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            previousDisabled = Disabled;
            previousFocusableWhenDisabled = FocusableWhenDisabled;

            if (!NativeButton && Element.HasValue)
            {
                await InitializeJsAsync();
            }
        }
    }

    private string? ResolveClass(ButtonState state)
    {
        return AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
    }

    private string? ResolveStyle(ButtonState state)
    {
        return AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
    }

    private Dictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key != "class" && attr.Key != "style")
                {
                    attributes[attr.Key] = attr.Value;
                }
            }
        }

        if (NativeButton)
        {
            attributes["type"] = "button";

            if (FocusableWhenDisabled)
            {
                attributes["aria-Disabled"] = Disabled;
                attributes["tabindex"] = TabIndex;
            }
            else if (Disabled)
            {
                attributes["Disabled"] = true;
            }
            else
            {
                attributes["tabindex"] = TabIndex;
            }
        }
        else
        {
            attributes["role"] = "button";

            if (Disabled)
            {
                attributes["aria-Disabled"] = true;
                attributes["tabindex"] = FocusableWhenDisabled ? TabIndex : -1;
            }
            else
            {
                attributes["tabindex"] = TabIndex;
            }
        }

        return attributes;
    }

    private void SetElementReference(ElementReference elementReference)
    {
        Element = elementReference;
    }

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initialize", Element, Disabled, FocusableWhenDisabled);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdateJsStateAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("updateState", Element, Disabled, FocusableWhenDisabled);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated && Element.HasValue)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("dispose", Element);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }
}