namespace BlazorBaseUI.PreviewCard;

/// <summary>
/// Represents the state of the <see cref="PreviewCardViewport"/> component.
/// </summary>
/// <param name="Instant">The current instant transition type.</param>
public readonly record struct PreviewCardViewportState(PreviewCardInstantType Instant);
