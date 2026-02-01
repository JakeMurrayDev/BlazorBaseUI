using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.CheckboxGroup;

public class CheckboxGroupTestsWasm : CheckboxGroupTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public CheckboxGroupTestsWasm(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
