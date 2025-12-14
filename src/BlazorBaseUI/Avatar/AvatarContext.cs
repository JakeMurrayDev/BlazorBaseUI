namespace BlazorBaseUI.Avatar;

public record AvatarRootContext(
    ImageLoadingStatus ImageLoadingStatus,
    Action<ImageLoadingStatus> SetImageLoadingStatus);
