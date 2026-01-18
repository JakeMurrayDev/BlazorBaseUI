namespace BlazorBaseUI.Tooltip;

internal sealed record TooltipProviderContext(
    int? Delay,
    int? CloseDelay,
    int Timeout,
    Func<DateTime?> GetLastClosedTime,
    Action SetLastClosedTime)
{
    public bool IsInInstantPhase()
    {
        var lastClosed = GetLastClosedTime();
        if (!lastClosed.HasValue)
        {
            return false;
        }

        var elapsed = (DateTime.UtcNow - lastClosed.Value).TotalMilliseconds;
        return elapsed < Timeout;
    }
}
