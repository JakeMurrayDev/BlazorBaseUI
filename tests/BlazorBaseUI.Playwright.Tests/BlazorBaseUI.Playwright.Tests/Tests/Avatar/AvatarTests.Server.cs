using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Avatar;

public class AvatarTestsServer : AvatarTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public AvatarTestsServer(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
