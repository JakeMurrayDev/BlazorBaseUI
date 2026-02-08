# Refactoring Guide: Replacing As/RenderAs with Render + RenderElement

This document captures every change made during the Avatar prototype refactoring and serves as the reference for applying the same pattern to all other components.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Problems with the Old Approach](#2-problems-with-the-old-approach)
3. [New Architecture](#3-new-architecture)
4. [Shared Infrastructure Files](#4-shared-infrastructure-files)
5. [Step-by-Step Refactoring Checklist](#5-step-by-step-refactoring-checklist)
6. [Before/After Comparison: AvatarRoot](#6-beforeafter-comparison-avatarroot)
7. [Before/After Comparison: AvatarImage](#7-beforeafter-comparison-avatarimage)
8. [Before/After Comparison: AvatarFallback](#8-beforeafter-comparison-avatarfallback)
9. [Context Class Pattern](#9-context-class-pattern)
10. [CascadingValue IsFixed Rule](#10-cascadingvalue-isfixed-rule)
11. [Attribute Merging Strategy](#11-attribute-merging-strategy)
12. [Conditional Rendering via Enabled](#12-conditional-rendering-via-enabled)
13. [Render Function Usage for Consumers](#13-render-function-usage-for-consumers)
14. [React children vs Blazor ChildContent](#14-react-children-vs-blazor-childcontent)
15. [Common Pitfalls and Fixes](#15-common-pitfalls-and-fixes)
16. [File Inventory](#16-file-inventory)

---

## 1. Overview

The refactoring replaces the dual `As` (string tag) / `RenderAs` (Type) rendering parameters with a single `Render` parameter backed by a shared `RenderElement<TState>` component. This:

- Eliminates duplicated render branches across ~97 components (~170 `AddMultipleAttributes` calls)
- Converts manual `BuildRenderTree` `.cs` files to `.razor` files
- Fixes the double class/style emission bug
- Aligns with Base UI's `render={(props, state) => ...}` prop pattern
- Centralizes attribute merging in one place

**Base UI equivalent**: `useRenderElement` hook in `packages/react/src/utils/useRenderElement.tsx`

---

## 2. Problems with the Old Approach

### 2.1 Duplicated Render Branches
Every component had identical `if (isComponentRenderAs) { ... } else { ... }` blocks in `BuildRenderTree`. Example from `AvatarFallback.cs` (lines 136-181):

```csharp
// OLD: 45 lines of duplicated branching in EVERY component
if (isComponentRenderAs)
{
    builder.OpenComponent(0, RenderAs!);
    builder.AddMultipleAttributes(1, AdditionalAttributes);
    // ... class/style merge ...
    builder.AddAttribute(4, "ChildContent", ChildContent);
    builder.AddComponentReferenceCapture(5, ...);
    builder.CloseComponent();
}
else
{
    builder.OpenElement(0, As ?? DefaultTag);
    builder.AddMultipleAttributes(1, AdditionalAttributes);
    // ... same class/style merge ...
    builder.AddElementReferenceCapture(4, ...);
    builder.AddContent(5, ChildContent);
    builder.CloseElement();
}
```

### 2.2 Double Class/Style Emission
`AddMultipleAttributes` splats raw `class`/`style` from `AdditionalAttributes`, then explicit `AddAttribute` writes merged values. The last-write-wins behavior was browser-dependent.

### 2.3 Manual RenderTreeBuilder Sequencing
Pure `.cs` components required manual sequence number management, region tracking, and were harder to read than `.razor` markup.

### 2.4 RenderAs Constraint
`RenderAs` required the target type to implement `IReferencableComponent`, which is a Blazor-specific constraint with no Base UI equivalent.

---

## 3. New Architecture

```
Consumer markup
       │
       ▼
┌─────────────────────┐
│  ComponentRoot.razor │  ← Parameters: Render, ClassValue, StyleValue, ChildContent, AdditionalAttributes
│  (e.g. AvatarRoot)  │
│                      │
│  <CascadingValue     │
│    Value="context"   │
│    IsFixed="true">   │  ← Class context: always IsFixed="true"
│                      │
│    <RenderElement      │
│      TState="..."    │
│      Tag="span"      │  ← Default HTML tag
│      State="state"   │
│      Render="Render" │  ← Pass-through from consumer
│      ...             │
│    />                │
│  </CascadingValue>   │
└─────────────────────┘
          │
          ▼
┌─────────────────────┐
│ RenderElement<TState>  │  ← Shared component (ONE instance for all components)
│                      │
│  if (!Enabled) return│  ← Conditional rendering
│                      │
│  if (Render != null) │  ← Consumer's custom render
│    Render(props)     │     Gets: Attributes dict + State + ChildContent
│  else                │
│    <Tag ...attrs>    │  ← Default HTML element rendering
│      ChildContent    │
│    </Tag>            │
└─────────────────────┘
```

---

## 4. Shared Infrastructure Files

### 4.1 `RenderProps<TState>` — The Render Function Payload

**Location**: `src/BlazorBaseUI/AvatarPrototype/RenderProps.cs` (move to shared location during full rollout)

```csharp
public sealed record RenderProps<TState>(
    IReadOnlyDictionary<string, object> Attributes,
    TState State,
    RenderFragment? ChildContent);
```

This is the Blazor equivalent of Base UI's `(props, state)` passed to render functions. The consumer receives:
- **`Attributes`**: Pre-merged dictionary of all HTML attributes (aria-*, data-*, role, class, style, etc.)
- **`State`**: The component's current public state record
- **`ChildContent`**: The original `ChildContent` so the consumer can place it wherever they want

### 4.2 `RenderElement<TState>` — The Centralized Renderer

**Location**: `src/BlazorBaseUI/AvatarPrototype/RenderElement.razor` (move to shared location during full rollout)

**Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Tag` | `string` | Yes (`EditorRequired`) | HTML tag name (e.g. `"span"`, `"button"`, `"img"`) |
| `State` | `TState?` | No | Component state record for ClassValue/StyleValue callbacks |
| `Enabled` | `bool` | No (default: `true`) | When false, renders nothing (Base UI's `enabled` flag) |
| `Render` | `RenderFragment<RenderProps<TState>>?` | No | Consumer's custom render function |
| `ClassValue` | `Func<TState, string?>?` | No | State-driven CSS class callback |
| `StyleValue` | `Func<TState, string?>?` | No | State-driven inline style callback |
| `ComponentAttributes` | `IReadOnlyDictionary<string, object>?` | No | Internal attributes computed by the parent (aria-*, data-*, role) |
| `ChildContent` | `RenderFragment?` | No | Content to render inside the element |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | Splatted | Consumer's unmatched attributes |

**Rendering logic** (simplified):

```razor
@if (!Enabled) { return; }

@if (Render is not null)
{
    @Render(BuildRenderProps())
}
else
{
    @RenderDefaultElement
}
```

The `RenderDefaultElement` uses `RenderTreeBuilder` to emit a dynamic tag (since Razor can't do `<@Tag ...>`):

```csharp
private RenderFragment RenderDefaultElement => (RenderTreeBuilder builder) =>
{
    var attrs = BuildMergedAttributes();
    builder.OpenElement(0, Tag);
    builder.AddMultipleAttributes(1, attrs);
    builder.AddElementReferenceCapture(2, el => _element = el);
    builder.AddContent(3, ChildContent);
    builder.CloseElement();
};
```

> **Important**: Do NOT override `BuildRenderTree` in a `.razor` file — the Razor compiler generates it from markup. Use a `RenderFragment` field instead.

---

## 5. Step-by-Step Refactoring Checklist

For each component (e.g. `ButtonRoot`, `AccordionTrigger`, `ProgressRoot`):

### Phase 1: Convert Context (if not already done)

- [ ] Change context from `sealed record` to `sealed class` (per AGENTS.md section 7)
- [ ] Add `{ get; set; }` to mutable properties
- [ ] Keep `Action<T>` callbacks as `{ get; set; } = null!`
- [ ] Update root component to mutate context in place instead of creating new instances

### Phase 2: Convert Component File

- [ ] Rename `ComponentName.cs` to `ComponentName.razor`
- [ ] Add `@namespace` directive
- [ ] Add necessary `@using` directives (e.g. `Microsoft.Extensions.Logging`, `Microsoft.JSInterop`)
- [ ] Add `@implements` for `IDisposable`/`IAsyncDisposable` if applicable
- [ ] Add `@inject` for any services (replaces `[Inject]` properties)
- [ ] Replace the `BuildRenderTree` method body with `<RenderElement .../>` markup

### Phase 3: Remove Old Parameters, Add New

- [ ] **Remove** `As` parameter
- [ ] **Remove** `RenderAs` parameter
- [ ] **Remove** `isComponentRenderAs` field
- [ ] **Remove** `IReferencableComponent` type check in `OnParametersSet`
- [ ] **Remove** the entire `BuildRenderTree` override
- [ ] **Add** `Render` parameter: `RenderFragment<RenderProps<TState>>?`
- [ ] Keep `ClassValue`, `StyleValue`, `ChildContent`, `AdditionalAttributes` parameters as-is

### Phase 4: Update Root Component's CascadingValue

- [ ] Set `IsFixed="true"` on `<CascadingValue>` (see [Section 10](#10-cascadingvalue-isfixed-rule))
- [ ] Ensure `SetXxxStatus` methods mutate context in place + call `InvokeAsync(StateHasChanged)`

### Phase 5: Update Conditional Rendering

- [ ] Replace `if (condition) { return; }` at top of `BuildRenderTree` with `Enabled="expression"` on `<RenderElement>`
- [ ] Example: `Enabled="imageLoadingStatus == ImageLoadingStatus.Loaded"` replaces the early return

### Phase 6: Handle ComponentAttributes (if needed)

- [ ] If the component computes internal attributes (aria-*, data-*, role), pass them via `ComponentAttributes`
- [ ] Example: `ComponentAttributes="@(new Dictionary<string, object> { ["role"] = "progressbar", ["aria-valuenow"] = Value })""`

### Phase 7: Update Tests

- [ ] Remove tests for `As` parameter
- [ ] Remove tests for `RenderAs` parameter
- [ ] Add tests for `Render` parameter
- [ ] Verify `Enabled="false"` produces empty output

### Phase 8: Update Demo Pages

- [ ] Replace `As="div"` usage with `Render="RenderAsDiv"` pattern
- [ ] Replace `RenderAs="typeof(MyComponent)"` usage with `Render` function
- [ ] Add render function helpers in `@code` block

---

## 6. Before/After Comparison: AvatarRoot

### Before: `AvatarRoot.cs` (144 lines)

```csharp
public sealed class AvatarRoot : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "span";
    private bool isComponentRenderAs;
    private AvatarRootContext context = null!;   // record — new instance on each change

    [Parameter] public string? As { get; set; }
    [Parameter] public Type? RenderAs { get; set; }
    [Parameter] public Func<AvatarRootState, string?>? ClassValue { get; set; }
    [Parameter] public Func<AvatarRootState, string?>? StyleValue { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        context = new AvatarRootContext(imageLoadingStatus, SetImageLoadingStatus);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
            throw new InvalidOperationException(...);
        // ...
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<AvatarRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);  // record context = new ref each change
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderChildContent);
        builder.CloseComponent();
    }

    private void RenderChildContent(RenderTreeBuilder innerBuilder)
    {
        // 45 lines of duplicated As/RenderAs branching
        // with AddMultipleAttributes + explicit class/style (double emission)
    }

    private void SetImageLoadingStatus(ImageLoadingStatus status)
    {
        // Creates NEW record context instance
        context = new AvatarRootContext(imageLoadingStatus, SetImageLoadingStatus);
        _ = InvokeAsync(StateHasChanged);
    }
}
```

### After: `AvatarPrototypeRoot.razor` (87 lines)

```razor
@namespace BlazorBaseUI.AvatarPrototype
@using BlazorBaseUI.Avatar

<CascadingValue Value="context" IsFixed="true">
    <RenderElement TState="AvatarRootState"
                 Tag="span"
                 State="state"
                 Render="Render"
                 ClassValue="ClassValue"
                 StyleValue="StyleValue"
                 ChildContent="ChildContent"
                 @attributes="AdditionalAttributes" />
</CascadingValue>

@code {
    private ImageLoadingStatus imageLoadingStatus = ImageLoadingStatus.Idle;
    private AvatarPrototypeContext context = null!;   // class — mutated in place
    private AvatarRootState state = new(ImageLoadingStatus.Idle);

    [Parameter]
    public RenderFragment<RenderProps<AvatarRootState>>? Render { get; set; }

    [Parameter]
    public Func<AvatarRootState, string?>? ClassValue { get; set; }

    [Parameter]
    public Func<AvatarRootState, string?>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void OnInitialized()
    {
        context = new AvatarPrototypeContext
        {
            ImageLoadingStatus = imageLoadingStatus,
            SetImageLoadingStatus = SetImageLoadingStatus
        };
    }

    protected override void OnParametersSet()
    {
        if (state.ImageLoadingStatus != imageLoadingStatus)
        {
            state = new AvatarRootState(imageLoadingStatus);
        }
    }

    private void SetImageLoadingStatus(ImageLoadingStatus status)
    {
        if (imageLoadingStatus != status)
        {
            imageLoadingStatus = status;
            state = new AvatarRootState(imageLoadingStatus);
            context.ImageLoadingStatus = imageLoadingStatus;  // mutate in place
            _ = InvokeAsync(StateHasChanged);
        }
    }
}
```

**Key differences**:
- **-57 lines** (144 → 87)
- No `As`/`RenderAs` parameters
- No `isComponentRenderAs` field or type-checking logic
- No `BuildRenderTree` override
- No duplicated render branches
- No `IReferencableComponent` constraint
- `CascadingValue IsFixed="true"` with class context mutated in place

---

## 7. Before/After Comparison: AvatarImage

### Before: `AvatarImage.cs` (242 lines)

- Manual `BuildRenderTree` with early return + 45-line dual-branch
- `As`/`RenderAs` parameters + `isComponentRenderAs` checks
- `IReferencableComponent` constraint

### After: `AvatarPrototypeImage.razor` (178 lines)

```razor
<RenderElement TState="AvatarRootState"
             Tag="img"
             State="state"
             Enabled="imageLoadingStatus == ImageLoadingStatus.Loaded"
             Render="Render"
             ClassValue="ClassValue"
             StyleValue="StyleValue"
             ChildContent="ChildContent"
             @attributes="AdditionalAttributes" />
```

**Key differences**:
- **-64 lines** (242 → 178)
- Conditional rendering via `Enabled` flag instead of early `return` in `BuildRenderTree`
- `@inject` instead of `[Inject]` properties
- `@implements IAsyncDisposable` instead of class declaration
- No render branching at all — `RenderElement` handles everything

---

## 8. Before/After Comparison: AvatarFallback

### Before: `AvatarFallback.cs` (206 lines)

- Same duplicated pattern: `BuildRenderTree` with early return + 45-line dual-branch

### After: `AvatarPrototypeFallback.razor` (147 lines)

```razor
<RenderElement TState="AvatarRootState"
             Tag="span"
             State="state"
             Enabled="IsPresent"
             Render="Render"
             ClassValue="ClassValue"
             StyleValue="StyleValue"
             ChildContent="ChildContent"
             @attributes="AdditionalAttributes" />

@code {
    private bool IsPresent =>
        Context?.ImageLoadingStatus != ImageLoadingStatus.Loaded && delayPassed;
    // ...
}
```

**Key differences**:
- **-59 lines** (206 → 147)
- `Enabled="IsPresent"` replaces dual-condition early return
- `IsPresent` computed property encapsulates the visibility logic cleanly

---

## 9. Context Class Pattern

Per AGENTS.md section 7, all context types must be `sealed class` (not `sealed record`).

### Before (record):
```csharp
public sealed record AvatarRootContext(
    ImageLoadingStatus ImageLoadingStatus,
    Action<ImageLoadingStatus> SetImageLoadingStatus);
```

### After (class):
```csharp
public sealed class AvatarPrototypeContext
{
    public ImageLoadingStatus ImageLoadingStatus { get; set; }
    public Action<ImageLoadingStatus> SetImageLoadingStatus { get; set; } = null!;
}
```

### Root component update:

```csharp
// OLD (record): creates new instance on each change
context = new AvatarRootContext(imageLoadingStatus, SetImageLoadingStatus);

// NEW (class): mutates in place
context.ImageLoadingStatus = imageLoadingStatus;
```

### States remain records

State types (`AvatarRootState`, `ProgressRootState`, etc.) stay as `sealed record`:
- Structural equality allows `==` change detection in `OnParametersSet`
- Immutability protects consumers from accidental mutation
- `with` expressions enable clean derivation
- They represent snapshots, not long-lived service objects

---

## 10. CascadingValue IsFixed Rule

**Rule**: When context is a `sealed class` (mutable, identity-based), always use `IsFixed="true"`.

### Why

| Aspect | Record context (old) | Class context (new) |
|--------|---------------------|---------------------|
| How context changes | `context = new XxxContext(...)` | `context.Property = value` |
| Reference changes? | Yes (new instance) | No (same object) |
| `IsFixed="false"` detects change? | Yes | No (wasted comparison) |
| Children re-render anyway? | Via cascade + parent render | Via parent render only |
| Correct `IsFixed` | `false` | **`true`** |

With `IsFixed="true"`:
1. Blazor skips building the change-notification subscriber list
2. No reference comparison on every render cycle (the reference never changes anyway)
3. Children still receive fresh data because `InvokeAsync(StateHasChanged)` on the root re-renders the entire subtree
4. The `CascadingValue` only needs to *deliver* the reference once, not *monitor* it

### Pattern:

```razor
<CascadingValue Value="context" IsFixed="true">
    @* Children receive context via [CascadingParameter] *@
    @* Updates flow via parent's StateHasChanged, not cascade change detection *@
</CascadingValue>
```

---

## 11. Attribute Merging Strategy

`RenderElement.BuildMergedAttributes()` solves the double class/style emission bug:

```
Step 1: Copy AdditionalAttributes → result (SKIP class & style)
Step 2: Layer ComponentAttributes on top (wins for same key)
Step 3: Merge class: CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(State))
Step 4: Merge style: CombineStyles(AdditionalAttributes, StyleValue?.Invoke(State))
```

**Priority order** (highest wins):
1. `ComponentAttributes` (component-internal: aria-*, data-*, role)
2. `AdditionalAttributes` (consumer-provided, minus class/style)
3. Merged `class` = consumer's class + ClassValue(state)
4. Merged `style` = consumer's style + StyleValue(state)

This eliminates the old bug where `AddMultipleAttributes` would emit raw `class="..."` followed by another `class="..."` from the explicit `AddAttribute` call.

---

## 12. Conditional Rendering via Enabled

The `Enabled` parameter on `RenderElement` replaces early `return` statements in `BuildRenderTree`:

### Before:
```csharp
protected override void BuildRenderTree(RenderTreeBuilder builder)
{
    if (imageLoadingStatus != ImageLoadingStatus.Loaded)
    {
        return;  // hidden by early return
    }
    // ... 45 lines of render code
}
```

### After:
```razor
<RenderElement TState="AvatarRootState"
             Tag="img"
             Enabled="imageLoadingStatus == ImageLoadingStatus.Loaded"
             ... />
```

This maps directly to Base UI's `enabled` parameter in `useRenderElement`:
```tsx
useRenderElement('img', elementProps, {
  enabled: imageLoadingStatus === 'loaded',
  state,
  ref: mergedRef,
});
```

---

## 13. Render Function Usage for Consumers

### Basic syntax:

```razor
@* In @code block: *@
private RenderFragment<RenderProps<AvatarRootState>> MyRender =>
    ctx => @<div @attributes="ctx.Attributes">@ctx.ChildContent</div>;

@* In markup: *@
<AvatarRoot Render="MyRender" class="...">
    child content here
</AvatarRoot>
```

### Common patterns:

**Change the HTML tag** (replaces `As="div"`):
```csharp
private RenderFragment<RenderProps<AvatarRootState>> RenderAsDiv =>
    ctx => @<div @attributes="ctx.Attributes">@ctx.ChildContent</div>;
```

**Wrap in extra markup** (not possible with old As/RenderAs):
```csharp
private RenderFragment<RenderProps<AvatarRootState>> RenderWrapped =>
    ctx => @<div class="ring-2 ring-indigo-500">
               <span @attributes="ctx.Attributes">@ctx.ChildContent</span>
           </div>;
```

**Inspect state** (replaces needing callbacks):
```csharp
private RenderFragment<RenderProps<AvatarRootState>> RenderWithState =>
    ctx => @<span @attributes="ctx.Attributes"
                  title="@($"Status: {ctx.State.ImageLoadingStatus}")">
               @ctx.ChildContent
           </span>;
```

**Render as anchor** (replaces `As="a"`):
```csharp
private RenderFragment<RenderProps<AvatarRootState>> RenderAsAnchor =>
    ctx => @<a href="#profile" @attributes="ctx.Attributes">@ctx.ChildContent</a>;
```

### Important rules:

1. **Always splat `@attributes="ctx.Attributes"`** — this carries all merged attributes including class, style, aria-*, data-*, etc.
2. **Always place `@ctx.ChildContent`** — unlike React where `children` flows through props spread, Blazor's `ChildContent` must be explicitly placed.
3. **Razor templates can't be inline in markup attributes** — extract them to `@code` block properties and reference by name.

---

## 14. React children vs Blazor ChildContent

In React's Base UI, `children` is just another prop that flows through the spread:

```tsx
// React: children is in {...props}, implicitly placed
render={(props, state) => <div {...props} />}
// children renders automatically via the spread
```

In Blazor, `ChildContent` is a separate `RenderFragment` that must be explicitly placed:

```razor
@* Blazor: ChildContent must be explicitly placed *@
Render="ctx => @<div @attributes="ctx.Attributes">@ctx.ChildContent</div>"
```

If a consumer's render function omits `@ctx.ChildContent`, the children simply won't appear. This is intentional — it gives consumers the power to reposition or even omit children.

---

## 15. Common Pitfalls and Fixes

### Pitfall 1: `CS0111 BuildRenderTree already defined`
**Cause**: `.razor` files auto-generate `BuildRenderTree` from markup. You can't also define it in `@code`.
**Fix**: Use a `RenderFragment` field (`RenderDefaultElement`) and reference it from markup as `@RenderDefaultElement`.

### Pitfall 2: `RZ9986/RZ9994 Razor templates cannot be used in attributes`
**Cause**: Inline `@<div>...</div>` can't be passed as component parameter values directly in markup.
**Fix**: Extract to `@code` block as a named property:
```csharp
private RenderFragment<RenderProps<TState>> MyRender =>
    ctx => @<div @attributes="ctx.Attributes">@ctx.ChildContent</div>;
```
Then reference by name: `Render="MyRender"`.

### Pitfall 3: `CS0246` missing types in `.razor` files
**Cause**: `.razor` files don't inherit the `using` statements from `.cs` files in the same folder.
**Fix**: Add `@using` directives at the top of the `.razor` file:
```razor
@using Microsoft.Extensions.Logging
@using Microsoft.JSInterop
```

### Pitfall 4: `CascadingValue` not detecting class context changes
**Cause**: With `IsFixed="false"`, Blazor compares references. A mutated class instance has the same reference.
**Fix**: Use `IsFixed="true"`. Children re-render via `StateHasChanged` on the parent, not via cascade detection.

### Pitfall 5: Forgetting to place `@ctx.ChildContent` in render functions
**Cause**: Unlike React's `{...props}` spread which includes `children`, Blazor's `@attributes` does NOT include `ChildContent`.
**Fix**: Always explicitly place `@ctx.ChildContent` inside the render function's markup.

---

## 16. File Inventory

### Shared infrastructure (move to root `src/BlazorBaseUI/` during full rollout):

| File | Purpose |
|------|---------|
| `RenderProps.cs` | Record carrying Attributes + State + ChildContent to render functions |
| `RenderElement.razor` | Centralized renderer replacing all As/RenderAs dual-branch logic |

### Avatar prototype components:

| File | Replaces | Lines saved |
|------|----------|-------------|
| `AvatarPrototypeRoot.razor` | `AvatarRoot.cs` | 144 → 87 (-57) |
| `AvatarPrototypeImage.razor` | `AvatarImage.cs` | 242 → 178 (-64) |
| `AvatarPrototypeFallback.razor` | `AvatarFallback.cs` | 206 → 147 (-59) |
| `AvatarPrototypeContext.cs` | `AvatarRootContext.cs` | record → class |

### Demo files:

| File | Purpose |
|------|---------|
| `demo/.../Components/Pages/AvatarPrototypePage.razor` | Server route `/avatar-prototype` |
| `demo/.../Client/Components/Pages/AvatarPrototypePage.razor` | WASM route `/avatar-prototype/wasm` |
| `demo/.../Client/Shared/Sections/AvatarPrototypeSection.razor` | 6 demo sections showcasing all features |

### Parameters removed vs added:

| Removed | Added |
|---------|-------|
| `As` (string) | `Render` (RenderFragment<RenderProps<TState>>?) |
| `RenderAs` (Type) | — |

### Total line reduction for Avatar alone: **-180 lines** across 3 components (not counting the shared RenderElement which is written once and reused by all ~97 components).
