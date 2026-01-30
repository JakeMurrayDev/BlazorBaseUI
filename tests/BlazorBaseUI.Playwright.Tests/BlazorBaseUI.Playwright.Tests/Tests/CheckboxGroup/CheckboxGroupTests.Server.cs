using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.CheckboxGroup;

public class CheckboxGroupTestsServer : CheckboxGroupTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public CheckboxGroupTestsServer(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
