const STATE_KEY = Symbol.for('BlazorBaseUI.MenuBar.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const state = window[STATE_KEY];

const SCROLL_LOCK_KEY = Symbol.for('BlazorBaseUI.MenuBar.ScrollLock');
if (!window[SCROLL_LOCK_KEY]) {
    window[SCROLL_LOCK_KEY] = {
        lockedBy: null,
        originalStyles: null
    };
}
const scrollLockState = window[SCROLL_LOCK_KEY];

function getItems(element) {
    const menuBarState = state.get(element);
    if (!menuBarState) return [];
    const items = Array.from(menuBarState.items).filter(item => document.contains(item));
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
    const menuBarState = state.get(element);
    if (!menuBarState) return;

    const { orientation, loopFocus } = menuBarState;
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

export function initMenuBar(element, orientation, loopFocus) {
    if (!element) return;

    const menuBarState = {
        orientation,
        loopFocus,
        items: new Set()
    };

    state.set(element, menuBarState);
    element.addEventListener('keydown', handleKeyDown);
}

export function updateMenuBar(element, orientation, loopFocus) {
    if (!element) return;

    const menuBarState = state.get(element);
    if (menuBarState) {
        menuBarState.orientation = orientation;
        menuBarState.loopFocus = loopFocus;
    }
}

export function disposeMenuBar(element) {
    if (!element) return;

    element.removeEventListener('keydown', handleKeyDown);

    if (scrollLockState.lockedBy === element) {
        unlockScroll();
    }

    state.delete(element);
}

export function updateScrollLock(element, shouldLock) {
    if (!element) return;

    if (shouldLock) {
        if (scrollLockState.lockedBy === null) {
            lockScroll(element);
        }
    } else {
        if (scrollLockState.lockedBy === element) {
            unlockScroll();
        }
    }
}

function lockScroll(element) {
    const html = document.documentElement;
    const body = document.body;

    const scrollbarWidth = window.innerWidth - html.clientWidth;

    scrollLockState.originalStyles = {
        htmlOverflow: html.style.overflow,
        bodyOverflow: body.style.overflow,
        bodyPaddingRight: body.style.paddingRight
    };

    html.style.overflow = 'hidden';
    body.style.overflow = 'hidden';

    if (scrollbarWidth > 0) {
        body.style.paddingRight = `${scrollbarWidth}px`;
    }

    scrollLockState.lockedBy = element;
}

function unlockScroll() {
    if (scrollLockState.originalStyles === null) return;

    const html = document.documentElement;
    const body = document.body;

    html.style.overflow = scrollLockState.originalStyles.htmlOverflow;
    body.style.overflow = scrollLockState.originalStyles.bodyOverflow;
    body.style.paddingRight = scrollLockState.originalStyles.bodyPaddingRight;

    scrollLockState.originalStyles = null;
    scrollLockState.lockedBy = null;
}

export function registerItem(menuBarElement, itemElement) {
    if (!menuBarElement || !itemElement) return;

    const menuBarState = state.get(menuBarElement);
    if (!menuBarState) return;

    const items = menuBarState.items;
    items.add(itemElement);

    const sortedItems = getItems(menuBarElement);
    const firstItem = sortedItems[0];

    for (const item of sortedItems) {
        item.tabIndex = item === firstItem ? 0 : -1;
    }
}

export function unregisterItem(menuBarElement, itemElement) {
    if (!menuBarElement || !itemElement) return;

    const menuBarState = state.get(menuBarElement);
    if (!menuBarState) return;

    const items = menuBarState.items;
    const hadFocus = document.activeElement === itemElement;
    const sortedBefore = getItems(menuBarElement);
    const wasFirst = sortedBefore[0] === itemElement;

    items.delete(itemElement);

    if (items.size > 0 && (hadFocus || wasFirst)) {
        const sortedAfter = getItems(menuBarElement);
        const firstItem = sortedAfter[0];
        if (firstItem) {
            firstItem.tabIndex = 0;
            if (hadFocus) {
                firstItem.focus();
            }
        }
    }
}
