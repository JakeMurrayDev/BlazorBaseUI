using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Tooltip;

[Collection("BlazorTests")]
public class TooltipTestsWasm : TooltipTestsBase
{
    public TooltipTestsWasm(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }

    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;
}
