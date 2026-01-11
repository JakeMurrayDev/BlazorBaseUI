import {
    detectAnimationType,
    measureDimensions,
    waitForAnimationsToFinish,
    requestAnimationFrameAsync,
    setCssVariables,
    setDataAttribute
} from './blazor-baseui-animations.js';

const STATE_KEY = Symbol.for('BlazorBaseUI.Collapsible.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const panelStates = window[STATE_KEY];

/**
 * Helper to generate variable names based on a prefix
 */
function getVarNames(prefix) {
    const p = prefix || 'collapsible-panel';
    return {
        height: `--${p}-height`,
        width: `--${p}-width`
    };
}

/**
 * @typedef {Object} PanelState
 * @property {AbortController | null} abortController
 * @property {DotNetObjectReference} dotNetRef
 * @property {string} prefix
 */

function getOrCreateState(panel, dotNetRef, prefix) {
    let state = panelStates.get(panel);

    if (!state) {
        state = {
            abortController: null,
            dotNetRef,
            prefix: prefix || 'collapsible-panel'
        };
        panelStates.set(panel, state);
    } else {
        state.dotNetRef = dotNetRef;
        if (prefix) state.prefix = prefix;
    }

    return state;
}

export function initialize(panel, dotNetRef, initialOpen, prefix) {
    if (!panel) return;

    const state = getOrCreateState(panel, dotNetRef, prefix);
    const vars = getVarNames(state.prefix);

    if (initialOpen) {
        const dims = measureDimensions(panel);
        setCssVariables(panel, {
            [vars.height]: dims.height === 0 ? 'auto' : `${dims.height}px`,
            [vars.width]: dims.width === 0 ? 'auto' : `${dims.width}px`
        });
    }
}

export async function open(panel, skipAnimation) {
    const state = panelStates.get(panel);
    if (!state) return;

    if (state.abortController) {
        state.abortController.abort();
        state.abortController = null;
    }

    const vars = getVarNames(state.prefix);

    setDataAttribute(panel, 'ending-style', false);
    setDataAttribute(panel, 'starting-style', false);

    panel.style.removeProperty(vars.height);
    panel.style.removeProperty(vars.width);

    await new Promise(resolve => requestAnimationFrame(resolve));

    const animationType = detectAnimationType(panel);
    const dims = measureDimensions(panel);

    setCssVariables(panel, {
        [vars.height]: `${dims.height}px`,
        [vars.width]: `${dims.width}px`
    });

    if (skipAnimation || animationType === 'none') {
        setCssVariables(panel, {
            [vars.height]: 'auto',
            [vars.width]: 'auto'
        });
        invokeAnimationEnded(state.dotNetRef, 'open', true);
        return;
    }

    const abortController = new AbortController();
    state.abortController = abortController;
    const signal = abortController.signal;

    setDataAttribute(panel, 'starting-style', true);
    invokeTransitionStatusChanged(state.dotNetRef, 'starting');

    const frameOk = await requestAnimationFrameAsync(signal);
    if (!frameOk) {
        invokeAnimationEnded(state.dotNetRef, 'open', false);
        return;
    }

    setDataAttribute(panel, 'starting-style', false);

    const completed = await waitForAnimationsToFinish(panel, signal);
    if (!completed) {
        invokeAnimationEnded(state.dotNetRef, 'open', false);
        return;
    }

    if (state.abortController === abortController) {
        state.abortController = null;
    }

    setCssVariables(panel, {
        [vars.height]: 'auto',
        [vars.width]: 'auto'
    });

    invokeAnimationEnded(state.dotNetRef, 'open', true);
}

export async function close(panel) {
    const state = panelStates.get(panel);
    if (!state) return;

    if (state.abortController) {
        state.abortController.abort();
        state.abortController = null;
    }

    const vars = getVarNames(state.prefix);
    setDataAttribute(panel, 'starting-style', false);

    const dims = measureDimensions(panel);
    if (dims.height === 0 && dims.width === 0) {
        invokeAnimationEnded(state.dotNetRef, 'close', true);
        return;
    }

    setCssVariables(panel, {
        [vars.height]: `${dims.height}px`,
        [vars.width]: `${dims.width}px`
    });

    const animationType = detectAnimationType(panel);

    if (animationType === 'none') {
        setCssVariables(panel, {
            [vars.height]: '0px',
            [vars.width]: '0px'
        });
        invokeAnimationEnded(state.dotNetRef, 'close', true);
        return;
    }

    const abortController = new AbortController();
    state.abortController = abortController;
    const signal = abortController.signal;

    if (animationType === 'css-animation') {
        panel.style.removeProperty('animation-name');
    }

    const frameOk = await requestAnimationFrameAsync(signal);
    if (!frameOk) {
        invokeAnimationEnded(state.dotNetRef, 'close', false);
        return;
    }

    setDataAttribute(panel, 'ending-style', true);
    invokeTransitionStatusChanged(state.dotNetRef, 'ending');

    const completed = await waitForAnimationsToFinish(panel, signal);
    setDataAttribute(panel, 'ending-style', false);

    if (!completed) {
        invokeAnimationEnded(state.dotNetRef, 'close', false);
        return;
    }

    if (state.abortController === abortController) {
        state.abortController = null;
    }

    setCssVariables(panel, {
        [vars.height]: '0px',
        [vars.width]: '0px'
    });

    invokeAnimationEnded(state.dotNetRef, 'close', true);
}

export function updateDimensions(panel) {
    if (!panel) return;
    const state = panelStates.get(panel);
    if (!state) return;

    const vars = getVarNames(state.prefix);
    const dims = measureDimensions(panel);
    setCssVariables(panel, {
        [vars.height]: dims.height === 0 ? 'auto' : `${dims.height}px`,
        [vars.width]: dims.width === 0 ? 'auto' : `${dims.width}px`
    });
}

export function dispose(panel) {
    const state = panelStates.get(panel);
    if (!state) return;

    if (state.abortController) {
        state.abortController.abort();
    }

    panelStates.delete(panel);
}

function invokeTransitionStatusChanged(dotNetRef, status) {
    try {
        dotNetRef.invokeMethodAsync('OnTransitionStatusChanged', status);
    } catch (e) { }
}

function invokeAnimationEnded(dotNetRef, animationType, completed) {
    try {
        dotNetRef.invokeMethodAsync('OnAnimationEnded', animationType, completed);
    } catch (e) { }
}
