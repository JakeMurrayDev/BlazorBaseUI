using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI;

/// <summary>
/// Utilities for handling event forwarding when components are used via RenderAs.
/// Components that support being RenderAs targets should use these utilities to invoke
/// any event handlers passed through AdditionalAttributes.
/// </summary>
public static class EventUtilities
{
    /// <summary>
    /// Invokes an onclick handler from AdditionalAttributes if present.
    /// Use this in components that have their own click handling but may be used as RenderAs targets.
    /// </summary>
    public static Task InvokeOnClickAsync(IReadOnlyDictionary<string, object>? additionalAttributes, MouseEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onclick", e);
    }

    /// <summary>
    /// Invokes an onkeydown handler from AdditionalAttributes if present.
    /// </summary>
    public static Task InvokeOnKeyDownAsync(IReadOnlyDictionary<string, object>? additionalAttributes, KeyboardEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onkeydown", e);
    }

    public static Task InvokeOnMouseEnterAsync(IReadOnlyDictionary<string, object>? additionalAttributes, MouseEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onmouseenter", e);
    }

    public static Task InvokeOnMouseLeaveAsync(IReadOnlyDictionary<string, object>? additionalAttributes, MouseEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onmouseleave", e);
    }

    public static Task InvokeOnFocusAsync(IReadOnlyDictionary<string, object>? additionalAttributes, FocusEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onfocus", e);
    }

    public static Task InvokeOnBlurAsync(IReadOnlyDictionary<string, object>? additionalAttributes, FocusEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onblur", e);
    }

    public static Task InvokeOnPointerDownAsync(IReadOnlyDictionary<string, object>? additionalAttributes, PointerEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onpointerdown", e);
    }

    private static async Task InvokeEventAsync<TEvent>(IReadOnlyDictionary<string, object>? additionalAttributes, string attribute, TEvent e) where TEvent : EventArgs
    {
        if (additionalAttributes is null)
        {
            return;
        }

        if (!additionalAttributes.TryGetValue(attribute, out var value))
        {
            return;
        }

        switch (value)
        {
            case EventCallback<TEvent> callback:
                await callback.InvokeAsync(e);
                break;
            case EventCallback nonGenericCallback:
                await nonGenericCallback.InvokeAsync();
                break;
            case Action action:
                action();
                break;
            case Action<TEvent> actionWithArgs:
                actionWithArgs(e);
                break;
            case Func<Task> asyncAction:
                await asyncAction();
                break;
            case Func<TEvent, Task> asyncActionWithArgs:
                await asyncActionWithArgs(e);
                break;
            case MulticastDelegate del:
                var result = del.DynamicInvoke(e);
                if (result is Task task)
                {
                    await task;
                }
                break;
        }
    }
}
