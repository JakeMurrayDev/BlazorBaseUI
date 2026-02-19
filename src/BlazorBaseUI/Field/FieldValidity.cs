using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Field;

/// <summary>
/// Used to display a custom message based on the field's validity.
/// Requires <see cref="ChildContent"/> to be a function that accepts field validity state as an argument.
/// </summary>
public sealed class FieldValidity : ComponentBase
{
    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    /// <summary>
    /// Gets or sets a render fragment that accepts the field validity state as a context parameter.
    /// </summary>
    [Parameter]
    public RenderFragment<FieldValidityData>? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var validityData = FieldContext?.ValidityData ?? FieldValidityData.Default;
        builder.AddContent(0, ChildContent?.Invoke(validityData));
    }
}