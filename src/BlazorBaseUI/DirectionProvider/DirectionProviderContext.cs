namespace BlazorBaseUI.DirectionProvider;

/// <summary>
/// Provides the current text direction context to descendant components.
/// </summary>
/// <param name="Direction">The reading direction of the text.</param>
public sealed record DirectionProviderContext(Direction Direction);
