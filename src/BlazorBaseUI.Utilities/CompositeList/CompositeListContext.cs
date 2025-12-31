using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Utilities.CompositeList;

public interface ICompositeListContext
{
    int Register(ElementReference element);
    void Unregister(int index);
    ElementReference? GetElement(int index);
    IReadOnlyList<ElementReference> GetAllElements();
    int Count { get; }
}

public interface ICompositeListContext<TMetadata> : ICompositeListContext
{
    int Register(ElementReference element, TMetadata metadata);
    TMetadata? GetMetadata(int index);
    IReadOnlyList<(ElementReference Element, TMetadata Metadata)> GetAllItems();
}

public sealed class CompositeListContext : ICompositeListContext
{
    private readonly List<ElementReference?> elements = [];
    private readonly Queue<int> freeIndices = new();

    public int Count => elements.Count(e => e.HasValue);

    public int Register(ElementReference element)
    {
        if (freeIndices.TryDequeue(out var freeIndex))
        {
            elements[freeIndex] = element;
            return freeIndex;
        }

        var index = elements.Count;
        elements.Add(element);
        return index;
    }

    public void Unregister(int index)
    {
        if (index >= 0 && index < elements.Count)
        {
            elements[index] = null;
            freeIndices.Enqueue(index);
        }
    }

    public ElementReference? GetElement(int index)
    {
        if (index >= 0 && index < elements.Count)
            return elements[index];
        return null;
    }

    public IReadOnlyList<ElementReference> GetAllElements()
    {
        return elements
            .Where(e => e.HasValue)
            .Select(e => e!.Value)
            .ToList();
    }
}

public sealed class CompositeListContext<TMetadata> : ICompositeListContext<TMetadata>
{
    private readonly List<(ElementReference? Element, TMetadata? Metadata)> items = [];
    private readonly Queue<int> freeIndices = new();

    public int Count => items.Count(i => i.Element.HasValue);

    public int Register(ElementReference element)
    {
        return Register(element, default!);
    }

    public int Register(ElementReference element, TMetadata metadata)
    {
        if (freeIndices.TryDequeue(out var freeIndex))
        {
            items[freeIndex] = (element, metadata);
            return freeIndex;
        }

        var index = items.Count;
        items.Add((element, metadata));
        return index;
    }

    public void Unregister(int index)
    {
        if (index >= 0 && index < items.Count)
        {
            items[index] = (null, default);
            freeIndices.Enqueue(index);
        }
    }

    public ElementReference? GetElement(int index)
    {
        if (index >= 0 && index < items.Count)
            return items[index].Element;
        return null;
    }

    public TMetadata? GetMetadata(int index)
    {
        if (index >= 0 && index < items.Count)
            return items[index].Metadata;
        return default;
    }

    public IReadOnlyList<ElementReference> GetAllElements()
    {
        return items
            .Where(i => i.Element.HasValue)
            .Select(i => i.Element!.Value)
            .ToList();
    }

    public IReadOnlyList<(ElementReference Element, TMetadata Metadata)> GetAllItems()
    {
        return items
            .Where(i => i.Element.HasValue)
            .Select(i => (i.Element!.Value, i.Metadata!))
            .ToList();
    }
}