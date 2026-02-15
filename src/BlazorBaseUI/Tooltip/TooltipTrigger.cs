namespace BlazorBaseUI.Tooltip;

/// <summary>
/// A tooltip trigger component that can work either nested inside a TooltipRoot
/// or detached with a handle for typed payloads.
/// Renders a <c>&lt;button&gt;</c> element.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the tooltip. Use object for untyped payloads.</typeparam>
public partial class TooltipTypedTrigger<TPayload>;

/// <summary>
/// A tooltip trigger component using untyped payloads.
/// Renders a <c>&lt;button&gt;</c> element.
/// </summary>
public sealed class TooltipTrigger : TooltipTypedTrigger<object>
{
}
