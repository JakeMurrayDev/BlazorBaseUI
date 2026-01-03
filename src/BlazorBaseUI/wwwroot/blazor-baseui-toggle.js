const STATE_KEY = Symbol.for('BlazorBaseUI.Toggle.State');
const GROUP_STATE_KEY = Symbol.for('BlazorBaseUI.ToggleGroup.State');
const GROUP_ITEM_STATE_KEY = Symbol.for('BlazorBaseUI.ToggleGroupItem.State');

export function initialize(element, disabled) {
    if (!element) {
        return;
    }

    const state = {
        disabled,
        keydownHandler: null,
        keyupHandler: null
    };

    state.keydownHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
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

        if (event.key === ' ') {
            element.click();
        }
    };

    element.addEventListener('keydown', state.keydownHandler);
    element.addEventListener('keyup', state.keyupHandler);

    element[STATE_KEY] = state;
}

export function updateState(element, disabled) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        state.disabled = disabled;
    }
}

export function focus(element) {
    if (!element) {
        return;
    }

    element.focus({ preventScroll: true });
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

export function initializeGroup(element) {
    if (!element) {
        return;
    }

    const state = {
        element
    };

    element[GROUP_STATE_KEY] = state;
}

export function disposeGroup(element) {
    if (!element) {
        return;
    }

    if (element[GROUP_STATE_KEY]) {
        delete element[GROUP_STATE_KEY];
    }
}

export function initializeGroupItem(element, orientation) {
    if (!element) {
        return;
    }

    const state = {
        orientation,
        keydownHandler: null
    };

    state.keydownHandler = (event) => {
        const isHorizontal = state.orientation === 'horizontal';
        const isVertical = state.orientation === 'vertical';

        const isArrowLeft = event.key === 'ArrowLeft';
        const isArrowRight = event.key === 'ArrowRight';
        const isArrowUp = event.key === 'ArrowUp';
        const isArrowDown = event.key === 'ArrowDown';
        const isHome = event.key === 'Home';
        const isEnd = event.key === 'End';

        const shouldPrevent =
            isHome ||
            isEnd ||
            (isHorizontal && (isArrowLeft || isArrowRight)) ||
            (isVertical && (isArrowUp || isArrowDown));

        if (shouldPrevent) {
            event.preventDefault();
        }
    };

    element.addEventListener('keydown', state.keydownHandler);

    element[GROUP_ITEM_STATE_KEY] = state;
}

export function updateGroupItemOrientation(element, orientation) {
    if (!element) {
        return;
    }

    const state = element[GROUP_ITEM_STATE_KEY];
    if (state) {
        state.orientation = orientation;
    }
}

export function disposeGroupItem(element) {
    if (!element) {
        return;
    }

    const state = element[GROUP_ITEM_STATE_KEY];
    if (state) {
        if (state.keydownHandler) {
            element.removeEventListener('keydown', state.keydownHandler);
        }
        delete element[GROUP_ITEM_STATE_KEY];
    }
}
