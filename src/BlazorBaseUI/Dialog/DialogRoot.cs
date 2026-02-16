namespace BlazorBaseUI.Dialog;

/// <summary>
/// Provides a root context for dialog components, managing open/close state,
/// transitions, accessibility, and nested dialog coordination.
/// Does not render its own element.
/// </summary>
public sealed partial class DialogRoot;

/// <summary>
/// Provides the payload context for dialog content rendering via <see cref="DialogRoot.ChildContentWithPayload"/>.
/// </summary>
public readonly record struct DialogRootPayloadContext(object? Payload);
