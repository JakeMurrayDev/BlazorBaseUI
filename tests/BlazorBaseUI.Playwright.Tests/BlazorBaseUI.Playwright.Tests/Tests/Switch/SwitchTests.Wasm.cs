using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Switch;

public class SwitchTestsWasm : SwitchTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public SwitchTestsWasm(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
