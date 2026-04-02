# Project-Wide Consolidation Design Spec

## Problem

The recent shared helper commits (`c024144`, `1fad898`, `8353887`, `a867ab0`) created 4 shared modules but none are wired up to consumers yet. Beyond that, significant duplication remains across both C# and JS — the Handle class alone is ~300 lines duplicated 5 times.

## Scope

Full consolidation: C# shared abstractions, JS shared module wiring, JS pattern extraction, and an AGENTS.md rule for JS interop file architecture.

## Out of Scope

- PortalContext classes — per-component (matching React source)
- BackdropState records — per-component (matching React source)
- PopupState records — per-component (component-specific fields)
- OpenChangeReason enums — per-component (matching React source's typed unions from shared atoms)
- ModalMode enums — per-component (differing variants)

---

## C# Consolidation

### C1. `ComponentHandleBase<TPayload, TReason>`

**Location:** `src/BlazorBaseUI/ComponentHandleBase.cs`

Abstract base class consolidating the ~300-line Handle pattern duplicated across Dialog, Popover, Menu, Tooltip, and PreviewCard.

**Base class contains:**
- `Dictionary<string, TriggerData>` registered triggers
- `List<IComponentHandleSubscriber<TReason>>` subscribers
- Core methods: `RegisterTrigger`, `UnregisterTrigger`, `UpdateTriggerElement`, `UpdateTriggerPayload`, `GetTriggerElement`, `GetTriggerPayload`, `Subscribe`, `Unsubscribe`, `SyncState`
- Virtual methods: `RequestOpen`, `RequestClose`, `SetOpenInternal`, `NotifyStateChanged`
- Private `TriggerData` record struct
- Properties: `IsOpen`, `ActiveTriggerId`, `Payload`

**Interfaces (same file or adjacent):**
- `IComponentHandle` — public surface (IsOpen, RegisterTrigger, etc.)
- `IComponentHandleSubscriber<TReason>` — `OnOpenChangeRequested(bool open, TReason reason, ...)`

**Component subclasses become thin wrappers (~20-40 lines):**

| Component | Override/Addition |
|---|---|
| `DialogHandle` | Adds `OpenWithPayload()`, passes `interactionType` through `SetOpenInternal` |
| `PopoverHandle` | Passes `interactionType` through `SetOpenInternal` |
| `MenuHandle` | Adds `PopupId` property, `SetPopupId()` method |
| `TooltipHandle` | Straight inheritance, minimal overrides |
| `PreviewCardHandle` | Straight inheritance, minimal overrides |

Each component keeps its own `XxxHandle.cs`, `IXxxHandle`, `IXxxHandleSubscriber`, and `XxxHandleFactory` files.

**Estimated savings:** ~1,200 lines eliminated.

### C2. `OpenChangeEventArgs<TReason>`

**Location:** `src/BlazorBaseUI/OpenChangeEventArgs.cs`

Abstract base class consolidating the 7 near-identical event args classes.

**Base class contains:**
- `bool Open { get; }`
- `TReason Reason { get; }`
- `bool IsCanceled { get; private set; }`
- `void Cancel()` — sets `IsCanceled = true`
- Virtual `bool PreventUnmount { get; private set; }`
- Virtual `void PreventUnmountOnClose()` — components opt in

**Naming normalization:**
- `Canceled` → `IsCanceled` (consistent across all components)
- `PreventUnmountingOnClose` / `UnmountPrevented` → `PreventUnmount` property + `PreventUnmountOnClose()` method

**Per-component subclasses:**
```csharp
public sealed class DialogOpenChangeEventArgs : OpenChangeEventArgs<DialogOpenChangeReason>;
```

Components with extra properties override/extend:
- `MenuOpenChangeEventArgs` — adds `Payload`, `IsPropagationAllowed`, `AllowPropagation()`
- `CollapsibleOpenChangeEventArgs` / `SelectOpenChangeEventArgs` — no `PreventUnmountOnClose`

**Estimated savings:** ~200 lines eliminated.

### C3. `ParseSide`/`ParseAlign` → Global `Extensions.cs`

Move identical `ParseSide(string)` and `ParseAlign(string)` methods from `Popover/Extensions.cs` and `Menu/Extensions.cs` to `src/BlazorBaseUI/Extensions.cs`. Delete the component-specific copies.

`Side` and `Align` are already global types in `src/BlazorBaseUI/Enumerations.cs`.

### C4. `AccessibilityUtilities` Wiring

Connect the existing `AccessibilityUtilities.cs` methods to components that currently inline the same logic:
- `ApplyButtonAttributes` — non-`<button>` elements with button behavior
- `ResolveAriaLabelledBy` — label ID resolution
- `ApplyFocusableWhenDisabled` — disabled elements that stay focusable

---

## JS Consolidation

### J1. Extend `blazor-baseui-floating.js`

Add exported functions extracted from tooltip, preview-card, popover, and dialog:

**Transition helpers (from tooltip/preview-card — identical):**
- `setupTransitionEndListener(rootState, isOpen)`
- `waitForPopupAndStartTransition(rootState, isOpen)`
- `startTransition(rootState, isOpen)`

**Hover interaction (from tooltip/preview-card/popover — near-identical):**
- `initializeHoverInteraction(state, rootId, triggerElement, options)`
- `disposeHoverInteraction(state, rootId)`
- `updateHoverInteractionFloatingElement(state, rootId, element)`
- `setHoverInteractionOpen(state, rootId, open)`

**Dialog cleanup:**
- Delete private `checkForTransitionOrAnimation`, `parseCssDuration`, `getMaxTransitionDuration` from `dialog.js` — import existing exports from `floating.js`

**Double-RAF cleanup:**
- `dialog.js`, `tooltip.js`, `preview-card.js`, `popover.js` replace inline `requestAnimationFrame(() => requestAnimationFrame(...))` with import of `requestDoubleAnimationFrame` from `blazor-baseui-animations.js`

**Shared Escape handler:**
- Extract identical `handleGlobalKeyDown` from tooltip/preview-card into a shared function in `floating.js`

### J2. Wire Up `blazor-baseui-composite.js`

Migrate 6 components that currently inline roving tabindex + arrow key navigation:

| Component JS | Code Removed |
|---|---|
| `blazor-baseui-tabs.js` | `navigateToNext/Previous/First/Last`, `updateTabIndexes`, `getOrderedTabs` |
| `blazor-baseui-toggle.js` | `navigateToNext/Previous/First/Last`, `updateToggleTabIndexes`, `getOrderedToggles` |
| `blazor-baseui-radio.js` | `navigateToNext/Previous`, `updateTabIndexes`, `getOrderedRadios` |
| `blazor-baseui-toolbar.js` | `handleKeyDown`, `registerItem/unregisterItem`, `getFocusableItems` |
| `blazor-baseui-menubar.js` | `handleKeyDown`, `registerItem/unregisterItem`, `getFocusableItems` |
| `blazor-baseui-accordion-trigger.js` | `handleKeyDown`, arrow key + Home/End navigation |

Each component JS file imports `initialize`/`dispose`/`updateOptions` from `blazor-baseui-composite.js` and removes inline duplication.

### J3. New `blazor-baseui-activation.js`

**Location:** `src/BlazorBaseUI/wwwroot/blazor-baseui-activation.js`

Shared module for Space/Enter key activation on non-button elements.

**API:**
```javascript
export function initialize(element, options)
export function dispose(element)
```

**Options:**
```javascript
{
    nativeButtonGuard: true  // skip if element is a <button>
}
```

**Behavior:**
- Space `keydown` — `preventDefault` (prevent scroll)
- Space `keyup` — fire `click`
- Enter `keydown` — fire `click`

**Consumers:** `button.js`, `toggle.js`, `switch.js`, `checkbox.js` import and delete inline keydown/keyup handlers.

### J4. Wire Up `blazor-baseui-press-and-hold.js`

Migrate `blazor-baseui-number-field.js`:
- Remove inline `startAutoChange`/`stopAutoChange` logic (~45 lines)
- Remove duplicated constants (`START_AUTO_CHANGE_DELAY=400`, `CHANGE_VALUE_TICK_DELAY=60`, `SCROLLING_POINTER_MOVE_DISTANCE=8`)
- Import `initialize`/`dispose` from `blazor-baseui-press-and-hold.js`

### J5. Wire Up `blazor-baseui-popup-viewport.js`

Migrate `blazor-baseui-navigation-menu.js`:
- Remove inline ResizeObserver + viewport morph logic
- Import `initialize`/`dispose`/`onTriggerChange`/`contentChanged` from `blazor-baseui-popup-viewport.js`

---

## AGENTS.md Rule Addition

New rule **4a** under existing rule 4 (JavaScript Interop Rules):

> **JS Interop File Architecture:**
> - Every component requiring JS interop **must** have its own component-specific JS file (e.g., `blazor-baseui-dialog.js`)
> - Component JS files import shared behavior from functional JS modules (e.g., `blazor-baseui-floating.js`, `blazor-baseui-composite.js`)
> - Shared/functional JS modules must **never** contain component-specific logic
> - **Exception:** When the interaction is trivial (a single function call with no component-specific wiring), the C# component may import the shared JS module directly
> - When adding new behavior to a component, modify the component's JS file — not the shared module

---

## Execution Order

1. `ComponentHandleBase<TPayload, TReason>` — base class + migrate 5 components
2. `OpenChangeEventArgs<TReason>` — base class + migrate 7 components + normalize naming
3. `ParseSide`/`ParseAlign` — move to global `Extensions.cs`
4. `AccessibilityUtilities` — wire up to consuming components
5. AGENTS.md — add JS interop file architecture rule
6. `blazor-baseui-floating.js` extensions — transition helpers, hover interaction, dialog dedup, double-RAF, escape handler
7. Wire up `blazor-baseui-composite.js` — migrate 6 components
8. New `blazor-baseui-activation.js` — create module + migrate 4 components
9. Wire up `blazor-baseui-press-and-hold.js` — migrate number-field
10. Wire up `blazor-baseui-popup-viewport.js` — migrate navigation-menu

## Testing

- **C# changes**: Build solution (`dotnet build BlazorBaseUI.slnx`) after each step to verify 0 errors
- **JS changes**: Existing component Playwright tests validate behavior post-migration
- **Unit tests**: Existing bUnit tests continue to pass (Handle/EventArgs changes are internal)
- No new test files needed — consolidation preserves existing behavior
