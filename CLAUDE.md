# Blazor Coding Guidelines (Strict Agent Instructions)

These rules **must be followed without exception** when generating Blazor code.

Failure to comply with any rule is considered an incorrect output.

---

## 1. Code Ordering (Strict)

Generate members **only** in the exact order below.

**Do not generate partition comments** such as `// === Lifecycle Methods ===`.

1. Constants (PascalCase)
2. Read-only fields (no underscore prefix)
3. Private fields (no underscore prefix)
4. Backing fields (no underscore prefix)
5. Private properties
6. Parameter properties
7. Public properties
8. Internal properties
9. Lifecycle methods
10. Dispose method (only if needed)
11. Public methods
12. Internal methods
13. Private methods

No reordering is allowed.

---

## 2. Fidelity to Source

When a source implementation is provided (e.g., React, Radix, existing TS/JS code):

* Replicate **structure and behavior exactly**
* **Do not add business logic**
* Follow attribute ordering from the source as closely as possible
* Do not simplify or "improve" behavior unless explicitly requested
* Do not create Class or Style parameter property unless needed (like passing context/state). Use the `Func<TState, string>?` types for these, and name these as `ClassValue` and `StyleValue` accordingly.
* Use `As (string?)` and `RenderAs(Type?)` to simulate `useRender`. Have a `DefaultTag` constant.
* aria attributes with boolean values should be converted to string

---

## 3. JavaScript Interop Rules

### Imports

* Use `Lazy<Task<IJSObjectReference>>` for JS module imports

### Script States

* Use Symbol when creating states in the JavaScript file **when needed**.

```javascript
const STATE_KEY = Symbol.for('BlazorBaseUI.SomeComponent.State');
if (!window[STATE_KEY]) {
  window[STATE_KEY] = { count: 0 };
}
const state = window[STATE_KEY];
```

### Script Placement

* **Never** recommend adding scripts to `index.html` unless absolutely necessary

### Exception Handling (Circuit-Safe Guard)

Wrap **all** JS interop calls in the following pattern to avoid circuit crashes during Hot Reload or disconnection:

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

### Responsibility Split (Strict UI Logic)

Move heavy or complex behavior to JavaScript to avoid interop latency and UI stuttering. The following **must** be handled in JavaScript:

1. **High-Frequency Events:**
* **Dragging and Resizing:** Any logic involving `mousemove` or `touchmove` for repositioning elements.
* **Scroll-Linked Effects:** Parallax, scroll-driven animations, or "sticky" header logic.

2. **Native Browser APIs:**
* **Observers:** `IntersectionObserver` and `MutationObserver`.
* **Hardware/Media:** Geolocation, Web Bluetooth, Web USB, and high-performance `<canvas>` or WebGL rendering.

3. **Complex Interaction Logic:**
* **Event Suppression:** Logic involving `preventDefault` or `stopPropagation`.
* **Focus Management:** Focus trapping within modals and managing caret/selection position.

---

## 4. Logging

* Use `ILogger`
* If recommending alternatives, explain **before** code generation.

---

## 5. Element Reference Capture (The Reference Rule)

Always include a public `Element` property after all parameter properties to support external refs:

```csharp
public ElementReference? Element { get; private set; }
```

### Capture Logic in `BuildRenderTree`

When using `As` and `RenderAs`, you must capture the reference based on whether a component or element is rendered. Use the `IReferencableComponent` interface for component captures:

```csharp
if (isComponent)
{
  if (!typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
  {
    throw new InvalidOperationException(
      $"Type {RenderAs!.Name} must implement IReferencableComponent."
    );
  }
  builder.OpenRegion(0);
  builder.OpenComponent(0, RenderAs!);
  // ... attributes (1, 2, 3, etc.) ...
  builder.AddComponentReferenceCapture(
    4,
    component =>
    {
      Element = ((IReferencableComponent)component).Element;
    }
  );
  builder.CloseComponent();
  builder.CloseRegion();
}
else
{
  builder.OpenRegion(1);
  builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
  // ... attributes (1, 2, 3, etc.) ...
  builder.AddElementReferenceCapture(
    4,
    elementReference => (Element = elementReference)
  );
  builder.CloseElement();
  builder.CloseRegion();
}
```

---

## 6. Cascading Parameters Rules

* **Always** create a context class (record type).
* Never cascade the parent component directly.
* Cascading parameters are **always private**.

---

## 7. Code Placement and Rendering Rules

* Never generate partial classes or code-behind files (`.razor.cs`).

### Render Mode Rule

If the component contains properties named **`As`** or **`RenderAs`**:

* Use **code-based rendering** (`BuildRenderTree`) in a pure `.cs` file.
* Do **not** use Razor markup (`.razor` files).

For simple components **without** `As`/`RenderAs` properties:

* Use `.razor` files with all code inside `@code { }`.

---

## 8. RenderTreeBuilder Sequencing (Strict)

When using `BuildRenderTree`, the following sequencing rules apply without exception:

