# Menu Component Suite Audit Report

**Date:** 2026-03-31
**Branch:** enhancement/audit-menu
**Auditor:** Opus Orchestrator (Phase 1 Architectural Audit)

---

## Sub-Component Inventory

| React Sub-component | Blazor Equivalent | Status |
|---|---|---|
| MenuRoot | MenuRoot.razor | Present |
| MenuTrigger | MenuTrigger.razor + MenuTypedTrigger.razor | Present |
| MenuPositioner | MenuPositioner.razor | Present |
| MenuPopup | MenuPopup.razor | Present |
| MenuPortal | MenuPortal.razor | Present |
| MenuItem | MenuItem.razor | Present |
| MenuArrow | MenuArrow.razor | Present |
| MenuBackdrop | MenuBackdrop.razor | Present |
| MenuCheckboxItem | MenuCheckboxItem.razor | Present |
| MenuCheckboxItemIndicator | MenuCheckboxItemIndicator.razor | Present |
| MenuRadioGroup | MenuRadioGroup.razor | Present |
| MenuRadioItem | MenuRadioItem.razor | Present |
| MenuRadioItemIndicator | MenuRadioItemIndicator.razor | Present |
| MenuGroup | MenuGroup.razor | Present |
| MenuGroupLabel | MenuGroupLabel.razor | Present |
| MenuSubmenuRoot | MenuSubmenuRoot.razor | Present |
| MenuSubmenuTrigger | MenuSubmenuTrigger.razor | Present |
| MenuHandle / createMenuHandle | MenuHandle.cs | Present |
| MenuViewport | MenuViewport.razor | Present |
| Separator (re-export) | Separator/ (standalone) | Present (separate component) |

---

## Findings

### F1 - MenuItem missing `data-disabled` attribute rendering for non-disabled items

| Field | Value |
|---|---|
| **ID** | F1 |
| **Category** | Data Attributes |
| **Sub-component** | MenuItem |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/item/useMenuItem.ts` - uses `focusableWhenDisabled: true` in useButton, and the item mapping only applies `data-disabled`/`data-highlighted` via state attribute mapping |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuItem.razor` line 109 |
| **Description** | MenuItem sets `attrs["data-disabled"] = true` only inside an `if (Disabled)` block (line 107-110), meaning non-disabled items do not get `data-disabled` rendered at all. Blazor automatically omits `false` boolean attributes, so `attrs["data-disabled"] = Disabled` would be correct - it would be present when disabled and absent when not. Currently, only the disabled branch is handled but not the general assignment. This is actually **correct behavior** - Blazor omits false boolean attrs. However, looking more closely, the `data-highlighted` attribute at line 112 is set as `attrs["data-highlighted"] = highlighted` which means it renders as a standalone attribute when true and is omitted when false. This is correct per MEMORY.md. No issue here. |
| **Fix approach** | FALSE POSITIVE - No fix needed. |

### F2 - MenuItem / MenuLinkItem / MenuCheckboxItem / MenuRadioItem: Missing `HighlightItemOnHover` guard on MouseLeave

