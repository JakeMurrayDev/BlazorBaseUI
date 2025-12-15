namespace BlazorBaseUI.Avatar;

public record AvatarContext(
    ImageLoadingStatus ImageLoadingStatus,
    Action<ImageLoadingStatus> SetImageLoadingStatus);
