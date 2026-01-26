using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Fixtures;

public class PlaywrightFixture : IAsyncLifetime
{
    private bool isInitialized;

    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        if (isInitialized)
        {
            Console.WriteLine("[PlaywrightFixture] Already initialized.");
            return;
        }

        Console.WriteLine("[PlaywrightFixture] Initializing...");

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        var browserType = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSER")?.ToLower() ?? "chromium";
        var headless = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADLESS") != "false";

        var options = new BrowserTypeLaunchOptions { Headless = headless };

        Browser = browserType switch
        {
            "firefox" => await Playwright.Firefox.LaunchAsync(options),
            "webkit" => await Playwright.Webkit.LaunchAsync(options),
            _ => await Playwright.Chromium.LaunchAsync(options)
        };

        isInitialized = true;
        Console.WriteLine($"[PlaywrightFixture] Initialized {browserType} (headless: {headless}). Browser version: {Browser.Version}");
    }

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("[PlaywrightFixture] Disposing...");

        await Browser.DisposeAsync();
        Playwright.Dispose();

        Console.WriteLine("[PlaywrightFixture] Disposed.");
    }
}