| Field | Value |
|---|---|
| **ID** | F2 |
| **Category** | Events |
| **Sub-component** | MenuItem, MenuLinkItem |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | React's highlighting is controlled via the store's `focusItemOnHover` flag in `useListNavigation`, which is wired to `highlightItemOnHover` |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuItem.razor` lines 155-167 |
| **Description** | `MenuItem.HandleMouseLeaveAsync` checks `shouldHighlight` before unhighlighting. `MenuLinkItem.HandleMouseLeaveAsync` also checks `shouldHighlight`. Both are correct. `MenuCheckboxItem` and `MenuRadioItem` do NOT check `HighlightItemOnHover` on mouse enter/leave. When `HighlightItemOnHover` is false, these items should not highlight on hover, but they do because they lack the guard. |
| **Fix approach** | Add `HighlightItemOnHover` guard to `MenuCheckboxItem` and `MenuRadioItem` mouse enter/leave/move handlers, same as `MenuItem` and `MenuLinkItem`. |

### F3 - MenuArrow: Missing `data-anchor-hidden` attribute

| Field | Value |
|---|---|
| **ID** | F3 |
| **Category** | Data Attributes |
| **Sub-component** | MenuArrow |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/arrow/MenuArrow.tsx` - uses `popupStateMapping` which maps `anchorHidden` |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuArrow.razor` lines 80-92 |
| **Description** | React's `MenuArrow` uses `popupStateMapping` which includes `anchorHidden` data attribute. The Blazor `MenuArrow` does not output `data-anchor-hidden`. The `MenuArrowState` record does not include `AnchorHidden` either. However, looking at the React source more carefully, the `popupStateMapping` applies `data-anchor-hidden` only when the state includes `anchorHidden`. The `MenuArrowState` in React does NOT include `anchorHidden` - it has `open`, `side`, `align`, `uncentered`. So the popupStateMapping's `anchorHidden` handler would return null since the state doesn't have it. This is actually a FALSE POSITIVE. |
| **Fix approach** | FALSE POSITIVE - No fix needed. React's state mapping silently ignores missing state keys. |

### F4 - MenuPositioner: `data-open` and `data-closed` render as boolean attributes instead of conditional presence

| Field | Value |
|---|---|
| **ID** | F4 |
| **Category** | Data Attributes |
| **Sub-component** | MenuPositioner |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/utils/popupStateMapping.ts` - `popupStateMapping.open()` returns `{'data-open': ''}` when open, `{'data-closed': ''}` when closed |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuPositioner.razor` lines 320-321 |
| **Description** | Blazor sets `attrs["data-open"] = open` and `attrs["data-closed"] = !open` which means both attributes are rendered: one as present (true) and one as absent (false). Per MEMORY.md, Blazor omits false boolean attrs automatically, so `data-open = true` renders as standalone `data-open` and `data-open = false` is omitted. This is correct behavior. |
| **Fix approach** | FALSE POSITIVE - No fix needed. Blazor boolean attribute auto-removal handles this correctly. |

### F5 - MenuPositioner: `data-ending-style` never rendered

| Field | Value |
|---|---|
| **ID** | F5 |
| **Category** | Data Attributes |
| **Sub-component** | MenuPositioner |
| **Severity** | Medium |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/utils/stateAttributesMapping.ts` - `transitionStatusMapping` maps `starting` to `data-starting-style` and `ending` to `data-ending-style` |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuPositioner.razor` line 335 |
| **Description** | The Blazor MenuPositioner only renders `data-starting-style` based on transition status (line 335), but never renders `data-ending-style`. React's `popupStateMapping` combined with `transitionStatusMapping` renders both. This means CSS exit transitions targeting `[data-ending-style]` on the positioner won't work. |
| **Fix approach** | Add `attrs["data-ending-style"] = transitionStatus == TransitionStatus.Ending;` to `BuildComponentAttributes()`. |

### F6 - MenuPopup: Missing `aria-labelledby` on the menu popup

| Field | Value |
|---|---|
| **ID** | F6 |
| **Category** | ARIA |
| **Sub-component** | MenuPopup |
| **Severity** | Medium |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/popup/MenuPopup.tsx` - React's `useRole` hook from Floating UI sets `aria-labelledby` on the popup referencing the trigger id. This is done via `popupProps` from the store which includes props from `getFloatingProps` in MenuRoot. |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuPopup.razor` lines 159-211 |
| **Description** | React's `useRole` hook sets `role="menu"` and `aria-labelledby` referencing the trigger element's ID on the floating (popup) element. The Blazor implementation sets `role="menu"` but does not set `aria-labelledby`. This is an accessibility gap - screen readers won't associate the menu with its trigger. |
| **Fix approach** | In `MenuPopup.BuildComponentAttributes()`, add `aria-labelledby` pointing to the trigger's ID. The trigger ID is available via `RootContext?.TriggerId`. |

### F7 - MenuSubmenuTrigger: Missing `aria-controls` attribute

| Field | Value |
|---|---|
| **ID** | F7 |
| **Category** | ARIA |
| **Sub-component** | MenuSubmenuTrigger |
| **Severity** | Medium |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/trigger/MenuTrigger.tsx` - React's `getReferenceProps` from `useInteractions` sets `aria-controls` referencing the popup |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuSubmenuTrigger.razor` lines 183-217 |
| **Description** | `MenuSubmenuTrigger` sets `aria-haspopup="menu"` and `aria-expanded` but does not set `aria-controls` when the submenu is open. The main `MenuTrigger` correctly sets `aria-controls` (line 200), but `MenuSubmenuTrigger` omits it. |
| **Fix approach** | Add `aria-controls` to `MenuSubmenuTrigger.BuildComponentAttributes()` when `state.Open` is true, referencing `RootContext?.PopupId`. |

