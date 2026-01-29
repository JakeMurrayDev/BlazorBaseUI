using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Avatar;

public class AvatarTestsWasm : AvatarTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public AvatarTestsWasm(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
