using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Dialog;

public sealed class DialogPopup : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "div";

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool hasRendered;
    private bool isComponentRenderAs;
    private DialogPopupState state;
    private DotNetObjectReference<DialogPopup>? dotNetRef;

    private Lazy<Task<IJSObjectReference>> ModuleTask => moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
        JSRuntime!.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorBaseUI/blazor-baseui-dialog.js").AsTask());

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [CascadingParameter]
    private DialogRootContext? Context { get; set; }

    [CascadingParameter]
    private DialogPortalContext? PortalContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<DialogPopupState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<DialogPopupState, string>? StyleValue { get; set; }

    [Parameter]
    public ElementReference? InitialFocus { get; set; }

    [Parameter]
    public ElementReference? FinalFocus { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException("DialogPopup must be used within a DialogRoot.");
        }

        state = new DialogPopupState(Context.Open, Context.TransitionStatus, Context.InstantType, Context.NestedDialogCount);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (Context is not null)
        {
            state = new DialogPopupState(Context.Open, Context.TransitionStatus, Context.InstantType, Context.NestedDialogCount);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);

            if (Element.HasValue && Context is not null)
            {
                Context.SetPopupElement(Element);

                try
                {
                    var module = await ModuleTask.Value;
                    await module.InvokeVoidAsync(
                        "initializePopup",
                        Context.RootId,
                        Element.Value,
                        dotNetRef,
                        Context.Modal.ToDataAttributeString(),
                        InitialFocus,
                        FinalFocus);
                }
                catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
                {
                }
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
        {
            return;
        }

        var keepMounted = PortalContext?.KeepMounted ?? false;
        var shouldRender = keepMounted || Context.Mounted;

        if (!shouldRender)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var nestedDialogsStyle = Context.NestedDialogCount > 0 ? $"--nested-dialogs: {Context.NestedDialogCount};" : null;
        var combinedStyle = CombineStyleStrings(StyleValue?.Invoke(state), nestedDialogsStyle);
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, combinedStyle);
        var isHidden = keepMounted && !Context.Open && (Context.TransitionStatus == TransitionStatus.Undefined || Context.TransitionStatus == TransitionStatus.Idle);

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            AddAttributes(builder, 2, resolvedClass, resolvedStyle, isHidden);
            builder.AddAttribute(16, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(17, component =>
            {
                Element = ((IReferencableComponent)component).Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(18, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(19, AdditionalAttributes);
            AddAttributes(builder, 20, resolvedClass, resolvedStyle, isHidden);
            builder.AddContent(33, ChildContent);
            builder.AddElementReferenceCapture(34, elementReference => Element = elementReference);
            builder.CloseElement();
        }
    }

    private void AddAttributes(RenderTreeBuilder builder, int startSequence, string? resolvedClass, string? resolvedStyle, bool isHidden)
    {
        builder.AddAttribute(startSequence, "role", Context!.Role.ToRoleString());
        builder.AddAttribute(startSequence + 1, "tabindex", "-1");

        if (Context!.Modal != ModalMode.False)
        {
            builder.AddAttribute(startSequence + 2, "aria-modal", "true");
        }

        if (!string.IsNullOrEmpty(Context.TitleId))
        {
            builder.AddAttribute(startSequence + 3, "aria-labelledby", Context.TitleId);
        }

        if (!string.IsNullOrEmpty(Context.DescriptionId))
        {
            builder.AddAttribute(startSequence + 4, "aria-describedby", Context.DescriptionId);
        }

        if (Context.Open)
        {
            builder.AddAttribute(startSequence + 5, "data-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(startSequence + 6, "data-closed", string.Empty);
        }

        if (Context.TransitionStatus == TransitionStatus.Starting)
        {
            builder.AddAttribute(startSequence + 7, "data-starting-style", string.Empty);
        }
        else if (Context.TransitionStatus == TransitionStatus.Ending)
        {
            builder.AddAttribute(startSequence + 8, "data-ending-style", string.Empty);
        }

        if (Context.Nested)
        {
            builder.AddAttribute(startSequence + 9, "data-nested", string.Empty);
        }

        if (Context.NestedDialogCount > 0)
        {
            builder.AddAttribute(startSequence + 10, "data-nested-dialog-open", string.Empty);
        }

        var instantValue = Context.InstantType.ToDataAttributeString();
        if (!string.IsNullOrEmpty(instantValue))
        {
            builder.AddAttribute(startSequence + 11, "data-instant", instantValue);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(startSequence + 12, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(startSequence + 13, "style", resolvedStyle);
        }

        if (isHidden)
        {
            builder.AddAttribute(startSequence + 14, "hidden", string.Empty);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask?.IsValueCreated == true && hasRendered && Element.HasValue && Context is not null)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("disposePopup", Context.RootId);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }

        dotNetRef?.Dispose();
    }

    private static string? CombineStyleStrings(string? style1, string? style2)
    {
        if (string.IsNullOrEmpty(style1) && string.IsNullOrEmpty(style2))
        {
            return null;
        }

        if (string.IsNullOrEmpty(style1))
        {
            return style2;
        }

        if (string.IsNullOrEmpty(style2))
        {
            return style1;
        }

        var separator = style1.TrimEnd().EndsWith(';') ? " " : "; ";
        return $"{style1}{separator}{style2}";
    }
}
