const STATE_KEY = Symbol.for('BlazorBaseUI.Tabs.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        resizeObservers: new WeakMap(),
        dotNetRefs: new WeakMap()
    };
}
const state = window[STATE_KEY];

export function initializeList(element) {
    if (!element) return;
}

export function disposeList(element) {
    if (!element) return;
    unobserveResize(element);
}

export function initializeTab(element, disabled) {
    if (!element) return;

    element._tabsKeydownHandler = (e) => {
        if (disabled) return;

        if (e.key === ' ' || e.key === 'Enter') {
            e.preventDefault();
            element.click();
        }
    };

    element.addEventListener('keydown', element._tabsKeydownHandler);
}

export function dispose(element) {
    if (!element) return;

    if (element._tabsKeydownHandler) {
        element.removeEventListener('keydown', element._tabsKeydownHandler);
        delete element._tabsKeydownHandler;
    }
}

export function focus(element) {
    if (!element) return;
    element.focus({ preventScroll: true });
}

export function getTabPosition(listElement, tabElement) {
    if (!listElement || !tabElement) {
        return { left: 0, top: 0, width: 0, height: 0 };
    }

    const tabRect = tabElement.getBoundingClientRect();
    const listRect = listElement.getBoundingClientRect();

    const left = tabRect.left - listRect.left + listElement.scrollLeft;
    const top = tabRect.top - listRect.top + listElement.scrollTop;

    return {
        left: left,
        top: top,
        width: tabRect.width,
        height: tabRect.height
    };
}

export function getIndicatorPosition(listElement, tabElement) {
    if (!listElement || !tabElement) {
        return { left: 0, right: 0, top: 0, bottom: 0, width: 0, height: 0 };
    }

    const tabRect = tabElement.getBoundingClientRect();
    const listRect = listElement.getBoundingClientRect();

    const listStyle = getComputedStyle(listElement);
    const listWidth = parseFloat(listStyle.width) || listRect.width;
    const listHeight = parseFloat(listStyle.height) || listRect.height;

    const scaleX = listWidth > 0 ? listRect.width / listWidth : 1;
    const scaleY = listHeight > 0 ? listRect.height / listHeight : 1;

    let left, top, width, height;

    const hasNonZeroScale = Math.abs(scaleX) > Number.EPSILON && Math.abs(scaleY) > Number.EPSILON;

    if (hasNonZeroScale) {
        const tabLeftDelta = tabRect.left - listRect.left;
        const tabTopDelta = tabRect.top - listRect.top;

        left = tabLeftDelta / scaleX + listElement.scrollLeft - listElement.clientLeft;
        top = tabTopDelta / scaleY + listElement.scrollTop - listElement.clientTop;
    } else {
        left = tabElement.offsetLeft;
        top = tabElement.offsetTop;
    }

    const tabStyle = getComputedStyle(tabElement);
    width = parseFloat(tabStyle.width) || tabRect.width;
    height = parseFloat(tabStyle.height) || tabRect.height;

    const right = listElement.scrollWidth - left - width;
    const bottom = listElement.scrollHeight - top - height;

    return {
        left: left,
        right: right,
        top: top,
        bottom: bottom,
        width: width,
        height: height
    };
}

export function observeResize(listElement, dotNetRef) {
    if (!listElement || !dotNetRef) return;

    if (typeof ResizeObserver === 'undefined') return;

    if (state.resizeObservers.has(listElement)) {
        return;
    }

    const observer = new ResizeObserver(() => {
        const ref = state.dotNetRefs.get(listElement);
        if (ref) {
            ref.invokeMethodAsync('OnResizeAsync').catch(() => { });
        }
    });

    observer.observe(listElement);
    state.resizeObservers.set(listElement, observer);
    state.dotNetRefs.set(listElement, dotNetRef);
}

export function unobserveResize(listElement) {
    if (!listElement) return;

    const observer = state.resizeObservers.get(listElement);
    if (observer) {
        observer.disconnect();
        state.resizeObservers.delete(listElement);
    }

    state.dotNetRefs.delete(listElement);
}