* **Hardcoded Literals:** Every call to a `RenderTreeBuilder` method (`OpenElement`, `AddAttribute`, etc.) must use a hardcoded integer literal sequence number.
* **Strict Linear Sequencing:** Sequence numbers must start at `0` and increment by `1` for every subsequent instruction in the source code.
* **Branching Continuity:** Do **not** reuse sequence numbers across `if/else` or `switch` branches. Indices must continue to climb linearly based on their vertical position in the code to ensure the diffing engine identifies every distinct line as unique.
* **Loop Handling:** Within a loop (e.g., foreach), the sequence numbers must remain constant for each instruction within the loop body. The fact that numbers repeat at runtime informs the diffing system that it is processing a loop.
* **No Variables:** Never use counter variables or expressions (e.g., `i++`) for sequence numbers.
* For long or complex `BuildRenderTree` implementations, use `OpenRegion`/`CloseRegion` to create isolated sequence number spaces.

```csharp
protected override void BuildRenderTree(RenderTreeBuilder builder)
{
  builder.OpenRegion(0);
  // Sequence numbers restart at 0 within this region
  builder.OpenElement(0, "div");
  builder.AddAttribute(1, "class", "header");
  builder.CloseElement();
  builder.CloseRegion();

  builder.OpenRegion(1);
  // Another isolated sequence space
  builder.OpenElement(0, "div");
  builder.AddAttribute(1, "class", "content");
  builder.CloseElement();
  builder.CloseRegion();
}

## 9. Pre-Generation Validation Rule (Mandatory)

Before generating any code:

1. Check relevant files (e.g., `index.parts.ts`).
2. List all relevant components found.
3. **Confirm with the user first**. Do **not** generate code yet.

---

## 10. Plan Creation Rule

After receiving answers:

* **Create a detailed plan first**.
* Outline files, structure, and approach.
* Present for confirmation.

---

## 11. Folder Structure and Files

1. Enums in `Enumerations.cs`.
2. Interface and implementation in a single file.
3. If applicable, include internal `Extensions.cs` for `ToDataAttributeString()` mapping.

---

## 12. Async Lifecycle and Exception Handling

* **Non-Blocking:** Never `await` in `OnParametersSetAsync` for non-critical work. Use `_ = SyncMethod();`.
* **Logged Fire-and-Forget:** Cache callbacks in `OnInitialized` to avoid allocations.
* **JS Timing:** JS interop cannot be called before the first render. Use `hasRendered` guard if applicable.

### Async Event Handlers

* **Always return `Task` or `ValueTask`** from async event handlers. Never use `async void`.
* Blazor does not track void-returning async methods; exceptions will be lost silently.

```csharp
// WRONG - exceptions are lost
private async void HandleClick() { ... }

// CORRECT
private async Task HandleClick() { ... }
```

### Thread-Blocking Methods (Never Use)

The following methods block the execution thread and must **never** be used in components:

* `Task.Result`
* `Task.Wait()`
* `Task.WaitAny()`
* `Task.WaitAll()`
* `Thread.Sleep()`
* `TaskAwaiter.GetResult()`

### Component Re-entrancy

Components are re-entrant at any `await` point. Lifecycle methods may be called before an async operation completes:

* Ensure the component is in a **valid state for rendering** before any `await`
* `OnParametersSetAsync` or disposal may occur while `OnInitializedAsync` is still running
* Use guards or null checks for state that may not yet be initialized

### Fire-and-Forget Exception Handling

For fire-and-forget operations, use `DispatchExceptionAsync` to surface exceptions to error boundaries:

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

---

## 13. Dispose Method Implementation

* **Only** include `GC.SuppressFinalize(this)` if the class has a destructor (`~ClassName()`).
* Clean up JS interop objects and event handlers.

### CancellationToken for Background Work

For components with background operations (timers, polling, long-running tasks), use `CancellationTokenSource`:

```csharp
private CancellationTokenSource? cts;

protected override void OnInitialized()
{
  cts = new CancellationTokenSource();
  _ = DoBackgroundWorkAsync(cts.Token);
}

private async Task DoBackgroundWorkAsync(CancellationToken cancellationToken)
{
  while (!cancellationToken.IsCancellationRequested)
  {
    await Task.Delay(1000, cancellationToken);
    // ... work ...
  }
}

public void Dispose()
{
  cts?.Cancel();
  cts?.Dispose();
}
```

---

## 14. ElementReference and Module Guard Checks

### ElementReference Guard

Always check `Element.HasValue` before using `Element.Value` in JS interop calls:

```csharp
if (Element.HasValue)
{
  await module.InvokeVoidAsync("sync", Element.Value, ...);
}
```

### Lazy Module Guard in DisposeAsync

Check `moduleTask.IsValueCreated` and `Element.HasValue` before awaiting in `DisposeAsync`:

```csharp
if (moduleTask.IsValueCreated && Element.HasValue)
{
  try
  {
    var module = await moduleTask.Value;
    await module.InvokeVoidAsync("sync", Element.Value, ..., true); // dispose: true
    await module.DisposeAsync();
  }
  catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
  )
  {
  }
}
```

---

## 15. Absolute Compliance

* All rules must be followed **without exception**.
* No emojis, no creative deviations, no undocumented changes.
* Uses .NET 10 version.