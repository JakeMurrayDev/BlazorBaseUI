using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.ToggleGroup;

public interface IToggleGroupContext
{
    IReadOnlyList<string> Value { get; }
    bool Disabled { get; }
    Orientation Orientation { get; }
    bool LoopFocus { get; }
    ElementReference? GroupElement { get; }
    Task SetGroupValueAsync(string toggleValue, bool nextPressed);
}

public sealed class ToggleGroupContext : IToggleGroupContext
{
    private readonly Func<IReadOnlyList<string>> getValue;
    private readonly Func<string, bool, Task> setGroupValue;
    private readonly Func<ElementReference?> getGroupElement;

    public ToggleGroupContext(
        bool disabled,
        Orientation orientation,
        bool loopFocus,
        Func<IReadOnlyList<string>> getValue,
        Func<string, bool, Task> setGroupValue,
        Func<ElementReference?> getGroupElement)
    {
        Disabled = disabled;
        Orientation = orientation;
        LoopFocus = loopFocus;
        this.getValue = getValue;
        this.setGroupValue = setGroupValue;
        this.getGroupElement = getGroupElement;
    }

    public bool Disabled { get; set; }
    public Orientation Orientation { get; set; }
    public bool LoopFocus { get; set; }

    public IReadOnlyList<string> Value => getValue();
    public ElementReference? GroupElement => getGroupElement();

    public async Task SetGroupValueAsync(string toggleValue, bool nextPressed)
    {
        await setGroupValue(toggleValue, nextPressed);
    }
}
