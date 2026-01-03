using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Slider;

public sealed class SliderValue : ComponentBase
{
    private const string DefaultTag = "output";

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

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
            return;

        var state = Context.State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var attributes = BuildValueAttributes(state);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        var formattedValues = GetFormattedValues();
        var displayContent = GetDisplayContent(formattedValues);

        if (RenderAs is not null)
        {
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddComponentParameter(2, "ChildContent", displayContent);
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, attributes);
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, displayContent);
        builder.CloseElement();
    }

    private Dictionary<string, object> BuildValueAttributes(SliderRootState state)
    {
        var attributes = new Dictionary<string, object>();

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key is not "class" and not "style")
                    attributes[attr.Key] = attr.Value;
            }
        }
        
        if (attributes.TryGetValue("aria-live", out var ariaLive) && ariaLive is string s &&
            string.IsNullOrEmpty(s))
        {
            attributes["aria-live"] = "off";
        }
        
        var htmlFor = GetHtmlFor();
        if (!string.IsNullOrEmpty(htmlFor))
            attributes["for"] = htmlFor;

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
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
}
