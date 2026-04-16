using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Select;

/// <summary>
/// Adapts an <see cref="ISelectRootContext"/> to <see cref="IFloatingRootContext"/> for use with
/// <see cref="FloatingFocusManager.FloatingFocusManager"/>.
/// </summary>
internal sealed class SelectFloatingRootContextAdapter : IFloatingRootContext
{
    private readonly ISelectRootContext _context;

    public SelectFloatingRootContextAdapter(ISelectRootContext context) => _context = context;

    /// <inheritdoc />
    public string FloatingId => _context.RootId;

    /// <inheritdoc />
    public bool GetOpen() => _context.GetOpen();

    /// <inheritdoc />
    public ElementReference? GetTriggerElement() => _context.GetTriggerElement();

    /// <inheritdoc />
    public ElementReference? GetPopupElement() => _context.GetPopupElement();

    /// <inheritdoc />
    public void SetPopupElement(ElementReference element) => _context.SetPopupElement(element);

    /// <inheritdoc />
    public Task SetOpenAsync(bool open) => _context.SetOpenAsync(open, SelectOpenChangeReason.FocusOut);

    /// <inheritdoc />
    public InteractionType CloseInteractionType => _context.OpenChangeReason switch
    {
        SelectOpenChangeReason.TriggerPress => InteractionType.Click,
        SelectOpenChangeReason.OutsidePress => InteractionType.Click,
        SelectOpenChangeReason.ItemPress => InteractionType.Click,
        SelectOpenChangeReason.CancelOpen => InteractionType.Click,
        SelectOpenChangeReason.EscapeKey => InteractionType.Keyboard,
        SelectOpenChangeReason.ListNavigation => InteractionType.Keyboard,
        _ => InteractionType.None
    };
}
