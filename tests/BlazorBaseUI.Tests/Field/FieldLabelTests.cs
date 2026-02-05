using BlazorBaseUI.Field;
using BlazorBaseUI.Tests.Contracts.Field;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlazorBaseUI.Tests.Field;

public class FieldLabelTests : BunitContext, IFieldLabelContract
{
    public FieldLabelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private RenderFragment CreateFieldWithLabel()
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldLabel>(0);
                fieldBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Field Label")));
                fieldBuilder.CloseComponent();

                fieldBuilder.OpenComponent<FieldControl<string>>(10);
                fieldBuilder.AddAttribute(11, "data-testid", "field-control");
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsLabelByDefault()
    {
        var cut = Render(CreateFieldWithLabel());
        var label = cut.Find("label");
        label.ShouldNotBeNull();
        label.TagName.ShouldBe("LABEL");
        label.TextContent.ShouldContain("Field Label");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsHtmlForReferencingControlAutomatically()
    {
        var cut = Render(CreateFieldWithLabel());
        var label = cut.Find("label");
        var control = cut.Find("input[data-testid='field-control']");

        var forValue = label.GetAttribute("for");
        var controlId = control.GetAttribute("id");

        forValue.ShouldNotBeNullOrEmpty();
        controlId.ShouldNotBeNullOrEmpty();
        forValue.ShouldBe(controlId);
        return Task.CompletedTask;
    }
}
