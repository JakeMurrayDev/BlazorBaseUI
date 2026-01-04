using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI;

public interface IReferencableComponent
{
    ElementReference? Element { get; }
}