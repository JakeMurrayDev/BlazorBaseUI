namespace BlazorBaseUI.Tests.Avatar;

public class AvatarImageTests : BunitContext, IAvatarImageContract
{
    public AvatarImageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static RenderFragment CreateAvatarRootWrapper(RenderFragment childContent)
    {
        return builder =>
        {
            builder.OpenComponent<AvatarRoot>(0);
            builder.AddAttribute(1, "ChildContent", childContent);
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersWhenLoaded()
    {
        JsInteropSetup.SetupLoadedImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" }
            });
            builder.CloseComponent();
        }));

        JSInterop.VerifyInvoke("import");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderWhenNotLoaded()
    {
        JsInteropSetup.SetupErrorImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" },
                { "data-testid", "avatar-image" }
            });
            builder.CloseComponent();
        }));

        var images = cut.FindAll("img");
        images.Count.ShouldBe(0);
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesStatusOnLoad()
    {
        JsInteropSetup.SetupLoadedImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/test-image.jpg" }
            });
            builder.CloseComponent();
        }));

        JSInterop.VerifyInvoke("import");
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesStatusOnError()
    {
        JsInteropSetup.SetupErrorImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/broken-image.jpg" }
            });
            builder.CloseComponent();
        }));

        var images = cut.FindAll("img");
        images.Count.ShouldBe(0);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task InvokesOnLoadingStatusChange()
    {
        ImageLoadingStatus? capturedStatus = null;

        JsInteropSetup.SetupLoadedImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" }
            });
            builder.AddAttribute(2, "OnLoadingStatusChange", EventCallback.Factory.Create<ImageLoadingStatus>(
                this,
                status => capturedStatus = status
            ));
            builder.CloseComponent();
        }));

        await Task.Delay(100);
        cut.Render();
    }

    [Fact]
    public Task ForwardsAttributes()
    {
        JsInteropSetup.SetupLoadedImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" },
                { "alt", "Test Image" },
                { "data-custom", "custom-value" }
            });
            builder.CloseComponent();
        }));

        cut.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        JsInteropSetup.SetupLoadedImage(JSInterop);

        Should.Throw<InvalidOperationException>(() =>
        {
            Render<AvatarImage>(parameters => parameters
                .Add(p => p.AdditionalAttributes, new Dictionary<string, object>
                {
                    { "src", "https://example.com/image.jpg" }
                })
            );
        });
        return Task.CompletedTask;
    }

    [Fact]
    public void Render_WithAsDifferentElement()
    {
        JsInteropSetup.SetupErrorImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "As", "picture");
            builder.AddAttribute(2, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" }
            });
            builder.CloseComponent();
        }));

        cut.ShouldNotBeNull();
    }

    [Fact]
    public void ClassValue_AcceptsStateFunction()
    {
        JsInteropSetup.SetupErrorImage(JSInterop);

        var cut = Render(CreateAvatarRootWrapper(builder =>
        {
            builder.OpenComponent<AvatarImage>(0);
            builder.AddAttribute(1, "ClassValue", (Func<AvatarRootState, string?>)(state =>
                state.ImageLoadingStatus == ImageLoadingStatus.Loaded ? "loaded-class" : "not-loaded-class"
            ));
            builder.AddAttribute(2, "AdditionalAttributes", new Dictionary<string, object>
            {
                { "src", "https://example.com/image.jpg" }
            });
            builder.CloseComponent();
        }));

        cut.ShouldNotBeNull();
    }
}
