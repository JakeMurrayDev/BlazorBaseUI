const STATE_KEY = Symbol.for("BlazorBaseUI.Label.State");

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        labelListeners: new WeakMap(),
    };
}

const state = window[STATE_KEY];

export function addLabelMouseDownListener(element) {
    if (!element || state.labelListeners.has(element)) return;

    const listener = (event) => {
        const target = event.target;
        if (target.closest("button, input, select, textarea")) {
            return;
        }
        if (!event.defaultPrevented && event.detail > 1) {
            event.preventDefault();
        }
    };

    element.addEventListener("mousedown", listener);
    state.labelListeners.set(element, listener);
}

export function removeLabelMouseDownListener(element) {
    if (!element) return;

    const listener = state.labelListeners.get(element);
    if (listener) {
        element.removeEventListener("mousedown", listener);
        state.labelListeners.delete(element);
    }
}

export function focusControlById(controlId) {
    if (!controlId) return;

    const controlElement = document.getElementById(controlId);
    if (controlElement) {
        controlElement.focus({ focusVisible: true });
    }
}
