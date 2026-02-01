using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Checkbox;

public class CheckboxTestsWasm : CheckboxTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public CheckboxTestsWasm(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
