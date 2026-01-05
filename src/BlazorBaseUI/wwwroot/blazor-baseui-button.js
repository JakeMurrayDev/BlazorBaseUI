const STATE_KEY = Symbol.for('BlazorBaseUI.Button.State');

export function initialize(element, disabled, focusableWhenDisabled, nativeButton) {
    if (!element) {
        return;
    }

    const state = {
        disabled,
        focusableWhenDisabled,
        nativeButton,
        clickHandler: null,
        pointerDownHandler: null,
        keydownHandler: null,
        keyupHandler: null
    };

    state.clickHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
            event.stopPropagation();
        }
    };

    state.pointerDownHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
        }
    };

    state.keydownHandler = (event) => {
        if (state.disabled && state.focusableWhenDisabled && event.key !== 'Tab') {
            event.preventDefault();
            return;
        }

        if (state.disabled) {
            return;
        }

        if (state.nativeButton) {
            return;
        }

        if (event.target !== event.currentTarget) {
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

        if (state.nativeButton) {
            return;
        }

        if (event.target !== event.currentTarget) {
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

    element.addEventListener('click', state.clickHandler);
    element.addEventListener('pointerdown', state.pointerDownHandler);
    element.addEventListener('keydown', state.keydownHandler);
    element.addEventListener('keyup', state.keyupHandler);

    element[STATE_KEY] = state;
}

export function updateState(element, disabled, focusableWhenDisabled, nativeButton) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        state.disabled = disabled;
        state.focusableWhenDisabled = focusableWhenDisabled;
        state.nativeButton = nativeButton;
    }
}

export function dispose(element) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        if (state.clickHandler) {
            element.removeEventListener('click', state.clickHandler);
        }
        if (state.pointerDownHandler) {
            element.removeEventListener('pointerdown', state.pointerDownHandler);
        }
        if (state.keydownHandler) {
            element.removeEventListener('keydown', state.keydownHandler);
        }
        if (state.keyupHandler) {
            element.removeEventListener('keyup', state.keyupHandler);
        }
        delete element[STATE_KEY];
    }
}
