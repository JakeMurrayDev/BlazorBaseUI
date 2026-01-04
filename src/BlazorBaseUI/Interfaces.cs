using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace BlazorBaseUI;

public interface IReferencableComponent
{
    [DisallowNull]
    ElementReference? Element { get; }
}