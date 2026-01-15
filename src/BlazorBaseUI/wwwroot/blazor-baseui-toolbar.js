const STATE_KEY = Symbol.for('BlazorBaseUI.Toolbar.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const state = window[STATE_KEY];

function getItems(element) {
    const toolbarState = state.get(element);
    if (!toolbarState) return [];
    const items = Array.from(toolbarState.items).filter(item => document.contains(item));
    items.sort((a, b) => {
        const position = a.compareDocumentPosition(b);
        if (position & Node.DOCUMENT_POSITION_FOLLOWING) return -1;
        if (position & Node.DOCUMENT_POSITION_PRECEDING) return 1;
        return 0;
    });
    return items;
}

function getFocusableItems(element) {
    return getItems(element).filter(item => {
        if (item.hasAttribute('data-disabled') && !item.hasAttribute('data-focusable')) {
            return false;
        }
        return true;
    });
}

function focusItem(item) {
    if (item) {
        item.tabIndex = 0;
        item.focus();
    }
}

function handleKeyDown(event) {
    const element = event.currentTarget;
    const toolbarState = state.get(element);
    if (!toolbarState) return;

    const { orientation, loopFocus } = toolbarState;
    const isHorizontal = orientation === 'horizontal';
    const items = getFocusableItems(element);

    if (items.length === 0) return;

    const currentIndex = items.indexOf(document.activeElement);
    if (currentIndex === -1) return;

    let nextIndex = -1;
    let handled = false;

    const prevKey = isHorizontal ? 'ArrowLeft' : 'ArrowUp';
    const nextKey = isHorizontal ? 'ArrowRight' : 'ArrowDown';

    if (event.key === nextKey) {
        if (currentIndex < items.length - 1) {
            nextIndex = currentIndex + 1;
        } else if (loopFocus) {
            nextIndex = 0;
        }
        handled = true;
    } else if (event.key === prevKey) {
        if (currentIndex > 0) {
            nextIndex = currentIndex - 1;
        } else if (loopFocus) {
            nextIndex = items.length - 1;
        }
        handled = true;
    } else if (event.key === 'Home') {
        nextIndex = 0;
        handled = true;
    } else if (event.key === 'End') {
        nextIndex = items.length - 1;
        handled = true;
    }

    if (handled) {
        event.preventDefault();
        event.stopPropagation();

        if (nextIndex !== -1 && nextIndex !== currentIndex) {
            items[currentIndex].tabIndex = -1;
            focusItem(items[nextIndex]);
        }
    }
}

export function initToolbar(element, orientation, loopFocus) {
    if (!element) return;

    const toolbarState = {
        orientation,
        loopFocus,
        items: new Set()
    };

    state.set(element, toolbarState);
    element.addEventListener('keydown', handleKeyDown);
}

export function updateToolbar(element, orientation, loopFocus) {
    if (!element) return;

    const toolbarState = state.get(element);
    if (toolbarState) {
        toolbarState.orientation = orientation;
        toolbarState.loopFocus = loopFocus;
    }
}

export function disposeToolbar(element) {
    if (!element) return;

    element.removeEventListener('keydown', handleKeyDown);
    state.delete(element);
}

export function registerItem(toolbarElement, itemElement) {
    if (!toolbarElement || !itemElement) return;

    const toolbarState = state.get(toolbarElement);
    if (!toolbarState) return;

    const items = toolbarState.items;
    items.add(itemElement);

    const sortedItems = getItems(toolbarElement);
    const firstItem = sortedItems[0];

    for (const item of sortedItems) {
        item.tabIndex = item === firstItem ? 0 : -1;
    }
}

export function unregisterItem(toolbarElement, itemElement) {
    if (!toolbarElement || !itemElement) return;

    const toolbarState = state.get(toolbarElement);
    if (!toolbarState) return;

    const items = toolbarState.items;
    const hadFocus = document.activeElement === itemElement;
    const sortedBefore = getItems(toolbarElement);
    const wasFirst = sortedBefore[0] === itemElement;

    items.delete(itemElement);

    if (items.size > 0 && (hadFocus || wasFirst)) {
        const sortedAfter = getItems(toolbarElement);
        const firstItem = sortedAfter[0];
        if (firstItem) {
            firstItem.tabIndex = 0;
            if (hadFocus) {
                firstItem.focus();
            }
        }
    }
}
