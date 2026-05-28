# Scroll Area Functional Audit

Date: 2026-05-28

Component: `ScrollArea`

React source audited: `.base-ui/packages/react/src/scroll-area`

Documentation audited: <https://base-ui.com/react/components/scroll-area>

Blazor source audited: `src/BlazorBaseUI/ScrollArea`, `src/BlazorBaseUI/wwwroot/blazor-baseui-scroll-area.js`

Spec artifacts:

- `../base-ui-specs/scroll-area/SPEC.md`
- `../base-ui-specs/scroll-area/pitfalls.md`

## React Parts Found

- `ScrollAreaRoot`
- `ScrollAreaViewport`
- `ScrollAreaContent`
- `ScrollAreaScrollbar`
- `ScrollAreaThumb`
- `ScrollAreaCorner`

## Resolved Gaps

| Gap | React source behavior | Blazor repair | Verification |
| --- | --- | --- | --- |
| Component absent from library | React exposes six Scroll Area parts. | Added `ScrollAreaRoot`, `ScrollAreaViewport`, `ScrollAreaContent`, `ScrollAreaScrollbar`, `ScrollAreaThumb`, and `ScrollAreaCorner`. | Build, bUnit, Playwright |
| DOM measurement absent | React measures viewport/content and computes hidden axes, corner size, thumb geometry, and overflow distances. | Added component JS module with `ResizeObserver`, geometry computation, CSS variables, and low-frequency Blazor state callbacks. | Playwright initial measurement tests |
| State/data attributes absent | React applies overflow and scrolling attributes to root, viewport, content, scrollbar, and thumb. | Added C# attribute helper plus immediate JS DOM synchronization for scroll-time state. | bUnit attribute tests and Playwright attribute assertions |
| Scrollbar interaction absent | React supports wheel, track click, thumb drag, pointer capture, and axis-specific scrolling state. | Implemented wheel prevention, track click scrolling, pointer capture, drag mapping, and scroll timeout handling in JS. | Playwright `TrackClickAndThumbDrag_UpdateViewportScrollPosition` |
| Root/child JS lifecycle race | React effects register against an initialized tree. Blazor child `OnAfterRenderAsync` can run before root JS initialization. | JS now queues pending viewport/content/scrollbar/thumb/corner registrations by root id and replays them from `initializeRoot`. | Exact failed server case and full Playwright suite |
| Pointer handler recursion | Event handler closure names shadowed module functions and recursed. | Renamed local closures to `onPointerDown`, `onPointerMove`, `onPointerUp`, and `onWheel`. | Manual page-error check and Playwright drag test |
| RTL edge normalization absent | React normalizes logical start/end with negative RTL `scrollLeft` behavior. | JS handles negative RTL scroll offsets and applies logical overflow edge attributes. | Playwright RTL test |
| `keepMounted` behavior absent | React renders tracks without overflow when `keepMounted` is true, but corner remains absent unless both axes overflow. | Added `KeepMounted` parameter on scrollbar and render gating consistent with React. | bUnit and Playwright keep-mounted tests |
| Scrollbar hiding style absent | React hides native scrollbars with shared utility styles. | JS injects `.base-ui-disable-scrollbar`; viewport applies the class. | bUnit viewport class test and Playwright visual geometry |
| Demo missing | Demo navigation listed Scroll Area as disabled. | Added Server and WASM demo pages and enabled the nav item. | Solution build |
| Documentation samples incomplete | Base UI documentation includes basic usage, both scrollbars, gradient scroll fade, and combining with Tabs examples. | Rebuilt the Scroll Area demo into discrete sections for each documentation sample plus a keep-mounted verification section. | Playwright CLI section-title check |
| Demo thumb overshoot | React example CSS lets component geometry own the thumb's scroll-axis size. | Removed `flex: 1` from demo/test thumbs and added orientation-specific cross-axis sizing. | Playwright CLI DOM bounds check and Playwright regression assertion |
| Keep-mounted demo did not scroll | `keepMounted` controls mounting only; scrollability still requires overflow content. | Replaced compact keep-mounted content with two-axis overflow content while retaining mounted tracks. | Playwright CLI scrollTop/scrollLeft check |
| Demo horizontal rail/corner misalignment | React positioning expects the horizontal rail, vertical rail, and corner to meet without extra visual offsets. | Removed scrollbar/corner margins in the Fluent sample styling and made the corner use the same rail background token. | Playwright CLI dark-mode alignment check and Playwright geometry assertion |
| Demo horizontal thumb origin misalignment | Thumb transform math assumes the static thumb starts at the track's padded origin. | Replaced axis centering with `justify-content: flex-start` and orientation-specific flex direction in demo/test styles. | Playwright CLI zero-origin check and Playwright regression assertion |
| Combining With Tabs demo flicker | The documentation composition nests a tab list in a horizontal scroll container while panels remain on the tab root; switching tabs must not expose two panels or resize the section. | Repaired Tabs panel handoff JS to resolve panels by `aria-controls` from the active list and compute the common panel scope instead of assuming `listElement.parentElement` contains panels. | Corrected-video Playwright CLI verification and Tabs nested-list regression test |