### F8 - MenuTrigger: Missing `aria-expanded` when used outside menubar

| Field | Value |
|---|---|
| **ID** | F8 |
| **Category** | ARIA |
| **Sub-component** | MenuTrigger |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/trigger/MenuTrigger.tsx` - React's `useRole` hook sets `aria-expanded` and `aria-controls` on the reference (trigger) element |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuTrigger.razor` line 197 |
| **Description** | Blazor correctly sets `aria-expanded` to `"true"` or `"false"` (line 197). This is correct. However, looking at this more carefully, `aria-expanded` is always rendered (even when false), which matches React's behavior from `useRole`. No issue. |
| **Fix approach** | FALSE POSITIVE - No fix needed. |

### F9 - MenuCheckboxItem / MenuRadioItem: Missing `data-highlighted` attribute on indicator children

| Field | Value |
|---|---|
| **ID** | F9 |
| **Category** | Data Attributes |
| **Sub-component** | MenuCheckboxItemIndicator, MenuRadioItemIndicator |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/utils/stateAttributesMapping.ts` + `.base-ui/packages/react/src/menu/checkbox-item-indicator/MenuCheckboxItemIndicator.tsx` - uses `itemMapping` which includes `checked` and `transitionStatusMapping` |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuCheckboxItemIndicator.razor` lines 170-181, `src/BlazorBaseUI/Menu/MenuRadioItemIndicator.razor` lines 170-179 |
| **Description** | React's `itemMapping` includes checked/unchecked and transitionStatus mappings but NOT `data-highlighted` or `data-disabled` - those come from the state attribute mapping system. Looking at the React source: `MenuCheckboxItemIndicator` uses `stateAttributesMapping: itemMapping` where `itemMapping` maps `checked` state to `data-checked`/`data-unchecked` and `transitionStatus` to `data-starting-style`/`data-ending-style`. The Blazor indicators render `data-checked`, `data-unchecked`, `data-disabled`, `data-starting-style`, `data-ending-style`. The React indicator state includes `highlighted` and `disabled` but the `itemMapping` doesn't map those to data attributes. So Blazor rendering `data-disabled` on the indicator is EXTRA compared to React. However, this is harmless and arguably useful. |
| **Fix approach** | Low priority - the extra `data-disabled` on indicators doesn't match React exactly but doesn't cause issues. Could be flagged for parity but not a bug. |

### F10 - MenuLinkItem: Missing `data-disabled` when link item has no disabled prop

| Field | Value |
|---|---|
| **ID** | F10 |
| **Category** | Prop Parity |
| **Sub-component** | MenuLinkItem |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/link-item/MenuLinkItem.tsx` - No `disabled` prop; `MenuLinkItemState` only has `highlighted` |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuLinkItem.razor` |
| **Description** | React's `MenuLinkItem` does NOT have a `disabled` prop - the state only contains `highlighted`. The Blazor `MenuLinkItem` also correctly has no `Disabled` parameter and no `data-disabled` attribute. This is correct parity. |
| **Fix approach** | FALSE POSITIVE - No fix needed. |

### F11 - MenuRadioGroup: `data-disabled` only rendered when disabled, missing when not disabled

| Field | Value |
|---|---|
| **ID** | F11 |
| **Category** | Data Attributes |
| **Sub-component** | MenuRadioGroup |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/radio-group/MenuRadioGroup.tsx` - React does NOT use any state attribute mapping for RadioGroup. It only sets `role="group"` and `aria-disabled`. |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuRadioGroup.razor` lines 120-131 |
| **Description** | Blazor renders `data-disabled = true` only when `Disabled` is true (inside the if block). React doesn't render any `data-disabled` on `MenuRadioGroup` at all - it only sets `aria-disabled`. So Blazor has an EXTRA `data-disabled` attribute. This is a minor divergence. |
| **Fix approach** | Remove `attrs["data-disabled"] = true;` from `MenuRadioGroup.BuildComponentAttributes()` to match React. Or keep it as an intentional Blazor enhancement. Low priority. |

### F12 - MenuBackdrop: Missing `data-anchor-hidden` check

