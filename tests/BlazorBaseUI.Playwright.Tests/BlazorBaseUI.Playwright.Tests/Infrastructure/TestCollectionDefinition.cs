using BlazorBaseUI.Playwright.Tests.Fixtures;

namespace BlazorBaseUI.Playwright.Tests.Infrastructure;

[CollectionDefinition("BlazorTests", DisableParallelization = true)]
public class BlazorTestCollection :
    ICollectionFixture<BlazorTestFixture>,
    ICollectionFixture<PlaywrightFixture>
{
}
