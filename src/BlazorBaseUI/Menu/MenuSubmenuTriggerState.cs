namespace BlazorBaseUI.Menu;

public readonly record struct MenuSubmenuTriggerState(
    bool Disabled,
    bool Highlighted,
    bool Open);
