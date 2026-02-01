using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Popover;

[Collection("BlazorTests")]
public class PopoverTestsWasm : PopoverTestsBase
{
    public PopoverTestsWasm(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }

    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;
}
