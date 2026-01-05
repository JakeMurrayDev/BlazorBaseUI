namespace BlazorBaseUI.Avatar;

public sealed record AvatarRootContext(
    ImageLoadingStatus ImageLoadingStatus,
    Action<ImageLoadingStatus> SetImageLoadingStatus);
