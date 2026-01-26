namespace BlazorBaseUI.MenuBar;

public readonly record struct MenuBarRootState(
    bool Disabled,
    bool HasSubmenuOpen,
    bool Modal,
    Orientation Orientation);