## Parity Matrix

| React hook/utility/source | Blazor equivalent | Status |
| --- | --- | --- |
| `useRender` | `RenderElement<TState>` on all parts | Verified |
| Root context | `ScrollAreaRootContext` cascade | Verified |
| Viewport context | `ScrollAreaViewportContext` cascade | Verified |
| Scrollbar context | `ScrollAreaScrollbarContext` cascade | Verified |
| `useDirection` | `DirectionProviderContext` plus JS RTL scroll normalization | Verified |
| `useResizeObserver` | JS `ResizeObserver` on viewport/content | Verified |
| `styleDisableScrollbar` | Injected `.base-ui-disable-scrollbar` rule | Verified |
| `getOffset` | JS `getOffset(element, prop, axis)` | Verified |
| `normalizeScrollOffset` | JS normalization with 1px edge tolerance | Verified |
| `SCROLL_TIMEOUT` | JS `SCROLL_TIMEOUT = 500` | Verified |
| `MIN_THUMB_SIZE` | JS `MIN_THUMB_SIZE = 16` | Verified |
| CSS.registerProperty overflow vars | JS registration with WebKit guard | Verified |
| Root state attributes | `ScrollAreaAttributeHelper.AddRootStateAttributes` plus JS immediate sync | Verified |
| Scrollbar state attributes | `ScrollAreaScrollbarState` plus JS axis-scoped `data-scrolling` | Verified |
| Thumb orientation attribute | `ScrollAreaThumbState` plus JS DOM sync | Verified |
| Track pointer down | JS `handleScrollbarPointerDown` | Verified |
| Thumb drag | JS pointer capture and drag-to-scroll mapping | Verified |
| Demo thumb sizing | Orientation-specific CSS: vertical `width: 100%`, horizontal `height: 100%`; no `flex: 1` | Verified |
| Demo thumb origin | Scrollbar flex layout starts thumbs at the track's padded origin before JS transforms are applied | Verified |
| Wheel prevention | JS non-passive wheel handler with edge guard | Verified |
| Animation-settled recompute | JS waits for descendant animations and recomputes | Verified |
| React disabled state | No React Scroll Area disabled API exists | Accounted |

## Attribute Matrix

| Part | React attributes/CSS variables | Blazor status |
| --- | --- | --- |
| Root | `role=presentation`, `data-scrolling`, `data-has-overflow-x`, `data-has-overflow-y`, edge data attributes, `--scroll-area-corner-height`, `--scroll-area-corner-width` | Present |
| Viewport | `role=presentation`, `tabIndex`, overflow data attributes, `overflow: scroll`, scrollbar-disabling class, overflow CSS vars | Present |
| Content | `role=presentation`, overflow data attributes, `min-width: fit-content` | Present |
| Scrollbar | `data-orientation`, `data-hovering`, axis `data-scrolling`, overflow data attributes, absolute positioning, thumb CSS variable | Present |
| Thumb | `data-orientation`, orientation-specific size style | Present |
| Corner | absolute bottom/end positioning, measured width/height | Present |

## Verification Commands

