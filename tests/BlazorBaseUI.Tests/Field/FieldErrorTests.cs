using BlazorBaseUI.Field;
using BlazorBaseUI.Tests.Contracts.Field;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlazorBaseUI.Tests.Field;

public class FieldErrorTests : BunitContext, IFieldErrorContract
{
    public FieldErrorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private RenderFragment CreateFieldWithError(
        bool? invalid = null,
        bool? match = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);

            if (invalid.HasValue)
                builder.AddAttribute(1, "Invalid", invalid.Value);

            builder.AddAttribute(2, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldControl<string>>(0);
                fieldBuilder.AddAttribute(1, "data-testid", "field-control");
                fieldBuilder.CloseComponent();

                fieldBuilder.OpenComponent<FieldError>(10);
                if (match.HasValue)
                    fieldBuilder.AddAttribute(11, "Match", match.Value);
                fieldBuilder.AddAttribute(12, "data-testid", "field-error");
                fieldBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Error message")));
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivWhenInvalid()
    {
        // FieldError.ShouldRenderError checks validityData.State.Valid, not FieldRoot's Invalid param.
        // Use Match=true to force render, and Invalid=true so the field state is invalid.
        var cut = Render(CreateFieldWithError(invalid: true, match: true));
        var error = cut.Find("[data-testid='field-error']");
        error.ShouldNotBeNull();
        error.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaDescribedByOnControlAutomatically()
    {
        // Use Match=true so the error renders and registers its message id
        var cut = Render(CreateFieldWithError(invalid: true, match: true));
        var control = cut.Find("input[data-testid='field-control']");
        var error = cut.Find("[data-testid='field-error']");

        var ariaDescribedBy = control.GetAttribute("aria-describedby");
        var errorId = error.GetAttribute("id");

        ariaDescribedBy.ShouldNotBeNullOrEmpty();
        errorId.ShouldNotBeNullOrEmpty();
        ariaDescribedBy.ShouldContain(errorId);
        return Task.CompletedTask;
    }

    [Fact]
    public Task MatchTrueAlwaysRendersErrorMessage()
    {
        // Match=true should render even when not invalid
        var cut = Render(CreateFieldWithError(match: true));
        var error = cut.Find("[data-testid='field-error']");
        error.ShouldNotBeNull();
        error.TextContent.ShouldContain("Error message");
        return Task.CompletedTask;
    }
}
