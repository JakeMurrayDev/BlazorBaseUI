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
        throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
    }
    builder.OpenComponent(0, RenderAs!);
    // ... attributes ...
    builder.AddComponentReferenceCapture(14, component => { Element = ((IReferencableComponent)component).Element; });
    builder.CloseComponent();
}
else
{
    builder.OpenElement(15, !string.IsNullOrEmpty(As) ? As : DefaultTag);
    // ... attributes ...
    builder.AddElementReferenceCapture(16, elementReference => Element = elementReference);
    builder.CloseElement();
}
```

---

## 6. Cascading Parameters Rules

* **Always** create a context class (record type).
* Never cascade the parent component directly.
* Cascading parameters are **always private**.

---

## 7. Code Placement and Rendering Rules

* **All code must be inside `@code { }`**.
* Never generate partial classes or code-behind files (`.razor.cs`).

### Render Mode and Sequencing Rule

If the component contains properties named **`As`** or **`RenderAs`**:

* Use **code-based rendering** (`BuildRenderTree`).
* Do **not** use Razor markup and do **not** do it in a razor file.
* **Strict Linear Sequencing:** Every call to a `RenderTreeBuilder` method (`OpenElement`, `AddAttribute`, etc.) must use a hardcoded integer literal sequence number. 
* **Incremental Order:** Sequence numbers must start at `0` and increment by `1` for every subsequent instruction in the source code.
* **Branching Continuity:** Do **not** reuse sequence numbers across `if/else` or `switch` branches. Indices must continue to climb linearly based on their vertical position in the code to ensure the diffing engine identifies every distinct line as unique.
* **No Variables:** Never use counter variables or expressions (e.g., `i++`) for sequence numbers.

---

## 8. Pre-Generation Validation Rule (Mandatory)

Before generating any code:

1. Check relevant files (e.g., `index.parts.ts`).
2. List all relevant components found.
3. **Confirm with the user first**. Do **not** generate code yet.

---

## 9. Plan Creation Rule

After receiving answers:

* **Create a detailed plan first**.
* Outline files, structure, and approach.
* Present for confirmation.

---

## 10. Folder Structure and Files

1. Enums in `Enumerations.cs`.
2. Interface and implementation in a single file.
3. Include internal `Extensions` for `ToDataAttributeString()` mapping.

---

## 11. Async Lifecycle and Exception Handling

* **Non-Blocking:** Never `await` in `OnParametersSetAsync` for non-critical work. Use `_ = SyncMethod();`.
* **Logged Fire-and-Forget:** Cache callbacks in `OnInitialized` to avoid allocations.
* **JS Timing:** JS interop cannot be called before the first render. Use `hasRendered` guard.

---

## 12. Dispose Method Implementation

* **Only** include `GC.SuppressFinalize(this)` if the class has a destructor (`~ClassName()`).
* Clean up JS interop objects and event handlers.

---

## 13. ElementReference and Module Guard Checks

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
    catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException) { }
}
```

---

## 14. Absolute Compliance

* All rules must be followed **without exception**.
* No emojis, no creative deviations, no undocumented changes.
* Uses .NET 10 version.