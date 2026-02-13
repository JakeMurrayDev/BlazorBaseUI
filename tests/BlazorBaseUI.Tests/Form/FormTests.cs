using BlazorBaseUI.Field;
using BlazorBaseUI.Form;
using BlazorBaseUI.Tests.Contracts.Form;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlazorBaseUI.Tests.Form;

public class FormTests : BunitContext, IFormContract
{
    public FormTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private sealed class TestModel
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    private RenderFragment CreateForm(
        Dictionary<string, string[]>? errors = null,
        RenderFragment? customContent = null,
        RenderFragment<RenderProps<FormState>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<BlazorBaseUI.Form.Form>(0);
            builder.AddAttribute(1, "Model", new TestModel());

            if (errors is not null)
                builder.AddAttribute(2, "Errors", errors);

            if (render is not null)
                builder.AddAttribute(3, "Render", render);

            builder.AddAttribute(4, "ChildContent", (RenderFragment<Microsoft.AspNetCore.Components.Forms.EditContext>)(context =>
                customContent ?? ((RenderFragment)(b => b.AddContent(0, "Form content")))));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateFormWithField(
        string fieldName,
        Dictionary<string, string[]>? errors = null)
    {
        return builder =>
        {
            builder.OpenComponent<BlazorBaseUI.Form.Form>(0);
            builder.AddAttribute(1, "Model", new TestModel());

            if (errors is not null)
                builder.AddAttribute(2, "Errors", errors);

            builder.AddAttribute(3, "ChildContent", (RenderFragment<Microsoft.AspNetCore.Components.Forms.EditContext>)(context =>
                (RenderFragment)(innerBuilder =>
                {
                    innerBuilder.OpenComponent<FieldRoot>(0);
                    innerBuilder.AddAttribute(1, "Name", fieldName);
                    innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(fieldBuilder =>
                    {
                        fieldBuilder.OpenComponent<FieldControl<string>>(0);
                        fieldBuilder.AddAttribute(1, "data-testid", "field-control");
                        fieldBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                })));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsFormByDefault()
    {
        var cut = Render(CreateForm());
        var form = cut.Find("form");
        form.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateForm(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var section = cut.Find("section");
        section.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsNoValidateByDefault()
    {
        var cut = Render(CreateForm());
        var form = cut.Find("form");
        form.HasAttribute("novalidate").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NoValidateIsAlwaysPresent()
    {
        // Blazor's Form always sets novalidate=true (React's Form has a noValidate prop
        // that can be set to false, but the Blazor port does not expose this parameter)
        var cut = Render(CreateForm());
        var form = cut.Find("form");
        form.HasAttribute("novalidate").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task MarksControlInvalidWhenErrorsProvided()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["name"] = ["Name is required"]
        };

        var cut = Render(CreateFormWithField("name", errors));

        var control = cut.Find("[data-testid='field-control']");
        control.HasAttribute("aria-invalid").ShouldBeTrue();
        control.GetAttribute("aria-invalid").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotMarkControlInvalidWhenNoErrors()
    {
        var cut = Render(CreateFormWithField("name"));

        var control = cut.Find("[data-testid='field-control']");
        control.HasAttribute("aria-invalid").ShouldBeFalse();

        return Task.CompletedTask;
    }
}
