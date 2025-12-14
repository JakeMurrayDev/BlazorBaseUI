using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorBaseUI.Utilities.Composite;

public static class CompositeKeyboardNavigation
{
    public const string ArrowUp = "ArrowUp";
    public const string ArrowDown = "ArrowDown";
    public const string ArrowLeft = "ArrowLeft";
    public const string ArrowRight = "ArrowRight";
    public const string Home = "Home";
    public const string End = "End";

    public static readonly HashSet<string> HorizontalKeys = new() { ArrowLeft, ArrowRight };
    public static readonly HashSet<string> VerticalKeys = new() { ArrowUp, ArrowDown };
    public static readonly HashSet<string> ArrowKeys = new() { ArrowUp, ArrowDown, ArrowLeft, ArrowRight };
    public static readonly HashSet<string> AllKeys = new() { ArrowUp, ArrowDown, ArrowLeft, ArrowRight, Home, End };
    public static readonly HashSet<string> ModifierKeys = new() { "Shift", "Control", "Alt", "Meta" };

    public static int GetNextIndex(
        string key,
        int currentIndex,
        int itemCount,
        CompositeOrientation orientation,
        bool loopFocus,
        bool isRtl,
        int cols = 1,
        bool enableHomeAndEndKeys = false,
        IReadOnlyList<int>? disabledIndices = null)
    {
        if (itemCount == 0)
        {
            return -1;
        }

        var isGrid = cols > 1;
        var nextIndex = currentIndex;

        var effectiveKey = key;
        if (isRtl)
        {
            effectiveKey = key switch
            {
                ArrowLeft => ArrowRight,
                ArrowRight => ArrowLeft,
                _ => key
            };
        }

        if (isGrid)
        {
            nextIndex = GetGridNavigatedIndex(
                effectiveKey,
                currentIndex,
                itemCount,
                cols,
                orientation,
                loopFocus,
                disabledIndices);
        }
        else
        {
            nextIndex = GetListNavigatedIndex(
                effectiveKey,
                currentIndex,
                itemCount,
                orientation,
                loopFocus,
                enableHomeAndEndKeys,
                disabledIndices);
        }

        return nextIndex;
    }

    private static int GetListNavigatedIndex(
        string key,
        int currentIndex,
        int itemCount,
        CompositeOrientation orientation,
        bool loopFocus,
        bool enableHomeAndEndKeys,
        IReadOnlyList<int>? disabledIndices)
    {
        var isHorizontal = orientation == CompositeOrientation.Horizontal;
        var isBoth = orientation == CompositeOrientation.Both;

        var prevKey = isHorizontal || isBoth ? ArrowLeft : ArrowUp;
        var nextKey = isHorizontal || isBoth ? ArrowRight : ArrowDown;

        if (orientation == CompositeOrientation.Vertical || isBoth)
        {
            if (key == ArrowUp)
            {
                prevKey = ArrowUp;
            }

            if (key == ArrowDown)
            {
                nextKey = ArrowDown;
            }
        }

        var nextIndex = currentIndex;

        if (key == prevKey)
        {
            nextIndex = FindNonDisabledIndex(currentIndex, itemCount, -1, loopFocus, disabledIndices);
        }
        else if (key == nextKey)
        {
            nextIndex = FindNonDisabledIndex(currentIndex, itemCount, 1, loopFocus, disabledIndices);
        }
        else if (enableHomeAndEndKeys && key == Home)
        {
            nextIndex = FindNonDisabledIndex(-1, itemCount, 1, false, disabledIndices);
        }
        else if (enableHomeAndEndKeys && key == End)
        {
            nextIndex = FindNonDisabledIndex(itemCount, itemCount, -1, false, disabledIndices);
        }

        return nextIndex;
    }

    private static int GetGridNavigatedIndex(
        string key,
        int currentIndex,
        int itemCount,
        int cols,
        CompositeOrientation orientation,
        bool loopFocus,
        IReadOnlyList<int>? disabledIndices)
    {
        var row = currentIndex / cols;
        var col = currentIndex % cols;
        var rows = (int)Math.Ceiling((double)itemCount / cols);
        var nextIndex = currentIndex;

        switch (key)
        {
            case ArrowUp when orientation != CompositeOrientation.Horizontal:
                if (row > 0)
                {
                    nextIndex = currentIndex - cols;
                }
                else if (loopFocus)
                {
                    nextIndex = (rows - 1) * cols + col;
                    if (nextIndex >= itemCount)
                    {
                        nextIndex -= cols;
                    }
                }
                break;

            case ArrowDown when orientation != CompositeOrientation.Horizontal:
                if (row < rows - 1 && currentIndex + cols < itemCount)
                {
                    nextIndex = currentIndex + cols;
                }
                else if (loopFocus)
                {
                    nextIndex = col;
                }
                break;

            case ArrowLeft when orientation != CompositeOrientation.Vertical:
                if (col > 0)
                {
                    nextIndex = currentIndex - 1;
                }
                else if (loopFocus && orientation == CompositeOrientation.Both)
                {
                    nextIndex = row * cols + cols - 1;
                    if (nextIndex >= itemCount)
                    {
                        nextIndex = itemCount - 1;
                    }
                }
                break;

            case ArrowRight when orientation != CompositeOrientation.Vertical:
                if (col < cols - 1 && currentIndex + 1 < itemCount)
                {
                    nextIndex = currentIndex + 1;
                }
                else if (loopFocus && orientation == CompositeOrientation.Both)
                {
                    nextIndex = row * cols;
                }
                break;
        }

        if (disabledIndices != null && disabledIndices.Contains(nextIndex))
        {
            return currentIndex;
        }

        return nextIndex;
    }

    private static int FindNonDisabledIndex(
        int startingIndex,
        int itemCount,
        int direction,
        bool loopFocus,
        IReadOnlyList<int>? disabledIndices)
    {
        var index = startingIndex + direction;

        while (index >= 0 && index < itemCount)
        {
            if (disabledIndices == null || !disabledIndices.Contains(index))
            {
                return index;
            }

            index += direction;
        }

        if (loopFocus)
        {
            index = direction > 0 ? 0 : itemCount - 1;

            while (index != startingIndex)
            {
                if (disabledIndices == null || !disabledIndices.Contains(index))
                {
                    return index;
                }

                index += direction;

                if (index < 0)
                {
                    index = itemCount - 1;
                }
                else if (index >= itemCount)
                {
                    index = 0;
                }
            }
        }

        return startingIndex;
    }

    public static bool IsRelevantKey(string key, CompositeOrientation orientation, bool enableHomeAndEndKeys)
    {
        var relevantKeys = orientation switch
        {
            CompositeOrientation.Horizontal => enableHomeAndEndKeys
                ? new HashSet<string> { ArrowLeft, ArrowRight, Home, End }
                : HorizontalKeys,
            CompositeOrientation.Vertical => enableHomeAndEndKeys
                ? new HashSet<string> { ArrowUp, ArrowDown, Home, End }
                : VerticalKeys,
            CompositeOrientation.Both => enableHomeAndEndKeys ? AllKeys : ArrowKeys,
            _ => ArrowKeys
        };

        return relevantKeys.Contains(key);
    }
}
