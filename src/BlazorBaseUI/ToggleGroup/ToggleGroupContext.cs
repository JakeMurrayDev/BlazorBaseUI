using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.ToggleGroup;

/// <summary>
/// Defines the contract for cascading state shared between a <see cref="ToggleGroup"/>
/// and its child <see cref="Toggle.Toggle"/> components.
/// </summary>
internal interface IToggleGroupContext
{
    /// <summary>
    /// Gets the values of all currently pressed toggle buttons in the group.
    /// </summary>
    IReadOnlyList<string> Value { get; }

    /// <summary>
    /// Gets whether the toggle group should ignore user interaction.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    /// Gets the orientation of the toggle group.
    /// </summary>
    Orientation Orientation { get; }

    /// <summary>
    /// Gets whether keyboard focus loops back to the first item when the end of the list is reached.
    /// </summary>
    bool LoopFocus { get; }

    /// <summary>
    /// Gets the associated <see cref="ElementReference"/> of the group element.
    /// </summary>
    ElementReference? GroupElement { get; }

    /// <summary>
    /// Updates the pressed state of a toggle within the group.
    /// </summary>
    /// <param name="toggleValue">The value identifier of the toggle to update.</param>
    /// <param name="nextPressed">The new pressed state for the toggle.</param>
    Task SetGroupValueAsync(string toggleValue, bool nextPressed);
}

/// <summary>
/// Provides cascading state shared between a <see cref="ToggleGroup"/>
/// and its child <see cref="Toggle.Toggle"/> components.
/// </summary>
internal sealed class ToggleGroupContext : IToggleGroupContext
{
    /// <summary>
    /// Gets or sets whether the toggle group should ignore user interaction.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the orientation of the toggle group.
    /// </summary>
    public Orientation Orientation { get; set; }

    /// <summary>
    /// Gets or sets whether keyboard focus loops back to the first item when the end of the list is reached.
    /// </summary>
    public bool LoopFocus { get; set; }

    /// <summary>
    /// Gets or sets a delegate that returns the values of all currently pressed toggle buttons.
    /// </summary>
    public Func<IReadOnlyList<string>> GetValueFunc { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that updates the pressed state of a toggle within the group.
    /// </summary>
    public Func<string, bool, Task> SetGroupValueFunc { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the associated <see cref="ElementReference"/> of the group element.
    /// </summary>
    public Func<ElementReference?> GetGroupElementFunc { get; set; } = null!;

    /// <inheritdoc />
    public IReadOnlyList<string> Value => GetValueFunc();

    /// <inheritdoc />
    public ElementReference? GroupElement => GetGroupElementFunc();

    /// <inheritdoc />
    public Task SetGroupValueAsync(string toggleValue, bool nextPressed) =>
        SetGroupValueFunc(toggleValue, nextPressed);
}
