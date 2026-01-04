using System.ComponentModel;

namespace BlazorBaseUI.Progress;

internal static class Extensions
{
    extension(ProgressStatus status)
    {
        public string ToDataAttributeString() =>
            status switch
            {
                ProgressStatus.Indeterminate => "indeterminate",
                ProgressStatus.Progressing => "progressing",
                ProgressStatus.Complete => "complete",
                _ => throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(ProgressStatus))
            };
    }
}
