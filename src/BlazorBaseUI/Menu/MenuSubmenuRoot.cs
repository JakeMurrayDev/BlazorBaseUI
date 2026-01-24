using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Menu;

public sealed class MenuSubmenuRoot : ComponentBase
{
    private MenuSubmenuRootContext? submenuContext;

    [CascadingParameter]
    private MenuRootContext? ParentMenuContext { get; set; }

    [Parameter]
    public bool? Open { get; set; }

    [Parameter]
    public bool DefaultOpen { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool CloseParentOnEsc { get; set; }

    [Parameter]
    public bool LoopFocus { get; set; } = true;

    [Parameter]
    public MenuRootActions? ActionsRef { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public EventCallback<MenuOpenChangeEventArgs> OnOpenChange { get; set; }

    [Parameter]
    public EventCallback<bool> OnOpenChangeComplete { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        submenuContext = new MenuSubmenuRootContext(ParentMenuContext);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<MenuSubmenuRootContext>>(0);
        builder.AddComponentParameter(1, "Value", submenuContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderMenuRoot);
        builder.CloseComponent();
    }

    private void RenderMenuRoot(RenderTreeBuilder builder)
    {
        builder.OpenComponent<MenuRoot>(0);
        builder.AddComponentParameter(1, "Open", Open);
        builder.AddComponentParameter(2, "DefaultOpen", DefaultOpen);
        builder.AddComponentParameter(3, "Disabled", Disabled);
        builder.AddComponentParameter(4, "CloseParentOnEsc", CloseParentOnEsc);
        builder.AddComponentParameter(5, "LoopFocus", LoopFocus);
        builder.AddComponentParameter(6, "ActionsRef", ActionsRef);
        builder.AddComponentParameter(7, "OpenChanged", OpenChanged);
        builder.AddComponentParameter(8, "OnOpenChange", OnOpenChange);
        builder.AddComponentParameter(9, "OnOpenChangeComplete", OnOpenChangeComplete);
        builder.AddComponentParameter(10, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}
