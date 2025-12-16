namespace BlazorBaseUI.Avatar;

public record AvatarRootState(ImageLoadingStatus ImageLoadingStatus);

public record AvatarRootContext(
    ImageLoadingStatus ImageLoadingStatus,
    Action<ImageLoadingStatus> SetImageLoadingStatus,
    AvatarRootState State);
