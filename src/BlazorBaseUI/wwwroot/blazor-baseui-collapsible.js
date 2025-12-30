import {
    detectAnimationType,
    detectTransitionDimension,
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

const CSS_VAR_HEIGHT = '--collapsible-panel-height';
const CSS_VAR_WIDTH = '--collapsible-panel-width';

/**
 * @typedef {Object} PanelState
 * @property {AbortController | null} abortController
 * @property {DotNetObjectReference} dotNetRef
 */

/**
 * Gets or creates state for a panel.
 * @param {HTMLElement} panel
 * @param {DotNetObjectReference} dotNetRef
 * @returns {PanelState}
 */
function getOrCreateState(panel, dotNetRef) {
    let state = panelStates.get(panel);

    if (!state) {
        state = {
            abortController: null,
            dotNetRef
        };
        panelStates.set(panel, state);
    } else {
        state.dotNetRef = dotNetRef;
    }

    return state;
}

/**
 * Initializes a collapsible panel.
 * @param {HTMLElement} panel
 * @param {DotNetObjectReference} dotNetRef
 * @param {boolean} initialOpen
 */
export function initialize(panel, dotNetRef, initialOpen) {
    if (!panel) return;

    getOrCreateState(panel, dotNetRef);

    if (initialOpen) {
        const dims = measureDimensions(panel);
        setCssVariables(panel, {
            [CSS_VAR_HEIGHT]: dims.height === 0 ? 'auto' : `${dims.height}px`,
            [CSS_VAR_WIDTH]: dims.width === 0 ? 'auto' : `${dims.width}px`
        });
    }
}

/**
 * Handles the opening animation of a collapsible panel.
 * @param {HTMLElement} panel
 * @param {boolean} skipAnimation
 * @returns {Promise<void>}
 */
export async function open(panel, skipAnimation) {
    const state = panelStates.get(panel);
    if (!state) {
        console.warn('[Collapsible] Panel not initialized');
        return;
    }

    if (state.abortController) {
        state.abortController.abort();
        state.abortController = null;
    }

    setDataAttribute(panel, 'ending-style', false);
    setDataAttribute(panel, 'starting-style', false);

    panel.style.removeProperty('--collapsible-panel-height');
    panel.style.removeProperty('--collapsible-panel-width');

    await new Promise(resolve => requestAnimationFrame(resolve));

    const animationType = detectAnimationType(panel);
    const dims = measureDimensions(panel);

    setCssVariables(panel, {
        [CSS_VAR_HEIGHT]: `${dims.height}px`,
        [CSS_VAR_WIDTH]: `${dims.width}px`
    });

    if (skipAnimation || animationType === 'none') {
        setCssVariables(panel, {
            [CSS_VAR_HEIGHT]: 'auto',
            [CSS_VAR_WIDTH]: 'auto'
        });
        invokeCallback(state.dotNetRef, 'OnOpenAnimationComplete');
        return;
    }

    const abortController = new AbortController();
    state.abortController = abortController;
    const signal = abortController.signal;

    setDataAttribute(panel, 'starting-style', true);

    const frameOk = await requestAnimationFrameAsync(signal);
    if (!frameOk) return;

    setDataAttribute(panel, 'starting-style', false);

    const completed = await waitForAnimationsToFinish(panel, signal);
    if (!completed) return;

    if (state.abortController === abortController) {
        state.abortController = null;
    }

    setCssVariables(panel, {
        [CSS_VAR_HEIGHT]: 'auto',
        [CSS_VAR_WIDTH]: 'auto'
    });

    invokeCallback(state.dotNetRef, 'OnOpenAnimationComplete');
}

/**
 * Handles the closing animation of a collapsible panel.
 * @param {HTMLElement} panel
 * @returns {Promise<void>}
 */
export async function close(panel) {
    const state = panelStates.get(panel);
    if (!state) {
        console.warn('[Collapsible] Panel not initialized');
        return;
    }

    if (state.abortController) {
        state.abortController.abort();
        state.abortController = null;
    }

    setDataAttribute(panel, 'starting-style', false);

    const dims = measureDimensions(panel);
    if (dims.height === 0 && dims.width === 0) {
        invokeCallback(state.dotNetRef, 'OnCloseAnimationComplete');
        return;
    }

    setCssVariables(panel, {
        [CSS_VAR_HEIGHT]: `${dims.height}px`,
        [CSS_VAR_WIDTH]: `${dims.width}px`
    });

    const animationType = detectAnimationType(panel);

    if (animationType === 'none') {
        setCssVariables(panel, {
            [CSS_VAR_HEIGHT]: '0px',
            [CSS_VAR_WIDTH]: '0px'
        });
        invokeCallback(state.dotNetRef, 'OnCloseAnimationComplete');
        return;
    }

    const abortController = new AbortController();
    state.abortController = abortController;
    const signal = abortController.signal;

    if (animationType === 'css-animation') {
        panel.style.removeProperty('animation-name');
    }

    const frameOk = await requestAnimationFrameAsync(signal);
    if (!frameOk) return;

    setDataAttribute(panel, 'ending-style', true);

    const completed = await waitForAnimationsToFinish(panel, signal);

    setDataAttribute(panel, 'ending-style', false);

    if (!completed) return;

    if (state.abortController === abortController) {
        state.abortController = null;
    }

    setCssVariables(panel, {
        [CSS_VAR_HEIGHT]: '0px',
        [CSS_VAR_WIDTH]: '0px'
    });

    invokeCallback(state.dotNetRef, 'OnCloseAnimationComplete');
}

/**
 * Updates dimensions when content changes while open.
 * @param {HTMLElement} panel
 */
export function updateDimensions(panel) {
    if (!panel) return;

    const dims = measureDimensions(panel);
    setCssVariables(panel, {
        [CSS_VAR_HEIGHT]: dims.height === 0 ? 'auto' : `${dims.height}px`,
        [CSS_VAR_WIDTH]: dims.width === 0 ? 'auto' : `${dims.width}px`
    });
}

/**
 * Disposes of a collapsible panel's state.
 * @param {HTMLElement} panel
 */
export function dispose(panel) {
    const state = panelStates.get(panel);
    if (!state) return;

    if (state.abortController) {
        state.abortController.abort();
        state.abortController = null;
    }

    panelStates.delete(panel);
}

/**
 * Safely invokes a .NET callback.
 * @param {DotNetObjectReference} dotNetRef
 * @param {string} methodName
 */
function invokeCallback(dotNetRef, methodName) {
    try {
        dotNetRef.invokeMethodAsync(methodName);
    } catch (e) {
        // Ignore disconnection errors
    }
}