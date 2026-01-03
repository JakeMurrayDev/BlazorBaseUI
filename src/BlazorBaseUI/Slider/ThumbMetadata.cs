using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Slider;

public record ThumbMetadata(
    string InputId,
    ElementReference ThumbElement,
    ElementReference InputElement);
