namespace BlazorBaseUI.Tests.Progress;

public class ProgressTrackTests : BunitContext, IProgressTrackContract
{
    public ProgressTrackTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateProgressWithTrack(
        double? value = 50,
        Func<ProgressRootState, string>? trackClassValue = null,
        Func<ProgressRootState, string>? trackStyleValue = null,
        IReadOnlyDictionary<string, object>? trackAttributes = null,
        string? trackAs = null,
        Type? trackRenderAs = null)
    {
        return builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);
            var attrIndex = 1;

            if (value.HasValue)
                builder.AddAttribute(attrIndex++, "Value", value.Value);
            else
                builder.AddAttribute(attrIndex++, "Value", (double?)null);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ProgressTrack>(0);
                var trackAttrIndex = 1;

                if (trackClassValue is not null)
                    innerBuilder.AddAttribute(trackAttrIndex++, "ClassValue", trackClassValue);
                if (trackStyleValue is not null)
                    innerBuilder.AddAttribute(trackAttrIndex++, "StyleValue", trackStyleValue);
                if (trackAs is not null)
                    innerBuilder.AddAttribute(trackAttrIndex++, "As", trackAs);
                if (trackRenderAs is not null)
                    innerBuilder.AddAttribute(trackAttrIndex++, "RenderAs", trackRenderAs);

                var attrs = new Dictionary<string, object>
                {
                    { "data-testid", "track" }
                };
                if (trackAttributes is not null)
                {
                    foreach (var kvp in trackAttributes)
                        attrs[kvp.Key] = kvp.Value;
                }
                innerBuilder.AddAttribute(trackAttrIndex++, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)attrs);

                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateProgressWithTrack());
        var track = cut.Find("[data-testid='track']");
        track.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateProgressWithTrack(trackAs: "section"));
        var track = cut.Find("[data-testid='track']");
        track.TagName.ShouldBe("SECTION");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateProgressWithTrack(
            trackAttributes: new Dictionary<string, object>
            {
                { "aria-label", "track background" }
            }
        ));
        var track = cut.Find("[data-testid='track']");
        track.GetAttribute("aria-label").ShouldBe("track background");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateProgressWithTrack(
            trackClassValue: _ => "track-custom"
        ));
        var track = cut.Find("[data-testid='track']");
        track.GetAttribute("class").ShouldContain("track-custom");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateProgressWithTrack(
            trackStyleValue: _ => "height: 8px"
        ));
        var track = cut.Find("[data-testid='track']");
        track.GetAttribute("style").ShouldContain("height: 8px");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataStatusAttribute()
    {
        var cut = Render(CreateProgressWithTrack(value: 50));
        var track = cut.Find("[data-testid='track']");
        track.HasAttribute("data-progressing").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // RenderAs validation

    [Fact]
    public Task ThrowsWhenRenderAsDoesNotImplementInterface()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            Render(CreateProgressWithTrack(
                trackRenderAs: typeof(NonReferencableComponent)));
        });
        return Task.CompletedTask;
    }

    private sealed class NonReferencableComponent : ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.CloseElement();
        }
    }
}
