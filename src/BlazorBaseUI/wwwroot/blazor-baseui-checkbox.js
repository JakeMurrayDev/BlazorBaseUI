const STATE_KEY = Symbol.for('BlazorBaseUI.Checkbox.State');

/**
 * Applies optimistic visual state to the checkbox element immediately,
 * without waiting for the Blazor Server round-trip. This eliminates
 * perceived lag during rapid clicking in Interactive Server mode.
 */
function applyOptimisticState(element, checked, indeterminate) {
    element.setAttribute('aria-checked', indeterminate ? 'mixed' : checked ? 'true' : 'false');
    element.setAttribute('data-checked', checked && !indeterminate);
    element.setAttribute('data-unchecked', !checked && !indeterminate);
    element.setAttribute('data-indeterminate', indeterminate);
}

/**
 * Dispatches the toggle to Blazor by clicking the hidden input element.
 * When debouncing is active, only the final resolved state is dispatched.
 */
function dispatchToggle(state) {
    if (!state.inputElement) {
        return;
    }

    const currentInputChecked = state.inputElement.checked;
    const desiredChecked = state.optimisticChecked;

    // Only click the input if the desired state differs from its current state.
    // Each click toggles the input, so we only need one click (or none).
    if (currentInputChecked !== desiredChecked) {
        state.inputElement.click();
    }
}

export function initialize(element, inputElement, disabled, readOnly, indeterminate, checked) {
    if (!element) {
        return;
    }

    const state = {
        inputElement,
        disabled,
        readOnly,
        indeterminate,
        optimisticChecked: checked,
        debounceTimer: null,
        keydownHandler: null,
        keyupHandler: null,
        clickHandler: null,
        inputFocusHandler: null
    };

    state.inputFocusHandler = () => {
        element.focus();
    };

    if (inputElement) {
        inputElement.indeterminate = indeterminate;
        inputElement.addEventListener('focus', state.inputFocusHandler);
    }

    state.keydownHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
            return;
        }

        if (state.readOnly) {
            if (event.key === ' ' || event.key === 'Enter') {
                event.preventDefault();
            }
            return;
        }

        if (event.key === ' ') {
            event.preventDefault();
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            toggleCheckbox(element, state);
        }
    };

    state.keyupHandler = (event) => {
        if (state.disabled || state.readOnly) {
            return;
        }

        if (event.key === ' ') {
            event.preventDefault();
            toggleCheckbox(element, state);
        }
    };

    state.clickHandler = (event) => {
        if (state.disabled || state.readOnly) {
            event.preventDefault();
            return;
        }

        event.preventDefault();
        toggleCheckbox(element, state);
    };

    element.addEventListener('keydown', state.keydownHandler);
    element.addEventListener('keyup', state.keyupHandler);
    element.addEventListener('click', state.clickHandler);

    element[STATE_KEY] = state;
}

/**
 * Core toggle logic combining optimistic UI and debouncing.
 *
 * 1. Immediately flips the visual state (data attributes + aria) for instant feedback.
 * 2. Debounces the actual Blazor dispatch so rapid clicks coalesce into a single
 *    server round-trip with the final resolved state.
 */
function toggleCheckbox(element, state) {
    // Flip the optimistic state
    const nextChecked = !state.optimisticChecked;
    state.optimisticChecked = nextChecked;

    // If the checkbox was indeterminate, clicking always resolves to a definite state.
    if (state.indeterminate) {
        state.indeterminate = false;
        if (state.inputElement) {
            state.inputElement.indeterminate = false;
        }
    }

    // Apply visual feedback immediately (no server round-trip needed)
    applyOptimisticState(element, nextChecked, false);

    // Debounce the Blazor dispatch: coalesce rapid clicks into one server call.
    // 60ms is enough to catch double/triple clicks while feeling instant.
    if (state.debounceTimer !== null) {
        clearTimeout(state.debounceTimer);
    }

    state.debounceTimer = setTimeout(() => {
        state.debounceTimer = null;
        dispatchToggle(state);
    }, 60);
}

export function updateState(element, inputElement, disabled, readOnly, indeterminate, checked) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        if (state.inputElement !== inputElement) {
            if (state.inputElement && state.inputFocusHandler) {
                state.inputElement.removeEventListener('focus', state.inputFocusHandler);
            }

            state.inputElement = inputElement;

            if (state.inputElement && state.inputFocusHandler) {
                state.inputElement.addEventListener('focus', state.inputFocusHandler);
            }
        }

        state.disabled = disabled;
        state.readOnly = readOnly;
        state.indeterminate = indeterminate;

        if (state.inputElement) {
            state.inputElement.indeterminate = indeterminate;
        }

        // Reconcile optimistic state with server truth when no debounce is pending.
        // This ensures controlled checkboxes and group parents stay in sync.
        if (state.debounceTimer === null) {
            state.optimisticChecked = checked;
        }
    }
}

export function focus(element) {
    if (!element) {
        return;
    }

    element.focus();
}

export function setInputChecked(inputElement, checked) {
    if (!inputElement) {
        return;
    }

    inputElement.checked = checked;
}

export function dispose(element) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        if (state.debounceTimer !== null) {
            clearTimeout(state.debounceTimer);
        }
        if (state.inputElement && state.inputFocusHandler) {
            state.inputElement.removeEventListener('focus', state.inputFocusHandler);
        }
        if (state.keydownHandler) {
            element.removeEventListener('keydown', state.keydownHandler);
        }
        if (state.keyupHandler) {
            element.removeEventListener('keyup', state.keyupHandler);
        }
        if (state.clickHandler) {
            element.removeEventListener('click', state.clickHandler);
        }
        delete element[STATE_KEY];
    }
}
