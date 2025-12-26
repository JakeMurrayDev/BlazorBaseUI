const STATE_KEY = Symbol.for('BlazorBaseUI.Checkbox.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = { initialized: true };
}

export function initialize(element, inputElement, disabled, readOnly, indeterminate) {
    if (!element) {
        return;
    }

    const state = {
        inputElement,
        disabled,
        readOnly,
        indeterminate,
        keydownHandler: null,
        keyupHandler: null,
        clickHandler: null
    };

    if (inputElement) {
        inputElement.indeterminate = indeterminate;
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

export function updateState(element, inputElement, disabled, readOnly, indeterminate) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        state.disabled = disabled;
        state.readOnly = readOnly;
        state.indeterminate = indeterminate;

        if (inputElement) {
            inputElement.indeterminate = indeterminate;
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

export function setInputIndeterminate(inputElement, indeterminate) {
    if (!inputElement) {
        return;
    }

    inputElement.indeterminate = indeterminate;
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