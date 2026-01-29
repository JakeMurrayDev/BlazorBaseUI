using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Accordion;

public class AccordionTestsWasm : AccordionTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public AccordionTestsWasm(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
