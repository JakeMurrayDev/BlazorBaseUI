using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Checkbox;

public class CheckboxTestsServer : CheckboxTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public CheckboxTestsServer(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
