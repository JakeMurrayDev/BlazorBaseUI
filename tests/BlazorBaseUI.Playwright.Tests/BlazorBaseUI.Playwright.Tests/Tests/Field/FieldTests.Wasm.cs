using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Field;

public class FieldTestsWasm : FieldTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public FieldTestsWasm(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }
}
