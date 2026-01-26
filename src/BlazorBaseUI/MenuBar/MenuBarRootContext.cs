using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.MenuBar;

internal sealed record MenuBarRootContext(
    bool Disabled,
    bool HasSubmenuOpen,
    bool Modal,
    Orientation Orientation,
    Action<ElementReference> RegisterItem,
    Action<ElementReference> UnregisterItem,
    Action<bool> SetHasSubmenuOpen,
    Func<bool> GetHasSubmenuOpen,
    Func<ElementReference?> GetElement);
