using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.FieldError;

public class FieldErrorTestsServer : FieldErrorTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public FieldErrorTestsServer(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }
}
