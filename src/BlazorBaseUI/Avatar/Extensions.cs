using System.ComponentModel;

namespace BlazorBaseUI.Avatar;

internal static class Extensions
{
    extension(ImageLoadingStatus status)
    {
        public string ToDataAttributeString() =>
            status switch
            {
                ImageLoadingStatus.Idle => "idle",
                ImageLoadingStatus.Loading => "loading",
                ImageLoadingStatus.Loaded => "loaded",
                ImageLoadingStatus.Error => "error",
                _ => throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(ImageLoadingStatus))
            };
    }
}
