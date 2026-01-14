const STATE_KEY = Symbol.for('BlazorBaseUI.Tabs.State');
const LIST_STATE_KEY = Symbol.for('BlazorBaseUI.TabsList.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        resizeObservers: new WeakMap(),
        dotNetRefs: new WeakMap(),
        listHandlers: new WeakMap()
    };
}
const state = window[STATE_KEY];

if (!window[LIST_STATE_KEY]) {
    window[LIST_STATE_KEY] = new WeakMap();
}
const listStateMap = window[LIST_STATE_KEY];

const NAVIGATION_KEYS = ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'];

export function initializeList(element, orientation, loopFocus, activateOnFocus, dotNetRef) {
    if (!element) return;

    const listState = {
        element,
        orientation,
        loopFocus,
        activateOnFocus,
        dotNetRef,
        tabs: new Set()
    };

    const handler = (e) => {
        if (!NAVIGATION_KEYS.includes(e.key)) return;

        const isHorizontal = listState.orientation === 'horizontal';
        const isVertical = listState.orientation === 'vertical';

        const shouldPrevent =
            (isHorizontal && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) ||
            (isVertical && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) ||
            e.key === 'Home' ||
            e.key === 'End';

        if (shouldPrevent) {
            e.preventDefault();
        }
    };

    element.addEventListener('keydown', handler);
    state.listHandlers.set(element, handler);
    listStateMap.set(element, listState);
}

export function updateList(element, orientation, loopFocus, activateOnFocus) {
    if (!element) return;

    const listState = listStateMap.get(element);
    if (listState) {
        listState.orientation = orientation;
        listState.loopFocus = loopFocus;
        listState.activateOnFocus = activateOnFocus;
    }
}

export function disposeList(element) {
    if (!element) return;

    const handler = state.listHandlers.get(element);
    if (handler) {
        element.removeEventListener('keydown', handler);
        state.listHandlers.delete(element);
    }

    listStateMap.delete(element);
    unobserveResize(element);
}

export function registerTab(listElement, tabElement, value) {
    if (!listElement || !tabElement) return;

    const listState = listStateMap.get(listElement);
    if (!listState) return;

    for (const item of listState.tabs) {
        if (item.element === tabElement) {
            item.value = value;
            updateTabIndexes(listElement);
            return;
        }
    }

    listState.tabs.add({ element: tabElement, value });
    updateTabIndexes(listElement);
}

export function unregisterTab(listElement, tabElement) {
    if (!listElement || !tabElement) return;

    const listState = listStateMap.get(listElement);
    if (!listState) return;

    for (const item of listState.tabs) {
        if (item.element === tabElement) {
            listState.tabs.delete(item);
            updateTabIndexes(listElement);
            return;
        }
    }
}

function getOrderedTabs(listElement) {
    const listState = listStateMap.get(listElement);
    if (!listState) return [];

    const items = Array.from(listState.tabs).filter(item => document.contains(item.element));
    items.sort((a, b) => {
        const position = a.element.compareDocumentPosition(b.element);
        if (position & Node.DOCUMENT_POSITION_FOLLOWING) return -1;
        if (position & Node.DOCUMENT_POSITION_PRECEDING) return 1;
        return 0;
    });
    return items;
}

function isTabDisabled(tabElement) {
    return tabElement.hasAttribute('data-disabled') || tabElement.disabled || tabElement.getAttribute('aria-disabled') === 'true';
}

function updateTabIndexes(listElement) {
    const items = getOrderedTabs(listElement);
    if (items.length === 0) return;

    const hasActive = items.some(item => item.element.getAttribute('aria-selected') === 'true');
    const firstEnabled = items.find(item => !isTabDisabled(item.element));

    for (const item of items) {
        const isActive = item.element.getAttribute('aria-selected') === 'true';
        const isDisabled = isTabDisabled(item.element);

        if (isDisabled) {
            item.element.tabIndex = -1;
        } else if (isActive) {
            item.element.tabIndex = 0;
        } else if (!hasActive && item === firstEnabled) {
            item.element.tabIndex = 0;
        } else {
            item.element.tabIndex = -1;
        }
    }
}

export async function navigateToPrevious(listElement, currentElement) {
    const listState = listStateMap.get(listElement);
    if (!listState) return false;

    const items = getOrderedTabs(listElement);
    const currentIndex = items.findIndex(item => item.element === currentElement);
    if (currentIndex < 0) return false;

    for (let i = currentIndex - 1; i >= 0; i--) {
        if (!isTabDisabled(items[i].element)) {
            await setFocusedTab(listElement, items[i]);
            return true;
        }
    }

    if (listState.loopFocus) {
        for (let i = items.length - 1; i > currentIndex; i--) {
            if (!isTabDisabled(items[i].element)) {
                await setFocusedTab(listElement, items[i]);
                return true;
            }
        }
    }

    return false;
}

export async function navigateToNext(listElement, currentElement) {
    const listState = listStateMap.get(listElement);
    if (!listState) return false;

    const items = getOrderedTabs(listElement);
    const currentIndex = items.findIndex(item => item.element === currentElement);
    if (currentIndex < 0) return false;

    for (let i = currentIndex + 1; i < items.length; i++) {
        if (!isTabDisabled(items[i].element)) {
            await setFocusedTab(listElement, items[i]);
            return true;
        }
    }

    if (listState.loopFocus) {
        for (let i = 0; i < currentIndex; i++) {
            if (!isTabDisabled(items[i].element)) {
                await setFocusedTab(listElement, items[i]);
                return true;
            }
        }
    }

    return false;
}

export async function navigateToFirst(listElement) {
    const items = getOrderedTabs(listElement);
    for (const item of items) {
        if (!isTabDisabled(item.element)) {
            await setFocusedTab(listElement, item);
            return true;
        }
    }
    return false;
}

export async function navigateToLast(listElement) {
    const items = getOrderedTabs(listElement);
    for (let i = items.length - 1; i >= 0; i--) {
        if (!isTabDisabled(items[i].element)) {
            await setFocusedTab(listElement, items[i]);
            return true;
        }
    }
    return false;
}

async function setFocusedTab(listElement, item) {
    const listState = listStateMap.get(listElement);
    if (!listState) return;

    const items = getOrderedTabs(listElement);
    for (const t of items) {
        t.element.tabIndex = t.element === item.element ? 0 : -1;
    }

    item.element.focus({ preventScroll: true });
    item.element.scrollIntoView({ block: 'nearest', inline: 'nearest' });

    if (listState.activateOnFocus && listState.dotNetRef) {
        await listState.dotNetRef.invokeMethodAsync('OnNavigateToTab', item.value);
    }
}

export function getFirstEnabledTab(listElement) {
    const items = getOrderedTabs(listElement);
    for (const item of items) {
        if (!isTabDisabled(item.element)) {
            return item.element;
        }
    }
    return null;
}

export function getActiveElement() {
    return document.activeElement;
}

export function initializeTab(element) {
    if (!element) return;

    element._tabsKeydownHandler = (e) => {
        if (element.disabled || element.getAttribute('aria-disabled') === 'true') return;

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
    element.scrollIntoView({ block: 'nearest', inline: 'nearest' });
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
