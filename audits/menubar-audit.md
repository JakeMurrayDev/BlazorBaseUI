# MenuBar Component Audit: React Base UI vs Blazor Port

**Date:** 2026-03-31
**Component:** `Menubar` (React) -> `MenuBarRoot` (Blazor)
**React Source:** `.base-ui/packages/react/src/menubar/`
**Blazor Source:** `src/BlazorBaseUI/MenuBar/`

---

## 1. Missing Functional Gaps (The "Deficit" Table)

| React Feature/Utility | Status | Impact/Reasoning |
|----------------------|--------|------------------|
| `data-modal` attribute | **MISSING** | CSS selectors targeting `[data-modal]` on the menubar element will fail. React renders this standalone when `modal=true`. Blazor never renders it. |
| `data-has-submenu-open` standalone boolean | **DEVIATION** | Blazor renders `data-has-submenu-open="true"/"false"` (string, always present). React renders `data-has-submenu-open` standalone only when true, absent when false. CSS `[data-has-submenu-open]` selector works differently. |
| `useOpenInteractionType` tracking | **MISSING** | React tracks whether open was via mouse/touch/keyboard. Used to exclude scroll lock on touch (`openMethod !== 'touch'`). Blazor always applies scroll lock when `modal && hasSubmenuOpen`, causing potential scroll issues on iOS Safari when menu opened by touch. |
| Touch-exclusion for scroll lock | **MISSING** | Consequence of missing `useOpenInteractionType`. On iOS Safari, touch-opening a menu locks scroll unnecessarily. React: `useScrollLock(modal && hasSubmenuOpen && openMethod !== 'touch', contentElement)`. Blazor: `shouldLock = Modal && hasSubmenuOpen`. |
| `aria-orientation` attribute | **EXTRA** | Blazor explicitly sets `aria-orientation` on the menubar. React does NOT set this (CompositeRoot doesn't emit `aria-orientation`). Not harmful (WAI-ARIA default for menubar is horizontal), but not in React source. |
| CompositeRoot `highlightItemOnHover` scroll-into-view | **MISSING** | React's CompositeRoot auto-scrolls highlighted items into view during keyboard navigation. Blazor's JS keyboard handler does not call `scrollIntoView`. Only matters for very long menubars that overflow. |
| `interactionTypeProps` on root element | **MISSING** | React merges `onClick`/`onPointerDown` handlers from `useOpenInteractionType` onto the menubar root element to track interaction type. Blazor has no equivalent handlers on the root element. Linked to missing `useOpenInteractionType`. |

---

## 2. Architecture Audit

### Overengineering vs. Optimization

**Observation (JS Optimization):** `blazor-baseui-menubar.js` - Keyboard navigation (arrow keys, Home/End, roving tabindex) is correctly implemented entirely in JavaScript. No C#-to-JS interop round-trips during keydown events. **Optimal.**

**Observation (JS Optimization):** `blazor-baseui-scroll-lock.js` - Scroll lock acquisition/release delegated to shared JS utility with reference counting. Avoids interop overhead for lock management. **Optimal.**

**Observation (JS Optimization):** `registerItem`/`unregisterItem` in JS - Items sorted by `compareDocumentPosition`, tabindex managed entirely in JS. No DOM measurement round-trips to C#. **Optimal.**

**Observation (Architecture):** Counter-based submenu tracking (`openSubmenuCount`) vs React's FloatingTree event-based approach. The Blazor counter approach is simpler and handles race conditions better when multiple menus open/close in rapid succession (hovering between triggers). Prevents scroll-lock jank that would occur with boolean flip-flop. **Good design choice.**

**Observation (Architecture):** Pending registrations queue (`pendingRegistrations`/`pendingUnregistrations`) handles Blazor lifecycle where child components render before parent's `OnAfterRenderAsync`. This is a necessary Blazor-specific pattern that has no React equivalent. **Correct.**

**Observation (Architecture):** Menu components (`MenuRoot`, `MenuTypedTrigger`, `MenuTrigger`) correctly consume `MenuBarRootContext` via `[CascadingParameter]` and adapt behavior: `role="menuitem"`, hover-to-open, focus-to-open, click behavior, instant transitions (`MenuInstantType.Group`), positioning defaults (`Align.Start`), and focus return logic. This integration is thorough and matches React's behavior. **Excellent.**

No overengineering flags found. No React-ism patterns detected (no manual state-sync loops or forced rendering cycles).

---

## 3. Internal Dependency Checklist

| React Dependency | Purpose | Blazor Equivalent | Status |
|-----------------|---------|-------------------|--------|
| `MenubarContext` (React.createContext) | Cascade menubar state to child menus | `MenuBarRootContext` (CascadingValue) | **PRESENT** |
| `useMenubarContext()` | Consume context in Menu components | `[CascadingParameter] MenuBarRootContext?` | **PRESENT** |
| `CompositeRoot` | Keyboard navigation, roving tabindex, focus management | `blazor-baseui-menubar.js` (initMenuBar, handleKeyDown) | **PRESENT** |
| `CompositeList` / `CompositeItem` | Item registration for focus management | `registerItem` / `unregisterItem` in JS | **PRESENT** |
| `FloatingTree` / `FloatingNode` | Nested menu event coordination | Counter-based tracking (`openSubmenuCount`) in C# | **PRESENT** (different but functional approach) |
| `useScrollLock` | Lock document scroll when modal menu open | `blazor-baseui-scroll-lock.js` (acquireScrollLock) | **PRESENT** |
| `useOpenInteractionType` | Track mouse/touch/keyboard open method | N/A | **MISSING** |
| `useBaseUiId` | Generate stable unique ID | Not needed (AdditionalAttributes passthrough) | **N/A** |
| `getStateAttributesProps` / `StateAttributesMapping` | Convert state to data-attributes | `BuildComponentAttributes()` inline | **PRESENT** (partial - see data attribute gaps) |
| `MenubarDataAttributes` enum | Define data attribute names | Inline strings in `BuildComponentAttributes` | **PRESENT** (partial - missing `data-modal`) |
| `MenuOpenEventDetails` type | Menu open/close event payload | `MenuOpenChangeReason` enum + `MenuOpenChangeEventArgs` | **PRESENT** (adapted to Blazor patterns) |
| `interactionTypeProps` (from useOpenInteractionType) | Track interaction on root element | N/A | **MISSING** |

---

## 4. Detailed Attribute Audit

### HTML Attributes on Root Element

| Attribute | React | Blazor | Match |
|-----------|-------|--------|-------|
| `role="menubar"` | Yes | Yes | **MATCH** |
| `id` | Yes (from `useBaseUiId`) | Via `AdditionalAttributes` | **MATCH** (user-provided) |
| `data-orientation` | `"horizontal"` or `"vertical"` | `"horizontal"` or `"vertical"` | **MATCH** |
| `data-modal` | Standalone when `true`, absent when `false` | Not rendered | **MISMATCH** |
| `data-has-submenu-open` | Standalone when `true`, absent when `false` | `"true"`/`"false"` string, always present | **MISMATCH** |
| `data-disabled` | Standalone when `true`, absent when `false` | Boolean (Blazor auto-removes when `false`) | **MATCH** |
| `aria-orientation` | Not rendered | Rendered | **EXTRA** (harmless) |

### Props/Parameters

| React Prop | Default | Blazor Parameter | Default | Match |
|-----------|---------|-----------------|---------|-------|
| `orientation` | `'horizontal'` | `Orientation` | `Orientation.Horizontal` | **MATCH** |
| `loopFocus` | `true` | `LoopFocus` | `true` | **MATCH** |
| `modal` | `true` | `Modal` | `true` | **MATCH** |
| `disabled` | `false` | `Disabled` | `false` | **MATCH** |
| `render` | - | `Render` | - | **MATCH** |
| `className` | - | `ClassValue` | - | **MATCH** |
| - | - | `StyleValue` | - | **EXTRA** (intentional) |
| - | - | `ChildContent` | - | **EXTRA** (Blazor pattern) |
| - | - | `AdditionalAttributes` | - | **EXTRA** (Blazor pattern) |

### State Object

| React State Field | Blazor State Field | Match |
|------------------|-------------------|-------|
| `orientation` | `Orientation` | **MATCH** |
| `modal` | `Modal` | **MATCH** |
| `hasSubmenuOpen` | `HasSubmenuOpen` | **MATCH** |
| - | `Disabled` | **EXTRA** (reasonable addition) |

---

## 5. Menu-MenuBar Integration Audit

### MenuRoot.razor Menubar Behavior

| React Behavior | Blazor Implementation | Status |
|---------------|----------------------|--------|
| Detects `MenubarContext` to set `parent.type = 'menubar'` | `ParentType => MenuBarContext is not null ? MenuParentType.Menubar` | **PRESENT** |
| Sets `instantType: 'group'` for menubar reasons (triggerFocus, focusOut, triggerHover, listNavigation, siblingOpen) | Checks same reasons and sets `MenuInstantType.Group` | **PRESENT** |
| Passes `menubarElement` to JS for keyboard relay | Passes `MenuBarContext?.GetElement()` to `initializeRoot` | **PRESENT** |
| Calls `setHasSubmenuOpen` on open/close | `MenuBarContext?.SetHasSubmenuOpen(nextOpen)` | **PRESENT** |
| Suppresses hover-open when no menubar submenu is open | `if (MenuBarContext is not null && !MenuBarContext.GetHasSubmenuOpen()) return;` in `OnHoverOpen` | **PRESENT** |

### MenuTypedTrigger.razor / MenuTrigger.razor Menubar Behavior

| React Behavior | Blazor Implementation | Status |
|---------------|----------------------|--------|
| `role="menuitem"` when inside menubar | `attrs["role"] = "menuitem"` when `IsInMenuBar` | **PRESENT** |
| Hover-to-open when any menu open | `HandleMouseEnterAsync` checks `GetHasSubmenuOpen()` | **PRESENT** |
| Focus-to-open when any menu open | `HandleFocusAsync` checks `GetHasSubmenuOpen()` | **PRESENT** |
| Click changes from mousedown to click when open | `HandlePointerDownAsync` returns early when `currentlyOpen && IsInMenuBar` | **PRESENT** |
| Disabled from menubar context | `MenuBarContext?.Disabled ?? false` merged | **PRESENT** |
| Registers as CompositeItem | `MenuBarContext.RegisterItem(Element.Value)` on first render | **PRESENT** |
| Unregisters on dispose | `MenuBarContext.UnregisterItem(Element.Value)` in `DisposeAsync` | **PRESENT** |
| `onmouseenter` / `onfocus` handlers only in menubar | Conditionally added when `IsInMenuBar` | **PRESENT** |

### MenuPopup.razor Menubar Behavior

| React Behavior | Blazor Implementation | Status |
|---------------|----------------------|--------|
| Return focus unless outsidePress in menubar | Checks `parentType == MenuParentType.Menubar && reason != OutsidePress` | **PRESENT** |

### MenuPositioner.razor Menubar Behavior

| React Behavior | Blazor Implementation | Status |
|---------------|----------------------|--------|
| Default `Align.Start` for menubar menus | `isMenubar ? Align.Start : Align.Center` | **PRESENT** |

---

## 6. Test Coverage Assessment

| Test Type | Count | Coverage |
|-----------|-------|----------|
| Unit tests (bUnit) | 12 | Rendering, ARIA, attributes, context cascading, submenu tracking |
| E2E tests (Playwright) | 26 | Click, hover, keyboard, escape, submenu, disabled, focus management |
| Platforms | 2 | Server + WebAssembly |
| Test contract | Yes | `IMenuBarRootContract` |

---

## 7. Final Parity Score

### Scoring Breakdown

| Category | Weight | Score | Notes |
|----------|--------|-------|-------|
| Props/Parameters | 10% | 10/10 | All React props present with correct defaults |
| Context | 10% | 9/10 | All properties present; `rootId` not exposed (not needed) |
| State | 5% | 10/10 | All state fields present |
| Data Attributes | 15% | 7/10 | Missing `data-modal`; `data-has-submenu-open` format wrong; extra `aria-orientation` |
| Keyboard Navigation | 15% | 9/10 | Arrow, Home/End, loop, roving tabindex all present; missing scroll-into-view |
| Scroll Lock | 10% | 8/10 | Works correctly; missing touch exclusion |
| Menu Integration | 20% | 10/10 | Comprehensive: role, hover, focus, click, instant transitions, positioning, focus return |
| JS Architecture | 10% | 10/10 | Correct DOM-heavy logic in JS, no unnecessary interop |
| Test Coverage | 5% | 9/10 | 38 total tests across unit + E2E; good coverage |

### Score: **91/100**

### Verdict: **Production Ready** (with minor attribute fixes recommended)

The MenuBar component is functionally complete. All core behaviors - keyboard navigation, hover-to-open, focus-to-open, scroll lock, instant transitions, positioning, and focus management - are implemented correctly. The Menu-MenuBar integration is thorough and well-architected.

### Recommended Fixes (Priority Order)

1. **`data-has-submenu-open` format** (LOW) - Change from string `"true"/"false"` to boolean assignment so Blazor renders standalone when true and omits when false:
   ```csharp
   // Current:
   attrs["data-has-submenu-open"] = hasSubmenuOpen ? "true" : "false";
   // Fix:
   attrs["data-has-submenu-open"] = hasSubmenuOpen;
   ```

2. **`data-modal` attribute** (LOW) - Add missing attribute:
   ```csharp
   attrs["data-modal"] = Modal;
   ```

3. **`aria-orientation` removal** (COSMETIC) - Remove to match React output. WAI-ARIA defaults `menubar` to horizontal, so this is redundant when horizontal and divergent from source:
   ```csharp
   // Remove: attrs["aria-orientation"] = orientationString!;
   ```

4. **Touch scroll lock exclusion** (LOW) - Would require implementing interaction type tracking at the menubar level. Low priority since this is an iOS Safari edge case. Consider implementing if touch device support is a priority.
