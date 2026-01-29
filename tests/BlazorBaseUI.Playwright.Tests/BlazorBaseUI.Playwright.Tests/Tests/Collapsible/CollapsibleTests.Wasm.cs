using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Collapsible;

public class CollapsibleTestsWasm : CollapsibleTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public CollapsibleTestsWasm(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
