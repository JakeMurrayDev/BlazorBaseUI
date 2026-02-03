using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Form;

public class FormTestsWasm : FormTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public FormTestsWasm(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }
}
