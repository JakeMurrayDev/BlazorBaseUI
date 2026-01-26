using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Avatar;

public class AvatarTestsWasm : AvatarTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public AvatarTestsWasm(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }

    [Fact(Skip = "Cannot control .NET WASM runtime time from Playwright Clock API. Delay behavior is covered by bUnit tests with FakeTimeProvider.")]
    public override Task DoesNotShowBeforeDelayElapsed()
    {
        // Playwright Clock API only controls JavaScript timers (setTimeout/setInterval).
        // The AvatarFallback component uses Task.Delay with TimeProvider, which runs
        // in the .NET WASM runtime and cannot be controlled by browser clock manipulation.
        // This behavior is properly tested in bUnit tests using FakeTimeProvider.
        return Task.CompletedTask;
    }

    private async Task WaitForWasmHydrationAsync()
    {
        await Page.WaitForFunctionAsync(@"() =>
            window.Blazor?._internal?.navigationManager !== undefined",
            new PageWaitForFunctionOptions { Timeout = 30000 });
    }
}