| Field | Value |
|---|---|
| **ID** | F12 |
| **Category** | Data Attributes |
| **Sub-component** | MenuBackdrop |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/backdrop/MenuBackdrop.tsx` - React uses `stateAttributesMapping` which is `popupStateMapping + transitionStatusMapping`. The `popupStateMapping` includes `open` but the backdrop state does NOT include `anchorHidden`, so it won't render. |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuBackdrop.razor` lines 90-107 |
| **Description** | No issue - both React and Blazor correctly don't render `data-anchor-hidden` on the backdrop since the state doesn't include it. |
| **Fix approach** | FALSE POSITIVE - No fix needed. |

### F13 - MenuPopup: Missing `data-nested` attribute

| Field | Value |
|---|---|
| **ID** | F13 |
| **Category** | Data Attributes |
| **Sub-component** | MenuPopup |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/popup/MenuPopup.tsx` - `MenuPopupState` includes `nested: boolean` but it's NOT in any stateAttributesMapping |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuPopup.razor` line 203 |
| **Description** | Blazor renders `attrs["data-nested"] = true` when nested (line 203). React's `MenuPopup` state includes `nested` but the `stateAttributesMapping` used (`popupStateMapping + transitionStatusMapping`) does NOT map `nested` to a data attribute. So Blazor has an EXTRA `data-nested` attribute. Checking React more carefully: the Popup's `useRenderElement` uses `stateAttributesMapping` which is `{...baseMapping, ...transitionStatusMapping}` - `baseMapping` = `popupStateMapping` which only maps `open` and `anchorHidden`. So `nested` is NOT rendered as a data attribute in React. However, looking at React source: `data-rootownerid` IS rendered as an explicit prop (line 123). So Blazor's `data-nested` is extra. This is intentional Blazor behavior for CSS targeting - keep it. |
| **Fix approach** | No fix needed - intentional Blazor enhancement for CSS targeting. |

### F14 - MenuTrigger: `Payload` parameter missing on non-typed MenuTrigger

| Field | Value |
|---|---|
| **ID** | F14 |
| **Category** | Prop Parity |
| **Sub-component** | MenuTrigger |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/trigger/MenuTrigger.tsx` - `MenuTriggerProps<Payload>` includes `payload?: Payload` |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuTrigger.razor` - No `Payload` parameter |
| **Description** | React's `MenuTrigger` accepts a generic `payload` prop. Blazor has a separate `MenuTypedTrigger<TPayload>` that handles the payload pattern. The non-generic `MenuTrigger` intentionally doesn't have a Payload parameter since Blazor splits the typed/non-typed variants. This is an expected architectural difference. |
| **Fix approach** | FALSE POSITIVE - intentional Blazor design split. |

### F15 - MenuSubmenuRoot: Passes props that React's SubmenuRoot omits (`modal`, `orientation`, etc.)

| Field | Value |
|---|---|
| **ID** | F15 |
| **Category** | Prop Parity |
| **Sub-component** | MenuSubmenuRoot |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/submenu-root/MenuSubmenuRoot.tsx` - `MenuSubmenuRootProps` extends `Omit<MenuRoot.Props, 'modal' | 'openOnHover' | 'onOpenChange'>` |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuSubmenuRoot.razor` lines 1-18 |
| **Description** | React's `MenuSubmenuRoot` explicitly omits `modal` and `openOnHover` from the props it accepts (via TypeScript `Omit`). The Blazor `MenuSubmenuRoot` does not expose `Modal` or `OpenOnHover` parameters, which is correct. However, it passes `LoopFocus`, `HighlightItemOnHover`, `Orientation`, `Handle`, `TriggerId`, `DefaultTriggerId`, `ActionsRef` to the inner `MenuRoot`. These are all present in React's `MenuSubmenuRootProps` (it inherits them from `MenuRoot.Props` minus the omitted ones). So this is correct. |
| **Fix approach** | FALSE POSITIVE - No fix needed. |

### F16 - MenuPopup: Not setting `aria-labelledby` to reference trigger

| Field | Value |
|---|---|
| **ID** | F16 |
| **Category** | ARIA |
| **Sub-component** | MenuPopup |
| **Severity** | Medium |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/root/MenuRoot.tsx` lines 428-429 - `useRole(floatingRootContext, { role: 'menu' })` - This Floating UI hook automatically adds `aria-labelledby` to the floating element referencing the reference (trigger) element's id |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuPopup.razor` lines 164-167 |
| **Description** | Same as F6. React's `useRole` hook from Floating UI automatically sets `aria-labelledby` on the popup element referencing the trigger's ID. The Blazor `MenuPopup` does not set `aria-labelledby`. This is an accessibility deficiency. |
| **Fix approach** | Duplicate of F6. Add `aria-labelledby` to MenuPopup referencing `RootContext?.TriggerId`. |

