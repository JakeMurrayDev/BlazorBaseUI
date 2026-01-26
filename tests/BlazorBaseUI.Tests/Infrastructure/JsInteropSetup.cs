using Bunit;

namespace BlazorBaseUI.Tests.Infrastructure;

public static class JsInteropSetup
{
    private const string AvatarImageModule = "./_content/BlazorBaseUI/blazor-baseui-avatar-image.js";

    public static void SetupLoadedImage(BunitJSInterop jsInterop)
    {
        jsInterop.SetupModule(AvatarImageModule)
            .Setup<string>("loadImage", _ => true)
            .SetResult("loaded");
    }

    public static void SetupErrorImage(BunitJSInterop jsInterop)
    {
        jsInterop.SetupModule(AvatarImageModule)
            .Setup<string>("loadImage", _ => true)
            .SetResult("error");
    }

    public static void SetupIdleImage(BunitJSInterop jsInterop)
    {
        jsInterop.SetupModule(AvatarImageModule)
            .Setup<string>("loadImage", _ => true)
            .SetResult("idle");
    }

    private const string CollapsiblePanelModule = "./_content/BlazorBaseUI/blazor-baseui-collapsible.js";

    public static void SetupCollapsiblePanel(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(CollapsiblePanelModule);
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("open", _ => true);
        module.SetupVoid("close", _ => true);
        module.SetupVoid("updateDimensions", _ => true);
        module.SetupVoid("dispose", _ => true);
    }

    private const string MenuModule = "./_content/BlazorBaseUI/blazor-baseui-menu.js";

    public static void SetupMenuModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(MenuModule);
        module.SetupVoid("initializeRoot", _ => true);
        module.SetupVoid("disposeRoot", _ => true);
        module.SetupVoid("setRootOpen", _ => true);
        module.SetupVoid("setTriggerElement", _ => true);
        module.SetupVoid("setPopupElement", _ => true);
        module.SetupVoid("setActiveIndex", _ => true);
        module.SetupVoid("initializeHoverInteraction", _ => true);
        module.SetupVoid("disposeHoverInteraction", _ => true);
        module.SetupVoid("updateHoverInteractionFloatingElement", _ => true);
        module.SetupVoid("setHoverInteractionOpen", _ => true);
        module.Setup<string?>("initializePositioner", _ => true).SetResult("positioner-id");
        module.SetupVoid("updatePosition", _ => true);
        module.SetupVoid("disposePositioner", _ => true);
    }

    private const string MenuBarModule = "./_content/BlazorBaseUI/blazor-baseui-menubar.js";

    public static void SetupMenuBarModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(MenuBarModule);
        module.SetupVoid("initMenuBar", _ => true);
        module.SetupVoid("updateMenuBar", _ => true);
        module.SetupVoid("updateScrollLock", _ => true);
        module.SetupVoid("registerItem", _ => true);
        module.SetupVoid("unregisterItem", _ => true);
        module.SetupVoid("disposeMenuBar", _ => true);
    }
}
