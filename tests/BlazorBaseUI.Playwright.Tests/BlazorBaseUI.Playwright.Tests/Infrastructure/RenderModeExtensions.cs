using System.Text;

namespace BlazorBaseUI.Playwright.Tests.Infrastructure;

public enum TestRenderMode
{
    Server,
    Wasm
}

public static class RenderModeExtensions
{
    public static string GetTestPath(this TestRenderMode mode, string basePath)
    {
        // Convert /tests/collapsible to /tests/collapsible/server or /tests/collapsible/wasm
        return mode switch
        {
            TestRenderMode.Server => $"{basePath.TrimEnd('/')}/server",
            TestRenderMode.Wasm => $"{basePath.TrimEnd('/')}/wasm",
            _ => $"{basePath.TrimEnd('/')}/server"
        };
    }
}

public sealed class TestPageUrlBuilder
{
    private readonly string baseAddress;
    private readonly string path;
    private readonly TestRenderMode renderMode;
    private readonly Dictionary<string, string> queryParams = new();

    public TestPageUrlBuilder(string baseAddress, string basePath, TestRenderMode renderMode)
    {
        this.baseAddress = baseAddress.TrimEnd('/');
        this.path = renderMode.GetTestPath(basePath);
        this.renderMode = renderMode;
    }

    public TestPageUrlBuilder WithKeepMounted(bool value)
    {
        queryParams["keepMounted"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithHiddenUntilFound(bool value)
    {
        queryParams["hiddenUntilFound"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithAnimated(bool value)
    {
        queryParams["animated"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithDisabled(bool value)
    {
        queryParams["disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithDefaultOpen(bool value)
    {
        queryParams["defaultOpen"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithCustomPanelId(string? id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            queryParams["panelId"] = id;
        }
        return this;
    }

    public TestPageUrlBuilder WithImageSrc(string src)
    {
        queryParams["imageSrc"] = src;
        return this;
    }

    public TestPageUrlBuilder WithFallbackDelay(int? delayMs)
    {
        if (delayMs.HasValue)
        {
            queryParams["fallbackDelay"] = delayMs.Value.ToString();
        }
        return this;
    }

    public TestPageUrlBuilder WithForceError(bool value)
    {
        queryParams["forceError"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowFallback(bool value)
    {
        queryParams["showFallback"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithCustomClass(string className)
    {
        queryParams["customClass"] = className;
        return this;
    }

    // Menu-specific parameters
    public TestPageUrlBuilder WithModal(bool value)
    {
        queryParams["modal"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithOrientation(string orientation)
    {
        queryParams["orientation"] = orientation;
        return this;
    }

    public TestPageUrlBuilder WithLoopFocus(bool value)
    {
        queryParams["loopFocus"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithOpenOnHover(bool value)
    {
        queryParams["openOnHover"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithOpenDelay(int delayMs)
    {
        queryParams["openDelay"] = delayMs.ToString();
        return this;
    }

    public TestPageUrlBuilder WithCloseDelay(int delayMs)
    {
        queryParams["closeDelay"] = delayMs.ToString();
        return this;
    }

    public TestPageUrlBuilder WithShowCheckbox(bool value)
    {
        queryParams["showCheckbox"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowRadioGroup(bool value)
    {
        queryParams["showRadioGroup"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowSubmenu(bool value)
    {
        queryParams["showSubmenu"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithCloseParentOnEsc(bool value)
    {
        queryParams["closeParentOnEsc"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithHighlightItemOnHover(bool value)
    {
        queryParams["highlightItemOnHover"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowTextNavItems(bool value)
    {
        queryParams["showTextNavItems"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowNestedSubmenu(bool value)
    {
        queryParams["showNestedSubmenu"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithDirection(string direction)
    {
        queryParams["direction"] = direction;
        return this;
    }

    // Accordion-specific parameters
    public TestPageUrlBuilder WithMultiple(bool value)
    {
        queryParams["multiple"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithHorizontal(bool value)
    {
        queryParams["horizontal"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithRootDisabled(bool value)
    {
        queryParams["rootDisabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithItem1Disabled(bool value)
    {
        queryParams["item1Disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithItem2Disabled(bool value)
    {
        queryParams["item2Disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithItem3Disabled(bool value)
    {
        queryParams["item3Disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithUseCustomValues(bool value)
    {
        queryParams["useCustomValues"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithUseCustomPanelId(bool value)
    {
        queryParams["useCustomPanelId"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowThirdItem(bool value)
    {
        queryParams["showThirdItem"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowFourthItem(bool value)
    {
        queryParams["showFourthItem"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowInput(bool value)
    {
        queryParams["showInput"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public string Build()
    {
        var sb = new StringBuilder();
        sb.Append(baseAddress);
        sb.Append(path);

        if (queryParams.Count > 0)
        {
            sb.Append('?');
            var first = true;
            foreach (var (key, value) in queryParams)
            {
                if (!first) sb.Append('&');
                sb.Append(Uri.EscapeDataString(key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(value));
                first = false;
            }
        }

        return sb.ToString();
    }
}