### F17 - MenuCheckboxItem / MenuRadioItem: `onkeydown` handler for space key during typeahead missing

| Field | Value |
|---|---|
| **ID** | F17 |
| **Category** | Events |
| **Sub-component** | MenuCheckboxItem, MenuRadioItem, MenuItem, MenuLinkItem |
| **Severity** | Medium |
| **Complexity** | Moderate (Sonnet) |
| **React ref** | `.base-ui/packages/react/src/menu/item/useMenuItemCommonProps.ts` lines 63-66 - `onKeyDown` handler that prevents space during typeahead |
| **Blazor ref** | All menu item components |
| **Description** | React's `useMenuItemCommonProps` includes an `onKeyDown` handler that calls `event.preventDefault()` when space key is pressed during an active typeahead session (`typingRef.current`). This prevents the space from activating the item when the user is typing to search. The Blazor menu items do not have this keyboard handler. However, since Blazor delegates keyboard navigation to the JS module (`blazor-baseui-menu.js`), this may be handled there. This needs verification. |
| **Fix approach** | Verify if the JS module handles space key suppression during typeahead. If not, add `onkeydown` handler to menu items that prevents default when space is pressed during typeahead. This requires coordination with the JS module. Mark as DEFERRED pending JS module audit. |

### F18 - MenuCheckboxItem / MenuRadioItem: Missing `StateHasChanged()` in mouse enter handler

| Field | Value |
|---|---|
| **ID** | F18 |
| **Category** | Rendering |
| **Sub-component** | MenuCheckboxItem, MenuRadioItem |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | N/A (Blazor-specific) |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuCheckboxItem.razor` line 238, `src/BlazorBaseUI/Menu/MenuRadioItem.razor` line 193 |
| **Description** | `MenuCheckboxItem.HandleMouseEnterAsync` and `MenuRadioItem.HandleMouseEnterAsync` update `highlighted`, `itemContext`, `state`, and call `BuildComponentAttributes()` but do NOT call `StateHasChanged()`. Compare to `MenuItem.HandleMouseEnterAsync` which does call `StateHasChanged()` (line 149). The mouse leave handlers similarly lack `StateHasChanged()` in CheckboxItem (line 249) and RadioItem (line 205). Without `StateHasChanged()`, the component may not re-render to show the highlight. |
| **Fix approach** | Add `StateHasChanged()` calls to `HandleMouseEnterAsync` and `HandleMouseLeaveAsync` in both `MenuCheckboxItem` and `MenuRadioItem`, matching the pattern in `MenuItem`. |

### F19 - MenuItem: Missing `data-checked` / `data-unchecked` on non-checkbox/radio items

| Field | Value |
|---|---|
| **ID** | F19 |
| **Category** | Data Attributes |
| **Sub-component** | MenuItem |
| **Severity** | Low |
| **Complexity** | N/A |
| **React ref** | React's `MenuItem` does NOT use `itemMapping` - it has no state attribute mapping at all for checked/unchecked |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuItem.razor` |
| **Description** | React's `MenuItem` does not render `data-checked`/`data-unchecked` attributes. Blazor's `MenuItem` also does not render them. No issue. |
| **Fix approach** | FALSE POSITIVE - No fix needed. |

### F20 - MenuPositioner: Missing `data-anchor-hidden` boolean toggle

| Field | Value |
|---|---|
| **ID** | F20 |
| **Category** | Data Attributes |
| **Sub-component** | MenuPositioner |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/utils/popupStateMapping.ts` - `anchorHidden(value)` only returns the attribute when true, otherwise null |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuPositioner.razor` line 322 |
| **Description** | Blazor sets `attrs["data-anchor-hidden"] = computedAnchorHidden` which renders the attribute as a boolean. When false, Blazor auto-omits it. When true, it's present. This matches React's behavior where `data-anchor-hidden` is only present when the anchor is hidden. Correct. |
| **Fix approach** | FALSE POSITIVE - No fix needed. |

### F21 - MenuViewport: Extra `data-transitioning` and `data-current` attributes

