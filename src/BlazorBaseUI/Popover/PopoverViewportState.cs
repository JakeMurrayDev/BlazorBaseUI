namespace BlazorBaseUI.Popover;

/// <summary>
/// Represents the state of the <see cref="PopoverViewport"/> component.
/// </summary>
/// <param name="Open">Indicates whether the popover is currently open.</param>
/// <param name="TransitionStatus">The current transition status of the viewport.</param>
public readonly record struct PopoverViewportState(bool Open, TransitionStatus TransitionStatus);
