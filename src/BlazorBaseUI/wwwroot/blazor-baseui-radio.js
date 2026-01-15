const STATE_KEY = Symbol.for('BlazorBaseUI.Radio.State');
const GROUP_STATE_KEY = Symbol.for('BlazorBaseUI.RadioGroup.State');

if (!window[GROUP_STATE_KEY]) {
    window[GROUP_STATE_KEY] = new WeakMap();
}
const groupState = window[GROUP_STATE_KEY];

export function initialize(element, inputElement, disabled, readOnly) {
    if (!element) {
        return;
    }

    const state = {
        inputElement,
        disabled,
        readOnly,
        keydownHandler: null
    };

    // Set up keyboard handler that prevents default for arrow keys
    state.keydownHandler = (e) => {
        if (state.disabled || state.readOnly) {
            return;
        }

        // Arrow keys should prevent default to stop browser scrolling
        const arrowKeys = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'];
        
        if (arrowKeys.includes(e.key)) {
            e.preventDefault();
        }
        
        // Space key should also prevent default (page scroll)
        if (e.key === ' ') {
            e.preventDefault();
        }
    };

    element.addEventListener('keydown', state.keydownHandler);
    element[STATE_KEY] = state;
}

export function updateState(element, inputElement, disabled, readOnly) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        state.disabled = disabled;
        state.readOnly = readOnly;
        state.inputElement = inputElement;
    }
}

export function focus(element) {
    if (!element) {
        return;
    }

    element.focus({ preventScroll: true });
}

export function blur(element) {
    if (!element) {
        return;
    }

    element.blur();
}

export function dispose(element) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        if (state.keydownHandler) {
            element.removeEventListener('keydown', state.keydownHandler);
        }
        delete element[STATE_KEY];
    }
}

export function initializeGroup(element, dotNetRef) {
    if (!element) {
        return;
    }

    const state = {
        element,
        dotNetRef,
        items: new Set(),
        keydownHandler: null
    };

    state.keydownHandler = (e) => {
        const arrowKeys = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'];

        if (arrowKeys.includes(e.key)) {
            e.preventDefault();
        }

        if (e.key === ' ') {
            e.preventDefault();
        }
    };

    element.addEventListener('keydown', state.keydownHandler, { capture: true });
    groupState.set(element, state);
}

export function disposeGroup(element) {
    if (!element) {
        return;
    }

    const state = groupState.get(element);
    if (state) {
        if (state.keydownHandler) {
            element.removeEventListener('keydown', state.keydownHandler, { capture: true });
        }
        groupState.delete(element);
    }
}

export function registerRadio(groupElement, radioElement, value) {
    if (!groupElement || !radioElement) {
        return;
    }

    const state = groupState.get(groupElement);
    if (!state) {
        return;
    }

    for (const item of state.items) {
        if (item.element === radioElement) {
            item.value = value;
            updateTabIndexes(groupElement);
            return;
        }
    }

    state.items.add({ element: radioElement, value });
    updateTabIndexes(groupElement);
}

function updateTabIndexes(groupElement) {
    const items = getOrderedRadios(groupElement);
    if (items.length === 0) {
        return;
    }

    const hasChecked = items.some(item => item.element.getAttribute('aria-checked') === 'true');
    const firstEnabled = items.find(item => !isRadioDisabled(item.element));

    for (const item of items) {
        const isChecked = item.element.getAttribute('aria-checked') === 'true';
        const isDisabled = isRadioDisabled(item.element);

        if (isDisabled) {
            item.element.tabIndex = -1;
        } else if (isChecked) {
            item.element.tabIndex = 0;
        } else if (!hasChecked && item === firstEnabled) {
            item.element.tabIndex = 0;
        } else {
            item.element.tabIndex = -1;
        }
    }
}

export function unregisterRadio(groupElement, radioElement) {
    if (!groupElement || !radioElement) {
        return;
    }

    const state = groupState.get(groupElement);
    if (!state) {
        return;
    }

    for (const item of state.items) {
        if (item.element === radioElement) {
            state.items.delete(item);
            updateTabIndexes(groupElement);
            return;
        }
    }
}

function getOrderedRadios(groupElement) {
    const state = groupState.get(groupElement);
    if (!state) {
        return [];
    }

    const items = Array.from(state.items).filter(item => document.contains(item.element));
    items.sort((a, b) => {
        const position = a.element.compareDocumentPosition(b.element);
        if (position & Node.DOCUMENT_POSITION_FOLLOWING) return -1;
        if (position & Node.DOCUMENT_POSITION_PRECEDING) return 1;
        return 0;
    });
    return items;
}

function isRadioDisabled(radioElement) {
    return radioElement.hasAttribute('data-disabled');
}

export async function navigateToPrevious(groupElement, currentElement) {
    const state = groupState.get(groupElement);
    if (!state) {
        return false;
    }

    const items = getOrderedRadios(groupElement);
    const currentIndex = items.findIndex(item => item.element === currentElement);
    if (currentIndex < 0) {
        return false;
    }

    for (let i = currentIndex - 1; i >= 0; i--) {
        if (!isRadioDisabled(items[i].element)) {
            items[i].element.focus({ preventScroll: true });
            await state.dotNetRef.invokeMethodAsync('OnNavigateToRadio', items[i].value);
            return true;
        }
    }

    for (let i = items.length - 1; i > currentIndex; i--) {
        if (!isRadioDisabled(items[i].element)) {
            items[i].element.focus({ preventScroll: true });
            await state.dotNetRef.invokeMethodAsync('OnNavigateToRadio', items[i].value);
            return true;
        }
    }

    return false;
}

export async function navigateToNext(groupElement, currentElement) {
    const state = groupState.get(groupElement);
    if (!state) {
        return false;
    }

    const items = getOrderedRadios(groupElement);
    const currentIndex = items.findIndex(item => item.element === currentElement);
    if (currentIndex < 0) {
        return false;
    }

    for (let i = currentIndex + 1; i < items.length; i++) {
        if (!isRadioDisabled(items[i].element)) {
            items[i].element.focus({ preventScroll: true });
            await state.dotNetRef.invokeMethodAsync('OnNavigateToRadio', items[i].value);
            return true;
        }
    }

    for (let i = 0; i < currentIndex; i++) {
        if (!isRadioDisabled(items[i].element)) {
            items[i].element.focus({ preventScroll: true });
            await state.dotNetRef.invokeMethodAsync('OnNavigateToRadio', items[i].value);
            return true;
        }
    }

    return false;
}

export function getFirstEnabledRadio(groupElement) {
    const items = getOrderedRadios(groupElement);
    for (const item of items) {
        if (!isRadioDisabled(item.element)) {
            return item.element;
        }
    }
    return null;
}

export function isFirstEnabledRadio(groupElement, radioElement) {
    const firstEnabled = getFirstEnabledRadio(groupElement);
    return firstEnabled === radioElement;
}

export function isBlurWithinGroup(groupElement) {
    if (!groupElement) {
        return false;
    }

    const activeElement = document.activeElement;
    if (!activeElement) {
        return false;
    }

    return groupElement.contains(activeElement);
}
