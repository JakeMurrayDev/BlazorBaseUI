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

    private const string AccordionTriggerModule = "./_content/BlazorBaseUI/blazor-baseui-accordion-trigger.js";

    public static void SetupAccordionTrigger(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(AccordionTriggerModule);
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("dispose", _ => true);
    }

    public static void SetupAccordionModules(BunitJSInterop jsInterop)
    {
        SetupAccordionTrigger(jsInterop);
        SetupCollapsiblePanel(jsInterop);
    }

    private const string SliderModule = "./_content/BlazorBaseUI/blazor-baseui-slider.js";

    public static void SetupSliderModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(SliderModule);
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("dispose", _ => true);
        module.SetupVoid("startDrag", _ => true);
        module.SetupVoid("stopDrag", _ => true);
        module.SetupVoid("setPointerCapture", _ => true);
        module.SetupVoid("focusThumbInput", _ => true);
        module.Setup<object?>("getThumbRect", _ => true).SetResult(null);
    }

    private const string SwitchModule = "./_content/BlazorBaseUI/blazor-baseui-switch.js";

    public static void SetupSwitchModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(SwitchModule);
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("dispose", _ => true);
        module.SetupVoid("updateState", _ => true);
        module.SetupVoid("setInputChecked", _ => true);
        module.SetupVoid("focus", _ => true);
    }

    private const string CheckboxModule = "./_content/BlazorBaseUI/blazor-baseui-checkbox.js";

    public static void SetupCheckboxModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(CheckboxModule);
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("dispose", _ => true);
        module.SetupVoid("updateState", _ => true);
        module.SetupVoid("setInputChecked", _ => true);
        module.SetupVoid("focus", _ => true);
    }

    private const string PopoverModule = "./_content/BlazorBaseUI/blazor-baseui-popover.js";

    public static void SetupPopoverModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(PopoverModule);
        module.SetupVoid("initializeRoot", _ => true);
        module.SetupVoid("disposeRoot", _ => true);
        module.SetupVoid("setRootOpen", _ => true);
        module.SetupVoid("setTriggerElement", _ => true);
        module.SetupVoid("setPopupElement", _ => true);
        module.SetupVoid("initializeHoverInteraction", _ => true);
        module.SetupVoid("disposeHoverInteraction", _ => true);
        module.SetupVoid("updateHoverInteractionFloatingElement", _ => true);
        module.SetupVoid("setHoverInteractionOpen", _ => true);
        module.Setup<string?>("initializePositioner", _ => true).SetResult("positioner-id");
        module.SetupVoid("updatePosition", _ => true);
        module.SetupVoid("disposePositioner", _ => true);
        module.SetupVoid("initializePopup", _ => true);
        module.SetupVoid("disposePopup", _ => true);
        module.SetupVoid("focusElement", _ => true);
    }

    private const string TooltipModule = "./_content/BlazorBaseUI/blazor-baseui-tooltip.js";

    public static void SetupTooltipModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(TooltipModule);
        module.SetupVoid("initializeRoot", _ => true).SetVoidResult();
        module.SetupVoid("disposeRoot", _ => true).SetVoidResult();
        module.SetupVoid("setRootOpen", _ => true).SetVoidResult();
        module.SetupVoid("setTriggerElement", _ => true).SetVoidResult();
        module.SetupVoid("setPopupElement", _ => true).SetVoidResult();
        module.SetupVoid("initializeHoverInteraction", _ => true).SetVoidResult();
        module.SetupVoid("disposeHoverInteraction", _ => true).SetVoidResult();
        module.Setup<string?>("initializePositioner", _ => true).SetResult("positioner-id");
        module.SetupVoid("updatePosition", _ => true).SetVoidResult();
        module.SetupVoid("disposePositioner", _ => true).SetVoidResult();
        module.SetupVoid("initializePopup", _ => true).SetVoidResult();
        module.SetupVoid("disposePopup", _ => true).SetVoidResult();
    }

    private const string FieldModule = "./_content/BlazorBaseUI/blazor-baseui-field.js";

    public static void SetupFieldModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(FieldModule);
        module.Setup<object?>("getValidityState", _ => true).SetResult(null);
        module.Setup<string>("getValidationMessage", _ => true).SetResult("");
        module.SetupVoid("setCustomValidity", _ => true);
        module.Setup<bool>("checkValidity", _ => true).SetResult(true);
        module.Setup<bool>("reportValidity", _ => true).SetResult(true);
        module.SetupVoid("focusElement", _ => true);
        module.Setup<object?>("getValue", _ => true).SetResult(null);
        module.SetupVoid("setValue", _ => true);
        module.Setup<string?>("observeValidity", _ => true).SetResult(null);
        module.SetupVoid("disposeObserver", _ => true);
        module.SetupVoid("dispose", _ => true);
    }

    private const string LabelModule = "./_content/BlazorBaseUI/blazor-baseui-label.js";

    public static void SetupLabelModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(LabelModule);
        module.SetupVoid("addLabelMouseDownListener", _ => true);
        module.SetupVoid("removeLabelMouseDownListener", _ => true);
        module.SetupVoid("focusControlById", _ => true);
    }

    private const string DialogModule = "./_content/BlazorBaseUI/blazor-baseui-dialog.js";

    public static void SetupDialogModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(DialogModule);
        module.SetupVoid("initializeRoot", _ => true);
        module.SetupVoid("disposeRoot", _ => true);
        module.SetupVoid("setRootOpen", _ => true);
        module.SetupVoid("setTriggerElement", _ => true);
        module.SetupVoid("setPopupElement", _ => true);
        module.SetupVoid("initializePopup", _ => true);
        module.SetupVoid("setInitialFocusElement", _ => true);
        module.SetupVoid("disposePopup", _ => true);
    }

    private const string ButtonModule = "./_content/BlazorBaseUI/blazor-baseui-button.js";

    public static void SetupButtonModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(ButtonModule);
        module.SetupVoid("sync", _ => true);
    }

    private const string ToolbarModule = "./_content/BlazorBaseUI/blazor-baseui-toolbar.js";

    public static void SetupToolbarModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(ToolbarModule);
        module.SetupVoid("initToolbar", _ => true);
        module.SetupVoid("updateToolbar", _ => true);
        module.SetupVoid("registerItem", _ => true);
        module.SetupVoid("unregisterItem", _ => true);
        module.SetupVoid("disposeToolbar", _ => true);
    }

    private const string RadioModule = "./_content/BlazorBaseUI/blazor-baseui-radio.js";

    public static void SetupRadioModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(RadioModule);
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("dispose", _ => true);
        module.SetupVoid("updateState", _ => true);
        module.SetupVoid("focus", _ => true);
        module.SetupVoid("registerRadio", _ => true);
        module.SetupVoid("unregisterRadio", _ => true);
        module.SetupVoid("navigateToPrevious", _ => true);
        module.SetupVoid("navigateToNext", _ => true);
        module.SetupVoid("initializeGroup", _ => true);
        module.SetupVoid("disposeGroup", _ => true);
        module.Setup<bool>("isBlurWithinGroup", _ => true).SetResult(false);
    }

    private const string ToggleModule = "./_content/BlazorBaseUI/blazor-baseui-toggle.js";

    public static void SetupToggleModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(ToggleModule);
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("updateState", _ => true);
        module.SetupVoid("dispose", _ => true);
        module.SetupVoid("initializeGroup", _ => true);
        module.SetupVoid("updateGroup", _ => true);
        module.SetupVoid("disposeGroup", _ => true);
        module.SetupVoid("registerToggle", _ => true);
        module.SetupVoid("unregisterToggle", _ => true);
        module.SetupVoid("navigateToPrevious", _ => true);
        module.SetupVoid("navigateToNext", _ => true);
        module.SetupVoid("navigateToFirst", _ => true);
        module.SetupVoid("navigateToLast", _ => true);
        module.SetupVoid("initializeGroupItem", _ => true);
        module.SetupVoid("updateGroupItemOrientation", _ => true);
        module.SetupVoid("disposeGroupItem", _ => true);
    }
}
