namespace BlazorBaseUI.Avatar;

public sealed record AvatarRootState(ImageLoadingStatus ImageLoadingStatus);

public sealed record AvatarRootContext(
    ImageLoadingStatus ImageLoadingStatus,
    Action<ImageLoadingStatus> SetImageLoadingStatus,
    AvatarRootState State);
