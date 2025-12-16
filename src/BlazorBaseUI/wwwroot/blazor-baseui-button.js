const STATE_KEY = Symbol.for('BlazorBaseUI.Button.State');

export function initialize(element, disabled, focusableWhenDisabled) {
    if (!element) {
        return;
    }

    const state = {
        disabled,
        focusableWhenDisabled,
        keydownHandler: null,
        keyupHandler: null
    };

    state.keydownHandler = (event) => {
        if (state.disabled && state.focusableWhenDisabled && event.key !== 'Tab') {
            event.preventDefault();
            return;
        }

        if (state.disabled) {
            return;
        }

        const isValidLink = element.tagName === 'A' && element.href;
        if (isValidLink) {
            return;
        }

        const isEnterKey = event.key === 'Enter';
        const isSpaceKey = event.key === ' ';

        if (isSpaceKey || isEnterKey) {
            event.preventDefault();
            if (isEnterKey) {
                element.click();
            }
        }
    };

    state.keyupHandler = (event) => {
        if (state.disabled) {
            return;
        }

        const isValidLink = element.tagName === 'A' && element.href;
        if (isValidLink) {
            return;
        }

        if (event.key === ' ') {
            element.click();
        }
    };

    element.addEventListener('keydown', state.keydownHandler);
    element.addEventListener('keyup', state.keyupHandler);

    element[STATE_KEY] = state;
}

export function updateState(element, disabled, focusableWhenDisabled) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        state.disabled = disabled;
        state.focusableWhenDisabled = focusableWhenDisabled;
    }
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
        delete element[STATE_KEY];
    }
}