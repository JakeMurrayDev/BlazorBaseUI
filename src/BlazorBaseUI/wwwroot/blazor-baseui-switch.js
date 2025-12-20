const STATE_KEY = Symbol.for('BlazorBaseUI.Switch.State');

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
        keydownHandler: null,
        keyupHandler: null,
        clickHandler: null
    };

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
            if (state.inputElement) {
                state.inputElement.click();
            }
        }
    };

    state.keyupHandler = (event) => {
        if (state.disabled || state.readOnly) {
            return;
        }

        if (event.key === ' ') {
            event.preventDefault();
            if (state.inputElement) {
                state.inputElement.click();
            }
        }
    };

    state.clickHandler = (event) => {
        if (state.disabled || state.readOnly) {
            event.preventDefault();
            return;
        }

        event.preventDefault();

        if (state.inputElement) {
            state.inputElement.click();
        }
    };

    element.addEventListener('keydown', state.keydownHandler);
    element.addEventListener('keyup', state.keyupHandler);
    element.addEventListener('click', state.clickHandler);

    element[STATE_KEY] = state;
}

export function updateState(element, disabled, readOnly) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        state.disabled = disabled;
        state.readOnly = readOnly;
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