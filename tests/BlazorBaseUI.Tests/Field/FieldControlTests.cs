using BlazorBaseUI.Field;
using BlazorBaseUI.Tests.Contracts.Field;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlazorBaseUI.Tests.Field;

public class FieldControlTests : BunitContext, IFieldControlContract
{
    public FieldControlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private RenderFragment CreateFieldControl(
        RenderFragment<RenderProps<FieldRootState>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldControl<string>>(0);
                fieldBuilder.AddAttribute(1, "data-testid", "field-control");
                if (render is not null)
                    fieldBuilder.AddAttribute(2, "Render", render);
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsInputByDefault()
    {
        var cut = Render(CreateFieldControl());
        var input = cut.Find("input[data-testid='field-control']");
        input.ShouldNotBeNull();
        input.TagName.ShouldBe("INPUT");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateFieldControl(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "textarea");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.CloseElement();
            }
        ));

        var textarea = cut.Find("textarea");
        textarea.ShouldNotBeNull();
        textarea.HasAttribute("id").ShouldBeTrue();
        return Task.CompletedTask;
    }
}
