namespace BlazorBaseUI.Tooltip;

/// <summary>
/// Represents the state of the <see cref="TooltipViewport"/> component.
/// </summary>
/// <param name="Instant">The current instant transition type.</param>
public readonly record struct TooltipViewportState(TooltipInstantType Instant);