| Command | Result | Log |
| --- | --- | --- |
| `dotnet build BlazorBaseUI.slnx` | Passed, 0 warnings, 0 errors | `docs/audits/logs/scroll-area-dotnet-build.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~ScrollAreaTests"` | Passed, 18/18 | `docs/audits/logs/scroll-area-bunit-tests.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~ScrollAreaTests"` | Passed, 14/14 | `docs/audits/logs/scroll-area-playwright-tests.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Tabs"` | Passed, 119/119 | `docs/audits/logs/scroll-area-tabs-bunit-tests.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~TabsTests"` | Passed, 86/86 | `docs/audits/logs/scroll-area-tabs-playwright-tests.log` |
| Playwright CLI demo verification against `http://127.0.0.1:5177/scroll-area` | Passed: sections present, tabs horizontal rail aligned, keep-mounted horizontal rail/corner aligned in dark mode, keep-mounted scrolls, console has 0 errors/0 warnings | `docs/audits/logs/scroll-area-demo-playwright-cli.log` |
| Playwright CLI investigation against `http://127.0.0.1:5177/scroll-area --headed` | Passed: tabs and keep-mounted horizontal thumbs start at padded track origin, keep-mounted thumb shifts after scroll, console has 0 errors/0 warnings | `docs/audits/logs/scroll-area-investigation.log` |
| Playwright CLI corrected-video investigation against `http://127.0.0.1:5177/scroll-area --headed` | Passed: nested tabs list parent is the ScrollArea content wrapper, active panels are rooted on the tabs shell, `maxVisibleCount` stayed 1, shell height stayed 160px, and horizontal strip scrollLeft stayed 0 during tab changes | `docs/audits/logs/scroll-area-tabs-flicker-investigation.log` |
| `node --check` on Scroll Area and Tabs source/minified JS | Passed | `docs/audits/logs/scroll-area-js-syntax-check.log` |
| `curl -L https://base-ui.com/react/components/scroll-area.md \| rg ...` | Passed: documentation examples identified | `docs/audits/logs/scroll-area-docs-check.log` |
| `bash scripts/lint-rules.sh` | Passed, 0 violations. The log records existing macOS `grep -P` warnings before the zero-violation summary. | `docs/audits/logs/scroll-area-lint-rules.log` |
| `git diff --check` and `git diff --cached --check` | Passed, no whitespace errors | `docs/audits/logs/scroll-area-git-diff-check.log` |

## Playwright State Coverage

- Initial overflow measurement and thumb geometry.
- Active scrolling state and 500ms timeout reset.
- Track click and thumb drag.
- Focusable scrollable viewport.
- Keep-mounted non-overflow tracks.
- Per-edge overflow threshold.
- RTL horizontal start/end normalization.
- Server and WebAssembly render modes.
- Horizontal thumb bounds remain inside the visible track.
- Horizontal rail, vertical rail, and corner alignment.
- Horizontal and vertical thumb zero-scroll origin alignment.
- Nested TabsList inside the Scroll Area documentation composition keeps exactly one visible panel through activation.

## Manual Checks

- Compared React files under `.base-ui/packages/react/src/scroll-area/root`, `viewport`, `content`, `scrollbar`, `thumb`, `corner`, and `utils`.
- Compared Base UI Scroll Area documentation examples and confirmed demo sections for Basic Usage, Both Scrollbars, Gradient Scroll Fade, and Combining With Tabs.
- Confirmed all React data-attribute files have corresponding Blazor state/attribute handling.
- Confirmed DOM-heavy behavior remains in JS: measurement, wheel prevention, pointer capture, drag math, scroll timers, CSS variable mutation, and ResizeObserver callbacks.
- Confirmed Blazor lifecycle owns render state and context propagation while JS reports only low-frequency state transitions.
- Confirmed child-before-root registration is accounted for in the JS module and recorded in `../base-ui-specs/scroll-area/pitfalls.md`.
- Confirmed `ScrollArea` demo exists for Server and WASM routes and the navigation item is enabled.
- Confirmed demo thumb CSS does not use flex growth and horizontal thumbs stay within their measured tracks.
- Confirmed the Keep Mounted demo section scrolls on both axes while both scrollbars are mounted.
- Confirmed the Tabs and Keep Mounted horizontal scrollbars align with the viewport/corner in dark-mode demo styling.
- Confirmed `justify-content: center` was the residual horizontal misalignment cause after removing `flex: 1`; demo/test scrollbars now start thumbs at the padded origin.
- Confirmed minified and non-minified Scroll Area JS files are synchronized after source changes.
- Confirmed the corrected flicker video was a Tabs panel handoff issue exposed by the ScrollArea tabs composition, not ScrollArea geometry or demo CSS.
- Confirmed Tabs JS no longer assumes `listElement.parentElement` contains panels; controlled panels are resolved from tab `aria-controls`, which supports nested lists inside ScrollArea content.

## Final State

No remaining Scroll Area parity gaps were identified in the audited React source after the repairs above. The ScrollArea documentation tabs composition no longer flickers or resizes during tab activation.
