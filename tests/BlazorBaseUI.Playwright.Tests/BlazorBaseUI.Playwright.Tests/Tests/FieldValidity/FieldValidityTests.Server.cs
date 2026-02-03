using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.FieldValidity;

public class FieldValidityTestsServer : FieldValidityTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public FieldValidityTestsServer(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }
}
