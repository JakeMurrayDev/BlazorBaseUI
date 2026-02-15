# BlazorBaseUI

## Project Overview

BlazorBaseUI is a Blazor component library that ports [Base UI](https://base-ui.com/) (React) components to Blazor. The library provides unstyled, accessible UI primitives for building design systems.

### Repository Links

- **GitHub**: <https://github.com/JakeMurrayDev/BlazorBaseUI/>
- **GitHub API**: <https://api.github.com/repos/JakeMurrayDev/BlazorBaseUI>

### Technology Stack

- **.NET Version**: .NET 10
- **Blazor**: Server and WebAssembly (Auto render mode)
- **Testing**: xUnit v3, bUnit, Playwright, Shouldly, NSubstitute

### Project Structure

```
BlazorBaseUI/
├── src/
│   ├── BlazorBaseUI/              # Main component library
│   │   ├── [Component]/           # Component folders
│   │   └── wwwroot/               # JavaScript modules
│   └── BlazorBaseUI.Utilities/    # Shared utilities
├── demo/
│   └── BlazorBaseUI.Demo/         # Demo application (Server + Client)
└── tests/
    ├── BlazorBaseUI.Tests/        # Unit tests (bUnit)
    ├── BlazorBaseUI.Tests.Contracts/  # Test interface contracts
    └── BlazorBaseUI.Playwright.Tests/ # E2E tests (Playwright)
```

### Tool Preferences

- Use Serena for searching files; if it does not work, use default searching
- Use context7 to check for documentation and best practices
- Use `/dev/null` in Git Bash, not `nul`
- Put all `tmpclaude-*-cwd` files in `/.claude/tmp-files`
- **`gh api` on Windows**: Omit the leading `/` from endpoint paths. Windows shells rewrite `/repos/...` as filesystem paths (e.g., `F:/Git/repos/...`). Use `gh api repos/owner/repo/...` instead of `gh api /repos/owner/repo/...`

---

## Build and Test Commands

### Build

```bash
dotnet build BlazorBaseUI.slnx                    # Build entire solution
dotnet build src/BlazorBaseUI/BlazorBaseUI.csproj # Build specific project
dotnet build BlazorBaseUI.slnx -c Release         # Build in release mode
```

### Run Demo

```bash
dotnet run --project demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.csproj
```

### Unit Tests (bUnit)

```bash
dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj
dotnet test --filter "FullyQualifiedName~CheckboxRootTests"  # Specific class
dotnet test --filter "TestName"                               # Specific test
dotnet test -v detailed                                       # Verbose output
```

### E2E Tests (Playwright)

```bash
dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj
dotnet test --filter "FullyQualifiedName~CollapsibleServerTests"  # Specific class
```

See [Testing Instructions](#testing-instructions) for debugging, traces, and advanced configuration.

---

## Code Style Guidelines

### 1. Pre-Generation Validation Rule (Mandatory)

1. Check relevant files in `/.base-ui`
2. List all relevant components found
3. Create an implementation plan (Outline files, structure, and approach)

### 2. Code Ordering (Strict)

Generate members **only** in the exact order below. **Do not generate partition comments** such as `// === Lifecycle Methods ===`.

1. Constants (PascalCase)
2. Fields (no underscore prefix: read-only, private, backing)
3. Private properties
4. Parameter properties
5. Public properties
6. Internal properties
7. Lifecycle methods
8. Dispose method (only if needed)
9. Public methods
10. Internal methods
11. Private methods

No reordering is allowed.

### 3. Fidelity to Source

- Replicate **structure and behavior exactly**
- **Do not add business logic**
- Do not simplify or "improve" behavior unless explicitly requested
- Do not create Class or Style parameter property unless needed (like passing context/state). Use the `Func<TState, string>?` types for these, and name these as `ClassValue` and `StyleValue` accordingly.
- Use the shared `RenderElement<TState>` component to simulate `useRender`. Components expose a `Render` parameter of type `RenderFragment<RenderProps<TState>>?` and pass it to `<RenderElement>` along with a `Tag` (default HTML element), `State`, `ClassValue`, `StyleValue`, `ComponentAttributes`, and `ChildContent`.

#### Attribute Handling

- Follow attribute ordering from the source as closely as possible
- aria attributes with boolean values should be converted to string `isTrue ? "true" : "false"`
- Do not use `""` as value for standalone attributes `builder.AddAttribute(1, "data-disabled")` not `builder.AddAttribute(1, "data-disabled", "")`
- Check `./AttributeUtilities.cs` for attribute helpers

### 4. JavaScript Interop Rules

#### Imports

- Use `Lazy<Task<IJSObjectReference>>` for JS module imports

#### Script States

Use Symbol when creating states in the JavaScript file **when needed**:

```javascript
const STATE_KEY = Symbol.for('BlazorBaseUI.SomeComponent.State');
if (!window[STATE_KEY]) {
  window[STATE_KEY] = { count: 0 };
}
const state = window[STATE_KEY];
```

#### Script Placement

- **Never** recommend adding scripts to `index.html` unless absolutely necessary

#### Exception Handling (Circuit-Safe Guard)

Wrap **all** JS interop calls in the following pattern:

```csharp
try
{
    // JS call (e.g., await module.InvokeVoidAsync("someMethod", element);)
}
catch (Exception ex) when (
    ex is JSDisconnectedException or
    TaskCanceledException)
{
}
```

#### Responsibility Split (Strict UI Logic)

Move heavy or complex behavior to JavaScript. The following **must** be handled in JavaScript:

**High-Frequency Events:**
- Dragging and Resizing (any logic involving `mousemove` or `touchmove`)
- Scroll-Linked Effects (parallax, scroll-driven animations, "sticky" header logic)

**Native Browser APIs:**
- Observers (`IntersectionObserver`, `MutationObserver`)
- Hardware/Media (Geolocation, Web Bluetooth, Web USB, high-performance `<canvas>` or WebGL)

**Complex Interaction Logic:**
- Event Suppression (`preventDefault`, `stopPropagation`)
- Focus Management (focus trapping, caret/selection position)

### 5. Logging

- Use `ILogger`
- If recommending alternatives, explain **before** code generation

### 6. Element Reference Capture

Capture element references by adding `@ref` to `<RenderElement>` and delegating via a public `Element` property:

```razor
<RenderElement TState="MyComponentState"
               Tag="div"
               State="state"
               Enabled="true"
               Render="Render"
               ClassValue="ClassValue"
               StyleValue="StyleValue"
               ComponentAttributes="componentAttributes"
               ChildContent="ChildContent"
               @attributes="AdditionalAttributes"
               @ref="renderElementReference" />

@code {
    private RenderElement<MyComponentState>? renderElementReference;

    /// <summary>
    /// Gets or sets the associated <see cref="ElementReference"/>.
    /// </summary>
    public ElementReference? Element => renderElementReference?.Element;
}
```

`RenderElement.Element` is `ElementReference?` (nullable). The public `Element` property delegates through `renderElementReference?.Element`, returning `null` before first render.

### 7. Cascading Parameters Rules

- **Always** create a context sealed class
- Never cascade the parent component directly
- Cascading parameters are **always private**

### 8. Code Placement and Rendering Rules

- Never generate partial classes or code-behind files (`.razor.cs`)
- All components use `.razor` files with `<RenderElement>` in Razor markup and logic inside `@code { }`
- The `.cs` stub file contains only the namespace, XML doc comment, and `public partial class ComponentName;` declaration

### 9. RenderTreeBuilder Sequencing (Strict)

See `.blazor-docs/advanced-scenarios.md` for detailed guidance on sequence numbers.

Key rules:
- Use **hardcoded integer literals** for sequence numbers (no variables or expressions)
- Sequence numbers must increase in source code order, not runtime order
- For complex `BuildRenderTree`, use `OpenRegion`/`CloseRegion` for isolated sequence spaces

### 10. Component files guide

1. If there are enums specific to the component, create an `Enumerations.cs`
2. Interface and implementation in a single file
3. Extension method should be in `./Extensions.cs`, if component-scoped only, create its own.

### 11. Async Lifecycle and Exception Handling

- **Non-Blocking:** Never `await` in `OnParametersSetAsync` for non-critical work. Use `_ = SyncMethod();`
- **JS Timing:** JS interop cannot be called before the first render. Use `hasRendered` guard if applicable.

#### Async Event Handlers

**Always return `Task` or `ValueTask`** from async event handlers. Never use `async void`:

```csharp
// WRONG - exceptions are lost
private async void HandleClick() { ... }

// CORRECT
private async Task HandleClick() { ... }
```

#### Thread-Blocking Methods (Never Use)

See `.blazor-docs/components/synchronization-context.md` for the full list of thread-blocking methods to avoid.

#### Component Re-entrancy

Components are re-entrant at any `await` point:
- Ensure the component is in a **valid state for rendering** before any `await`
- `OnParametersSetAsync` or disposal may occur while `OnInitializedAsync` is still running
- Use guards or null checks for state that may not yet be initialized

#### Fire-and-Forget Exception Handling

Use `DispatchExceptionAsync` to surface exceptions to error boundaries:

```csharp
private void StartBackgroundWork()
{
  _ = Task.Run(async () =>
  {
    try
    {
      await SomeLongRunningTask();
    }
    catch (Exception ex)
    {
      await DispatchExceptionAsync(ex);
    }
  });
}
```

### 12. Dispose Method Implementation

- **Only** include `GC.SuppressFinalize(this)` if the class has a destructor
- Clean up JS interop objects and event handlers

#### CancellationToken for Background Work

```csharp
private CancellationTokenSource? cts;

protected override void OnInitialized()
{
  cts = new CancellationTokenSource();
  _ = DoBackgroundWorkAsync(cts.Token);
}

public void Dispose()
{
  cts?.Cancel();
  cts?.Dispose();
}
```

### 13. ElementReference and Module Guard Checks

#### ElementReference Guard (RenderElement pattern)

Guard on `Element.HasValue` before JS interop calls. `RenderElement.Element` is `ElementReference?`, and the component's public `Element` property delegates through `renderElementReference?.Element`:

```csharp
if (Element.HasValue)
{
  await module.InvokeVoidAsync("sync", Element.Value, ...);
}
```

#### Lazy Module Guard in DisposeAsync

```csharp
if (moduleTask.IsValueCreated && Element.HasValue)
{
  try
  {
    var module = await moduleTask.Value;
    await module.InvokeVoidAsync("dispose", Element.Value);
    await module.DisposeAsync();
  }
  catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
  {
  }
}
```

### 14. Event Handler Override Prevention

When a component uses `builder.AddMultipleAttributes()` followed by `builder.AddAttribute("onclick", internalHandler)`, the internal handler **overrides** any user-defined handler.

**Solution:** Use `EventUtilities` helper methods, calling user callback **after** internal logic:

```csharp
private async Task HandleClickAsync(MouseEventArgs e)
{
    await Context.SetOpenAsync(true, reason);  // Internal logic first
    await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);  // User handler after
}
```

**When to Apply:**

| Component Type | Action |
|----------------|--------|
| Triggers (Tooltip, Dialog, Popover) | Use `EventUtilities` helper methods as needed |
| Input components | Add focus/blur/paste handlers. **Do NOT** forward `oninput`/`onkeydown` |

---

## Commit and PR Style

- Do NOT add "Generated with Claude Code" or co-author footers to commits or PRs
- Keep commit messages concise and descriptive
- PR descriptions should focus on what changed and why
- Do NOT mark PRs as "ready for review" (`gh pr ready`) - leave PRs in draft mode and let the user decide when to mark them ready

### PR Review Comment Workflow

When addressing PR review comments, follow this process:

1. **Fetch comments** — Use `gh api repos/{owner}/{repo}/pulls/{number}/comments` with `--jq` to extract comment IDs, paths, lines, user, and body summary
2. **Validate each concern** — Cross-reference against:
   - The base-ui source in `.base-ui/` for behavioral fidelity
   - The AGENTS.md coding guidelines for pattern compliance
   - Existing reference implementations (e.g., Switch) for consistency
3. **Classify validity** — For each comment, determine: Valid, Partially Valid, or Invalid (with reasoning)
4. **Fix valid issues** — Apply changes, then build the full solution (`dotnet build BlazorBaseUI.slnx`) to verify 0 errors
5. **Check for same bug elsewhere** — If a bug is found in one component, grep for the same pattern in sibling components and fix those too
6. **Commit and push** — Single commit covering all fixes with a descriptive message
7. **Reply to each comment** — Use `gh api repos/{owner}/{repo}/pulls/{number}/comments/{comment_id}/replies -f body="..."` to reply inline, referencing the commit SHA and describing what was fixed

---

## Testing Instructions

### Test Configuration

Tests run in parallel by default (configured in `xunit.runner.json`):
```json
{
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4
}
```

### Unit Test Structure

Tests use bUnit with xUnit v3. Test classes inherit from `BunitContext` and implement a contract interface:

```csharp
namespace BlazorBaseUI.Tests.Checkbox;

public class CheckboxRootTests : BunitContext, ICheckboxRootContract
{
    public CheckboxRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupCheckboxModule(JSInterop);
    }

    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateCheckboxRoot());
        var checkbox = cut.Find("[role='checkbox']");
        checkbox.TagName.ShouldBe("SPAN");
        return Task.CompletedTask;
    }
}
```

### Test Contracts

Contract interfaces define the expected test methods for each component, ensuring consistent coverage:

```csharp
public interface ICollapsibleRootContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    // ... more test definitions
}
```

### JS Interop Setup

Configure JS module mocks using `JsInteropSetup`:

```csharp
public static void SetupCheckboxModule(BunitJSInterop jsInterop)
{
    var module = jsInterop.SetupModule(CheckboxModule);
    module.SetupVoid("initialize", _ => true);
    module.SetupVoid("dispose", _ => true);
    module.SetupVoid("updateState", _ => true);
}
```

### Playwright Test Structure

E2E tests use Playwright with xUnit for browser-based testing:

**Infrastructure:**
- `TestBase.cs` - Base class with navigation, timeouts, and helpers
- `TestRenderMode.cs` - Enum for Server/WASM modes
- `TestPageUrlBuilder.cs` - Fluent URL builder for test pages

**Fixtures:**
- `PlaywrightFixture.cs` - Browser instance management
- `BlazorTestFixture.cs` - Blazor server management

### WASM Timeout Scaling

WASM tests automatically use 3x timeout multiplier to account for:
- WebAssembly runtime download
- .NET assembly initialization
- JS module lazy loading
- Higher interop latency

### Debugging Playwright Tests

```bash
# PowerShell - Enable headed mode with debug pause points
$env:PLAYWRIGHT_DEBUG="1"
$env:PLAYWRIGHT_HEADLESS="false"
dotnet test --filter "TestName"

# Bash
PLAYWRIGHT_DEBUG=1 PLAYWRIGHT_HEADLESS=false dotnet test --filter "TestName"
```

Add `await DebugPauseAsync();` in tests where you want to pause for inspection.

### Viewing Traces

All tests generate trace files for post-mortem debugging:

```bash
npx playwright show-trace tests/BlazorBaseUI.Playwright.Tests/traces/TestName_timestamp.zip
```

Traces include screenshots, DOM snapshots, network requests, and console logs.

### Test Generation (Codegen)

Generate test code by recording browser interactions:

```bash
pwsh bin/Debug/net10.0/playwright.ps1 codegen http://localhost:5000
```

### Cross-Browser Testing

```bash
# PowerShell
$env:PLAYWRIGHT_BROWSER="firefox"  # or "webkit", "chromium"
dotnet test

# Install additional browsers
pwsh bin/Debug/net10.0/playwright.ps1 install firefox
pwsh bin/Debug/net10.0/playwright.ps1 install webkit
```

### Environment Variables

| Variable | Values | Default | Description |
|----------|--------|---------|-------------|
| `PLAYWRIGHT_BROWSER` | chromium, firefox, webkit | chromium | Browser engine |
| `PLAYWRIGHT_HEADLESS` | true, false | true | Headless mode |
| `PLAYWRIGHT_DEBUG` | 0, 1 | 0 | Enable debug pause |

### Assertion Library

Use **Shouldly** for assertions:

```csharp
checkbox.TagName.ShouldBe("SPAN");
checkbox.GetAttribute("aria-checked").ShouldBe("true");
checkbox.HasAttribute("data-disabled").ShouldBeTrue();
invoked.ShouldBeFalse();
```

<!-- BLAZOR-AGENTS-MD-START -->[Blazor Docs Index]|root: ./.blazor-docs|IMPORTANT: Prefer retrieval-led reasoning over pre-training-led reasoning for any Blazor tasks.|If docs missing run: context7.|01-root:{advanced-scenarios.md,blazor-ef-core.md,blazor-with-dotnet-on-web-workers.md,call-web-api.md,debug.md,file-downloads.md,file-uploads.md,globalization-localization.md,hosting-models.md,images-and-documents.md,index.md,project-structure.md,supported-platforms.md,test.md,tooling.md,webassembly-build-tools-and-aot.md,webassembly-lazy-load-assemblies.md,webassembly-native-dependencies.md}|02-components:{built-in-components.md,cascading-values-and-parameters.md,class-libraries-and-static-server-side-rendering.md,class-libraries.md,component-disposal.md,control-head-content.md,css-isolation.md,data-binding.md,dynamiccomponent.md,element-component-model-relationships.md,event-handling.md,generic-type-support.md,httpcontext.md,index.md,integration-hosted-webassembly.md,integration.md,js-spa-frameworks.md,layouts.md,lifecycle.md,overwriting-parameters.md,prerender.md,quickgrid.md,render-components-outside-of-aspnetcore.md,render-modes.md,rendering.md,sections.md,splat-attributes-and-arbitrary-parameters.md,synchronization-context.md,templated-components.md,virtualization.md}|03-fundamentals:{configuration.md,dependency-injection.md,environments.md,handle-errors.md,index.md,logging.md,navigation.md,routing.md,signalr.md,startup.md,static-files.md}|04-forms:{binding.md,index.md,input-components.md,troubleshoot.md,validation.md}|05-javascript-interoperability:{call-dotnet-from-javascript.md,call-javascript-from-dotnet.md,import-export-interop.md,index.md,location-of-javascript.md,static-server-rendering.md}|06-host-and-deploy:{app-base-path.md,configure-linker.md,configure-trimmer.md,index.md,server/index.md,server/memory-management.md,webassembly/apache.md,webassembly/azure-static-web-apps.md,webassembly/azure-storage.md,webassembly/bundle-caching-and-integrity-check-failures.md,webassembly/deployment-layout.md,webassembly/github-pages.md,webassembly/http-caching-issues.md,webassembly/iis.md,webassembly/index.md,webassembly/multiple-hosted-webassembly.md,webassembly/nginx.md}|07-security:{account-confirmation-and-password-recovery.md,additional-scenarios.md,authentication-state.md,blazor-web-app-with-entra.md,blazor-web-app-with-oidc.md,blazor-web-app-with-windows-authentication.md,content-security-policy.md,gdpr.md,index.md,interactive-server-side-rendering.md,qrcodes-for-authenticator-apps.md,static-server-side-rendering.md,includes/additional-scopes-standalone-MEID.md,includes/additional-scopes-standalone-nonMEID.md,includes/app-component.md,includes/authentication-component.md,includes/authorize-client-app.md,includes/fetchdata-component.md,includes/hosted-blazor-webassembly-notice.md,includes/index-page-authentication.md,includes/index-page-msal.md,includes/logindisplay-component.md,includes/msal-login-mode.md,includes/redirecttologin-component.md,includes/run-the-app.md,includes/secure-authentication-flows.md,includes/shared-state.md,includes/troubleshoot-server.md,includes/troubleshoot-wasm.md,includes/usermanager-signinmanager.md,includes/wasm-aad-b2c-custom-policies.md,webassembly/additional-scenarios.md,webassembly/graph-api.md,webassembly/hosted-with-azure-active-directory-b2c.md,webassembly/hosted-with-identity-server.md,webassembly/hosted-with-microsoft-entra-id.md,webassembly/index.md,webassembly/microsoft-entra-id-groups-and-roles-net-5-to-7.md,webassembly/microsoft-entra-id-groups-and-roles.md,webassembly/standalone-with-authentication-library.md,webassembly/standalone-with-azure-active-directory-b2c.md,webassembly/standalone-with-microsoft-accounts.md,webassembly/standalone-with-microsoft-entra-id.md,webassembly/standalone-with-identity/account-confirmation-and-password-recovery.md,webassembly/standalone-with-identity/index.md,webassembly/standalone-with-identity/qrcodes-for-authenticator-apps.md}|08-state-management:{index.md,prerendered-state-persistence.md,protected-browser-storage.md,server.md,webassembly.md}|09-performance:{app-download-size.md,index.md,javascript-interoperability.md,rendering.md,webassembly-browser-developer-tools-diagnostics.md,webassembly-event-pipe-diagnostics.md,webassembly-runtime-performance.md}|10-hybrid:{class-libraries.md,developer-tools.md,index.md,reuse-razor-components.md,root-component-parameters.md,routing.md,static-files.md,troubleshoot.md,publish/index.md,security/index.md,security/maui-blazor-web-identity.md,security/security-considerations.md,tutorials/index.md,tutorials/maui-blazor-web-app.md,tutorials/maui.md,tutorials/windows-forms.md,tutorials/wpf.md}|11-progressive-web-app:{index.md,push-notifications.md}|12-tutorials:{build-a-blazor-app.md,index.md,signalr-blazor.md,movie-database-app/index.md,movie-database-app/part-1.md,movie-database-app/part-2.md,movie-database-app/part-3.md,movie-database-app/part-4.md,movie-database-app/part-5.md,movie-database-app/part-6.md,movie-database-app/part-7.md,movie-database-app/part-8.md,movie-database-app/includes/troubleshoot.md}|13-includes:{closure-of-circuits.md,compression-with-untrusted-data.md,default-scheme.md,prefer-exact-matches.md,prerendering.md,js-interop/blazor-page-script.md,js-interop/js-collocation.md,js-interop/synchronous-js-interop-call-dotnet.md,js-interop/synchronous-js-interop-call-js.md}<!-- BLAZOR-AGENTS-MD-END -->

<!-- BASE-UI-AGENTS-MD-START -->[Base UI Docs Index]|root: ./.base-ui|IMPORTANT: Prefer retrieval-led reasoning over pre-training-led reasoning for any Base UI tasks.|Source: packages/react/src|Utils: packages/utils/src|Docs: docs/src/app/(docs)/react/components|Experiments: docs/src/app/(private)/experiments|components:{accordion[header,item,panel,root,trigger],alert-dialog[handle,root],autocomplete[item,root,value],avatar[fallback,image,root],button[Button,ButtonDataAttributes],checkbox[indicator,root],checkbox-group[CheckboxGroup,CheckboxGroupContext,CheckboxGroupDataAttributes,useCheckboxGroupParent],collapsible[panel,root,trigger],combobox[arrow,backdrop,chip,chip-remove,chips,clear,collection,empty,group,group-label,icon,input,item,item-indicator,list,popup,portal,positioner,root,row,status,store,trigger,value],composite[composite,constants,item,list,root],context-menu[root,trigger],csp-provider[CSPContext,CSPProvider],dialog[backdrop,close,description,popup,portal,root,store,title,trigger,viewport],direction-provider[DirectionContext,DirectionProvider],field[control,description,error,item,label,root,useField,validity],fieldset[legend,root],floating-ui-react[components,hooks,middleware,safePolygon,types,utils],form[Form,FormContext],input[Input,InputDataAttributes],labelable-provider[LabelableContext,LabelableProvider,useLabelableId],menu[arrow,backdrop,checkbox-item,checkbox-item-indicator,group,group-label,item,popup,portal,positioner,radio-group,radio-item,radio-item-indicator,root,store,submenu-root,submenu-trigger,trigger],menubar[Menubar,MenubarContext,MenubarDataAttributes],merge-props[mergeProps],meter[indicator,label,root,track,value],navigation-menu[arrow,backdrop,content,icon,item,link,list,popup,portal,positioner,root,trigger,viewport],number-field[decrement,group,increment,input,root,scrub-area,scrub-area-cursor],popover[arrow,backdrop,close,description,popup,portal,positioner,root,store,title,trigger,viewport],preview-card[arrow,backdrop,popup,portal,positioner,root,store,trigger,viewport],progress[indicator,label,root,track,value],radio[indicator,root],radio-group[RadioGroup,RadioGroupContext,RadioGroupDataAttributes],scroll-area[constants,content,corner,root,scrollbar,thumb,viewport],select[arrow,backdrop,group,group-label,icon,item,item-indicator,item-text,list,popup,portal,positioner,root,scroll-arrow,scroll-down-arrow,scroll-up-arrow,store,trigger,value],separator[Separator],slider[control,indicator,root,thumb,track,value],switch[root,stateAttributesMapping,thumb],tabs[indicator,list,panel,root,tab],toast[action,arrow,close,content,createToastManager,description,portal,positioner,provider,root,title,useToastManager,viewport],toggle[Toggle,ToggleDataAttributes],toggle-group[ToggleGroup,ToggleGroupContext,ToggleGroupDataAttributes],toolbar[button,group,input,link,root,separator],tooltip[arrow,popup,portal,positioner,provider,root,store,trigger,viewport],unstable-use-media-query,use-button[useButton],use-render[useRender]}|utils:{detectBrowser,empty,error,fastHooks,fastObjectShallowCompare,formatErrorMessage,generateId,getReactElementRef,inertValue,isElementDisabled,isMouseWithinBounds,mergeObjects,owner,reactVersion,safeReact,testUtils,useAnimationFrame,useControlled,useEnhancedClickHandler,useForcedRerendering,useId,useInterval,useIsoLayoutEffect,useMergedRefs,useOnFirstRender,useOnMount,usePreviousValue,useRefWithInit,useScrollLock,useStableCallback,useTimeout,useValueAsRef,visuallyHidden,warn}<!-- BASE-UI-AGENTS-MD-END -->