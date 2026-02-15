namespace BlazorBaseUI.Tooltip;

/// <summary>
/// Provides shared delay configuration for child components of a <see cref="TooltipProvider"/>.
/// </summary>
internal sealed class TooltipProviderContext
{
    /// <summary>
    /// Gets or sets the delay in milliseconds before the tooltip opens.
    /// </summary>
    public int? Delay { get; set; }

    /// <summary>
    /// Gets or sets the delay in milliseconds before the tooltip closes.
    /// </summary>
    public int? CloseDelay { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds for the instant phase.
    /// </summary>
    public int Timeout { get; set; }

    /// <summary>
    /// Gets or sets the delegate that returns when the last tooltip was closed.
    /// </summary>
    public Func<DateTime?> GetLastClosedTime { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that records the current time as the last close time.
    /// </summary>
    public Action SetLastClosedTime { get; set; } = null!;

    /// <summary>
    /// Determines whether the provider is in the instant phase where tooltips open without delay.
    /// </summary>
    /// <returns><see langword="true"/> if within the instant phase timeout; otherwise, <see langword="false"/>.</returns>
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