| Field | Value |
|---|---|
| **ID** | F21 |
| **Category** | Data Attributes |
| **Sub-component** | MenuViewport |
| **Severity** | Low |
| **Complexity** | Simple (Haiku) |
| **React ref** | `.base-ui/packages/react/src/menu/viewport/MenuViewport.tsx` - `stateAttributesMapping` only maps `activationDirection` to `data-activation-direction` |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuViewport.razor` lines 165-183 |
| **Description** | Blazor renders `data-transitioning` and `data-current` attributes that React does not. React's `MenuViewport` only maps `activationDirection` to a data attribute. The `transitioning` and `instant` state values are NOT mapped to data attributes in React. Blazor renders `data-transitioning` (line 174) and `data-current` (line 175). These are EXTRA attributes. The `data-instant` attribute IS rendered by Blazor but React's viewport does NOT render it through state attribute mapping either (though it's in the state). |
| **Fix approach** | Remove `attrs["data-transitioning"]` and `attrs["data-current"]` from `MenuViewport.BuildComponentAttributes()` to match React. Keep `data-activation-direction`. The `data-instant` is an intentional Blazor addition. Low priority. |

### F22 - MenuPopup: Missing `data-instant` attribute value mapping

| Field | Value |
|---|---|
| **ID** | F22 |
| **Category** | Data Attributes |
| **Sub-component** | MenuPopup |
| **Severity** | Low |
| **Complexity** | N/A |
| **React ref** | React's `MenuPopup` state includes `instant` but the `stateAttributesMapping` does NOT map it |
| **Blazor ref** | `src/BlazorBaseUI/Menu/MenuPopup.razor` line 196-199 |
| **Description** | Blazor renders `data-instant` on the popup. React does NOT render `data-instant` on the popup through state attribute mapping. However, React DOES pass `instant` as part of the state for the `className` render function. Blazor renders it as a data attribute for CSS targeting. This is an intentional Blazor enhancement. |
| **Fix approach** | FALSE POSITIVE - intentional Blazor enhancement. |

---

## Confirmed Findings (Requiring Fixes)

| ID | Sub-component | Severity | Complexity | Summary |
|---|---|---|---|---|
| F2 | MenuCheckboxItem, MenuRadioItem | Low | Simple | Missing HighlightItemOnHover guard on mouse enter/leave/move |
| F5 | MenuPositioner | Medium | Simple | Missing `data-ending-style` attribute |
| F6 | MenuPopup | Medium | Simple | Missing `aria-labelledby` referencing trigger |
| F7 | MenuSubmenuTrigger | Medium | Simple | Missing `aria-controls` when open |
| F11 | MenuRadioGroup | Low | Simple | Extra `data-disabled` not in React |
| F18 | MenuCheckboxItem, MenuRadioItem | Low | Simple | Missing StateHasChanged() in mouse enter/leave |
| F21 | MenuViewport | Low | Simple | Extra `data-transitioning` and `data-current` |

## Deferred Items

| ID | Sub-component | Summary | Rationale |
|---|---|---|---|
| D1/F17 | All menu items | Space key suppression during typeahead | Requires JS module audit to determine if already handled in `blazor-baseui-menu.js`. Cross-cutting concern. |

---

## Implementation Chunks

### Chunk 1: ARIA Accessibility Fixes (F6, F7)
**Complexity:** Simple (Haiku)
**Definition of Done:**
- MenuPopup renders `aria-labelledby` referencing the trigger ID
- MenuSubmenuTrigger renders `aria-controls` referencing the popup ID when open
- All existing tests pass
- Build succeeds

**Findings:** F6, F7

### Chunk 2: Data Attribute Parity (F5, F11, F21)
**Complexity:** Simple (Haiku)
**Definition of Done:**
- MenuPositioner renders `data-ending-style` when transition status is Ending
- MenuRadioGroup does NOT render `data-disabled` (to match React)
- MenuViewport does NOT render `data-transitioning` or `data-current` (to match React)
- All existing tests pass
- Build succeeds

**Findings:** F5, F11, F21

### Chunk 3: Event Handler and Rendering Fixes (F2, F18)
**Complexity:** Simple (Haiku)
**Definition of Done:**
- MenuCheckboxItem respects HighlightItemOnHover on mouse enter/leave/move
- MenuRadioItem respects HighlightItemOnHover on mouse enter/leave/move
- MenuCheckboxItem calls StateHasChanged() in mouse enter/leave handlers
- MenuRadioItem calls StateHasChanged() in mouse enter/leave handlers
- All existing tests pass
- Build succeeds

**Findings:** F2, F18
