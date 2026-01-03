using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Slider;

public sealed record ThumbMetadata(
    string InputId,
    ElementReference ThumbElement,
    ElementReference InputElement);
