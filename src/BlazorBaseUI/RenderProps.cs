using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI;

/// <summary>
/// Carries the fully-computed attributes and component state into a
/// consumer-supplied <c>Render</c> function, allowing the consumer to
/// selectively spread, filter, or wrap them.
/// <para>
/// This is the Blazor equivalent of Base UI's
/// <c>render={(props, state) =&gt; ...}</c> prop.
/// </para>
/// </summary>
/// <typeparam name="TState">The component's public state record.</typeparam>
/// <param name="Attributes">
/// A pre-built dictionary of every HTML attribute the component would
/// normally render (aria-*, data-*, role, class, style, etc.).
/// The consumer can splat it via <c>@attributes="context.Attributes"</c>.
/// </param>
/// <param name="State">The component's current public state.</param>
/// <param name="ChildContent">
/// The original <c>ChildContent</c> passed to the component, so the
/// consumer's render function can place it wherever it likes.
/// </param>
public sealed record RenderProps<TState>(
    IReadOnlyDictionary<string, object> Attributes,
    TState State,
    RenderFragment? ChildContent);
