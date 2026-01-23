using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Slider;

public sealed class SliderValue : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "output";

    private bool isComponentRenderAs;
    private bool isRegistered;
    private SliderRootState state = SliderRootState.Default;

    [CascadingParameter]
    private ISliderRootContext? Context { get; set; }

    [Parameter]
    public RenderFragment<(string[] FormattedValues, double[] Values)>? ChildContent { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<SliderRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<SliderRootState, string>? StyleValue { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (Context is not null && !isRegistered)
        {
            Context.RegisterRealtimeSubscriber();
            isRegistered = true;
        }
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
            state = Context.State;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
            return;

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationStr = state.Orientation.ToDataAttributeString() ?? "horizontal";
        var htmlFor = GetHtmlFor();
        var formattedValues = GetFormattedValues();
        var displayContent = GetDisplayContent(formattedValues);

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (!string.IsNullOrEmpty(htmlFor))
            {
                builder.AddAttribute(2, "for", htmlFor);
            }

            var ariaLive = AttributeUtilities.GetAttributeStringValue(AdditionalAttributes, "aria-live");
            if (string.IsNullOrEmpty(ariaLive))
            {
                builder.AddAttribute(3, "aria-live", "off");
            }

            if (state.Dragging)
            {
                builder.AddAttribute(4, "data-dragging", string.Empty);
            }

            builder.AddAttribute(5, "data-orientation", orientationStr);

            if (state.Disabled)
            {
                builder.AddAttribute(6, "data-disabled", string.Empty);
            }

            if (state.ReadOnly)
            {
                builder.AddAttribute(7, "data-readonly", string.Empty);
            }

            if (state.Required)
            {
                builder.AddAttribute(8, "data-required", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(9, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(10, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(11, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(12, "data-dirty", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(13, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(14, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(15, "style", resolvedStyle);
            }

            builder.AddComponentParameter(16, "ChildContent", displayContent);
            builder.AddComponentReferenceCapture(17, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (!string.IsNullOrEmpty(htmlFor))
            {
                builder.AddAttribute(2, "for", htmlFor);
            }

            var ariaLive = AttributeUtilities.GetAttributeStringValue(AdditionalAttributes, "aria-live");
            if (string.IsNullOrEmpty(ariaLive))
            {
                builder.AddAttribute(3, "aria-live", "off");
            }

            if (state.Dragging)
            {
                builder.AddAttribute(4, "data-dragging", string.Empty);
            }

            builder.AddAttribute(5, "data-orientation", orientationStr);

            if (state.Disabled)
            {
                builder.AddAttribute(6, "data-disabled", string.Empty);
            }

            if (state.ReadOnly)
            {
                builder.AddAttribute(7, "data-readonly", string.Empty);
            }

            if (state.Required)
            {
                builder.AddAttribute(8, "data-required", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(9, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(10, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(11, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(12, "data-dirty", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(13, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(14, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(15, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(16, elementReference => Element = elementReference);
            builder.AddContent(17, displayContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private string? GetHtmlFor()
    {
        if (Context is null)
            return null;

        var thumbMetadata = Context.GetAllThumbMetadata();
        if (thumbMetadata.Count == 0)
            return null;

        var inputIds = thumbMetadata
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value.InputId)
            .Where(id => !string.IsNullOrEmpty(id));

        var result = string.Join(" ", inputIds);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private string[] GetFormattedValues()
    {
        if (Context is null)
            return [];

        return [.. Context.Values.Select(v => SliderUtilities.FormatNumber(v, Context.Locale, Context.FormatOptions))];
    }

    private RenderFragment GetDisplayContent(string[] formattedValues)
    {
        if (Context is null)
            return _ => { };

        if (ChildContent is not null)
        {
            return ChildContent((formattedValues, Context.Values));
        }

        var displayValue = string.Join(" \u2013 ", formattedValues.Select((f, i) =>
            !string.IsNullOrEmpty(f) ? f : Context.Values[i].ToString(Context.Locale)));

        return builder => builder.AddContent(0, displayValue);
    }

    public void Dispose()
    {
        if (Context is not null && isRegistered)
        {
            Context.UnregisterRealtimeSubscriber();
            isRegistered = false;
        }
    }
}
