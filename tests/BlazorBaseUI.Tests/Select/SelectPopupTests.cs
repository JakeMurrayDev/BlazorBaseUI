namespace BlazorBaseUI.Tests.Select;

public class SelectPopupTests : BunitContext, ISelectPopupContract
{
    public SelectPopupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
    }

    private RenderFragment CreateSelectWithPopupNoList(bool defaultOpen = true)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectItem<string>>(0);
                        popupBuilder.AddAttribute(1, "Value", "apple");
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateSelectWithPopupAndList(bool defaultOpen = true)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectList>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(listBuilder =>
                        {
                            listBuilder.OpenComponent<SelectItem<string>>(0);
                            listBuilder.AddAttribute(1, "Value", "apple");
                            listBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                            listBuilder.CloseComponent();
                        }));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task HasAriaAttributesWhenNoSelectListPresent()
    {
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true));

        var popup = cut.Find("[role='listbox']");
        popup.ShouldNotBeNull();
        popup.GetAttribute("role").ShouldBe("listbox");
        popup.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task PlacesAriaAttributesOnSelectListIfPresent()
    {
        var cut = Render(CreateSelectWithPopupAndList(defaultOpen: true));

        // The SelectList always renders with role="listbox", tabindex="-1", and a generated id
        // in its own BuildComponentAttributes, regardless of the render cycle.
        // Find the listbox element that has an id (the SelectList), not the popup.
        var listboxElements = cut.FindAll("[role='listbox']");
        var listElement = listboxElements.First(el => el.HasAttribute("id"));
        listElement.GetAttribute("role").ShouldBe("listbox");
        listElement.GetAttribute("tabindex").ShouldBe("-1");
        listElement.GetAttribute("id").ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }
}
