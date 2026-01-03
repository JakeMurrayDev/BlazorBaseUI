const STATE_KEY = Symbol.for('BlazorBaseUI.Radio.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = { initialized: true };
}

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

export function initializeGroup(element) {
    if (!element) {
        return;
    }

    const groupState = {
        element,
        keydownHandler: null
    };

    // Handle keydown at the group level to prevent default for arrow keys
    // Using capture phase to ensure we catch the event before Blazor
    groupState.keydownHandler = (e) => {
        const arrowKeys = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'];
        
        if (arrowKeys.includes(e.key)) {
            // Prevent the default browser behavior (scrolling)
            e.preventDefault();
        }
        
        if (e.key === ' ') {
            e.preventDefault();
        }
    };

    element.addEventListener('keydown', groupState.keydownHandler, { capture: true });
    element[STATE_KEY] = groupState;
}

export function disposeGroup(element) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        if (state.keydownHandler) {
            element.removeEventListener('keydown', state.keydownHandler, { capture: true });
        }
        delete element[STATE_KEY];
    }
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
