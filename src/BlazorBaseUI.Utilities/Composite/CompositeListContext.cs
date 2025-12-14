using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Utilities.Composite;

public sealed class CompositeListContext<TMetadata> : ICompositeListContext
{
    private readonly Dictionary<ElementReference, CompositeMetadata<TMetadata>> map = new();
    private readonly List<ElementReference> orderedElements = new();

    public IReadOnlyList<ElementReference> Elements => orderedElements;
    public event Action? MapChanged;

    public void Register(ElementReference element, object? metadata)
    {
        if (map.ContainsKey(element))
        {
            return;
        }

        var compositeMetadata = new CompositeMetadata<TMetadata>
        {
            Data = metadata is TMetadata typed ? typed : default
        };

        map[element] = compositeMetadata;
        orderedElements.Add(element);
        UpdateIndices();
        MapChanged?.Invoke();
    }

    public void Unregister(ElementReference element)
    {
        if (!map.ContainsKey(element))
        {
            return;
        }

        map.Remove(element);
        orderedElements.Remove(element);
        UpdateIndices();
        MapChanged?.Invoke();
    }

    public int GetIndex(ElementReference element)
    {
        return map.TryGetValue(element, out var metadata) ? metadata.Index : -1;
    }

    public CompositeMetadata<TMetadata>? GetMetadata(ElementReference element)
    {
        return map.TryGetValue(element, out var metadata) ? metadata : null;
    }

    public void ReorderByDom(IReadOnlyList<ElementReference> sortedElements)
    {
        orderedElements.Clear();
        orderedElements.AddRange(sortedElements);
        UpdateIndices();
        MapChanged?.Invoke();
    }

    private void UpdateIndices()
    {
        for (var i = 0; i < orderedElements.Count; i++)
        {
            var element = orderedElements[i];
            if (map.TryGetValue(element, out var metadata))
            {
                metadata.Index = i;
            }
        }
    }
}

public interface ICompositeListContext
{
    void Register(ElementReference element, object? metadata);
    void Unregister(ElementReference element);
    int GetIndex(ElementReference element);
    event Action? MapChanged;
}
