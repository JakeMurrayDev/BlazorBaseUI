namespace BlazorBaseUI.Avatar;

/// <summary>
/// Provides context for the Avatar component tree.
/// </summary>
public sealed class AvatarRootContext
{
    /// <summary>The current image loading status.</summary>
    public ImageLoadingStatus ImageLoadingStatus { get; set; }

    /// <summary>The callback to update the image loading status.</summary>
    public Action<ImageLoadingStatus> SetImageLoadingStatus { get; set; } = null!;
}
