using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.FieldValidity;

public class FieldValidityTestsWasm : FieldValidityTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public FieldValidityTestsWasm(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }
}
