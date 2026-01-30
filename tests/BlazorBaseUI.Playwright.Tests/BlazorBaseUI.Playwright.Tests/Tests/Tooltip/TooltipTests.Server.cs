using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Tooltip;

[Collection("BlazorTests")]
public class TooltipTestsServer : TooltipTestsBase
{
    public TooltipTestsServer(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }

    protected override TestRenderMode RenderMode => TestRenderMode.Server;
}
