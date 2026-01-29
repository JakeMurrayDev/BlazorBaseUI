using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests;

[Collection("BlazorTests")]
public class DiagnosticTests : IAsyncLifetime
{
    private readonly BlazorTestFixture blazorFixture;
    private readonly PlaywrightFixture playwrightFixture;
    private IBrowserContext? context;
    private IPage page = null!;

    public DiagnosticTests(BlazorTestFixture blazorFixture, PlaywrightFixture playwrightFixture)
    {
        this.blazorFixture = blazorFixture;
        this.playwrightFixture = playwrightFixture;
    }

    public async ValueTask InitializeAsync()
    {
        Console.WriteLine($"[Diagnostic] InitializeAsync - ServerAddress: '{blazorFixture.ServerAddress}'");
        context = await playwrightFixture.Browser.NewContextAsync();
        page = await context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (page is not null)
        {
            await page.CloseAsync();
        }

        if (context is not null)
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task ServerIsRunning()
    {
        Console.WriteLine($"[Diagnostic] ServerIsRunning test starting");
        Console.WriteLine($"[Diagnostic] ServerAddress: '{blazorFixture.ServerAddress}'");

        Assert.False(string.IsNullOrEmpty(blazorFixture.ServerAddress), "ServerAddress should not be empty");

        var url = $"{blazorFixture.ServerAddress}/tests/collapsible/server";
        Console.WriteLine($"[Diagnostic] Navigating to: {url}");

        var response = await page.GotoAsync(url);

        Console.WriteLine($"[Diagnostic] Response status: {response?.Status}");
        Assert.NotNull(response);
        Assert.True(response.Ok, $"Expected OK response, got {response.Status}");

        var title = await page.TitleAsync();
        Console.WriteLine($"[Diagnostic] Page title: {title}");

        var content = await page.ContentAsync();
        Console.WriteLine($"[Diagnostic] Page content length: {content.Length}");
        Console.WriteLine($"[Diagnostic] Page content (first 500): {content[..Math.Min(500, content.Length)]}");
    }

    [Fact]
    public async Task CanLoadTestPageMultipleTimes()
    {
        var url = $"{blazorFixture.ServerAddress}/tests/collapsible/server";

        for (int i = 0; i < 3; i++)
        {
            Console.WriteLine($"[Diagnostic] Iteration {i + 1}");

            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load
            });

            Assert.NotNull(response);
            Assert.True(response.Ok, $"Iteration {i + 1}: Expected OK response, got {response.Status}");

            // Wait for test container
            var testContainer = page.GetByTestId("test-container");
            await testContainer.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = 10000
            });

            Console.WriteLine($"[Diagnostic] Iteration {i + 1}: Test container found");

            // Check trigger is visible
            var trigger = page.GetByTestId("collapsible-trigger");
            var isVisible = await trigger.IsVisibleAsync();
            Console.WriteLine($"[Diagnostic] Iteration {i + 1}: Trigger visible: {isVisible}");

            Assert.True(isVisible, $"Iteration {i + 1}: Trigger should be visible");
        }
    }
}
