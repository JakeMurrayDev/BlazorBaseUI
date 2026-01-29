using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Accordion;

public class AccordionTestsServer : AccordionTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public AccordionTestsServer(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
