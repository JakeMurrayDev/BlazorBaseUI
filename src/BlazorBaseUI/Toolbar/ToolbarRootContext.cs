using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Toolbar;

public sealed record ToolbarRootContext(
    bool Disabled,
    Orientation Orientation,
    Action<ElementReference> RegisterItem,
    Action<ElementReference> UnregisterItem);
