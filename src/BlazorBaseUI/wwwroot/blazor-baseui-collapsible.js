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
 * @property {Function | null} beforeMatchHandler
 * @property {'opening' | 'closing' | 'idle'} animationDirection
 */

function getOrCreateState(panel, dotNetRef, prefix) {
    let state = panelStates.get(panel);

    if (!state) {
        state = {
            abortController: null,
            dotNetRef,
            prefix: prefix || 'collapsible-panel',
            beforeMatchHandler: null,
            animationDirection: 'idle'
        };
        panelStates.set(panel, state);
    } else {
        state.dotNetRef = dotNetRef;
        if (prefix) state.prefix = prefix;
    }

    return state;
}

/**
 * Aborts any ongoing animation and immediately finalizes the CSS state.
 * This follows Base UI's approach where aborted animations are treated as finished
 * for CSS purposes - the abort only cancels the callback, not the visual state.
 */
function abortAndFinalize(state, panel, vars) {
    if (!state.abortController) {
        return;
    }

    state.abortController.abort();
    state.abortController = null;

    // Clear transition attributes
    setDataAttribute(panel, 'starting-style', false);
    setDataAttribute(panel, 'ending-style', false);

    // Immediately finalize CSS state based on what animation was running
    if (state.animationDirection === 'opening') {
        // Opening was interrupted - set to current measured dimensions
        const dims = measureDimensions(panel);
        setCssVariables(panel, {
            [vars.height]: dims.height > 0 ? `${dims.height}px` : 'auto',
            [vars.width]: dims.width > 0 ? `${dims.width}px` : 'auto'
        });
    } else if (state.animationDirection === 'closing') {
        // Closing was interrupted - measure current dimensions
        const dims = measureDimensions(panel);
        setCssVariables(panel, {
            [vars.height]: `${dims.height}px`,
            [vars.width]: `${dims.width}px`
        });
    }

    state.animationDirection = 'idle';
}

export function initialize(panel, dotNetRef, initialOpen, prefix) {
    if (!panel) return;

    const state = getOrCreateState(panel, dotNetRef, prefix);
    const vars = getVarNames(state.prefix);

    if (initialOpen) {
        // Use auto so the panel can resize naturally when nested content changes
        setCssVariables(panel, {
            [vars.height]: 'auto',
            [vars.width]: 'auto'
        });
    } else {
        setCssVariables(panel, {
            [vars.height]: '0px',
            [vars.width]: '0px'
        });
    }

    // Add beforematch event listener for hidden="until-found" support
    if (!state.beforeMatchHandler) {
        state.beforeMatchHandler = () => {
            invokeBeforeMatch(state.dotNetRef);
        };
        panel.addEventListener('beforematch', state.beforeMatchHandler);
    }
}

export async function open(panel, skipAnimation) {
    const state = panelStates.get(panel);
    if (!state) return;

    const vars = getVarNames(state.prefix);

    // Abort any ongoing animation and finalize its CSS state immediately
    abortAndFinalize(state, panel, vars);

    // Clear any transition attributes from previous animations
    setDataAttribute(panel, 'ending-style', false);
    setDataAttribute(panel, 'starting-style', false);

    // SYNCHRONOUSLY measure and set dimensions FIRST (Base UI approach)
    // This ensures CSS is always in a valid state
    const dims = measureDimensions(panel);
    setCssVariables(panel, {
        [vars.height]: `${dims.height}px`,
        [vars.width]: `${dims.width}px`
    });

    const animationType = detectAnimationType(panel);

    if (skipAnimation || animationType === 'none') {
        setCssVariables(panel, {
            [vars.height]: 'auto',
            [vars.width]: 'auto'
        });
        state.animationDirection = 'idle';
        invokeAnimationEnded(state.dotNetRef, 'open', true);
        return;
    }

    // Set up abort controller for animation waiting only
    const abortController = new AbortController();
    state.abortController = abortController;
    state.animationDirection = 'opening';
    const signal = abortController.signal;

    // Set starting-style for CSS transition starting point
    setDataAttribute(panel, 'starting-style', true);
    invokeTransitionStatusChanged(state.dotNetRef, 'starting');

    // Wait one frame for starting-style to be applied
    const frameOk = await requestAnimationFrameAsync(signal);
    if (!frameOk) {
        // Aborted - CSS state was already finalized by abortAndFinalize
        return;
    }

    // Remove starting-style to trigger the transition
    setDataAttribute(panel, 'starting-style', false);

    // Wait for animations to complete
    const completed = await waitForAnimationsToFinish(panel, signal);
    if (!completed) {
        // Aborted - CSS state was already finalized by abortAndFinalize
        return;
    }

    // Animation completed successfully
    if (state.abortController === abortController) {
        state.abortController = null;
    }
    state.animationDirection = 'idle';

    // Set to auto for responsive sizing
    setCssVariables(panel, {
        [vars.height]: 'auto',
        [vars.width]: 'auto'
    });

    invokeAnimationEnded(state.dotNetRef, 'open', true);
}

export async function close(panel) {
    const state = panelStates.get(panel);
    if (!state) return;

    const vars = getVarNames(state.prefix);

    // Abort any ongoing animation and finalize its CSS state immediately
    abortAndFinalize(state, panel, vars);

    // Clear starting-style from any previous animation
    setDataAttribute(panel, 'starting-style', false);

    // SYNCHRONOUSLY measure and set dimensions FIRST (Base UI approach)
    // This captures the current open height as the starting point for close animation
    const dims = measureDimensions(panel);
    if (dims.height === 0 && dims.width === 0) {
        // Already closed, nothing to animate
        state.animationDirection = 'idle';
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
        state.animationDirection = 'idle';
        invokeAnimationEnded(state.dotNetRef, 'close', true);
        return;
    }

    // Set up abort controller for animation waiting only
    const abortController = new AbortController();
    state.abortController = abortController;
    state.animationDirection = 'closing';
    const signal = abortController.signal;

    if (animationType === 'css-animation') {
        panel.style.removeProperty('animation-name');
    }

    // Wait one frame for dimensions to be applied
    const frameOk = await requestAnimationFrameAsync(signal);
    if (!frameOk) {
        // Aborted - CSS state was already finalized by abortAndFinalize
        return;
    }

    // Set ending-style to trigger the close transition
    setDataAttribute(panel, 'ending-style', true);
    invokeTransitionStatusChanged(state.dotNetRef, 'ending');

    // Wait for animations to complete
    const completed = await waitForAnimationsToFinish(panel, signal);

    // Always remove ending-style when done (even if aborted, but abort handles this too)
    setDataAttribute(panel, 'ending-style', false);

    if (!completed) {
        // Aborted - CSS state was already finalized by abortAndFinalize
        return;
    }

    // Animation completed successfully
    if (state.abortController === abortController) {
        state.abortController = null;
    }
    state.animationDirection = 'idle';

    // Set final closed state
    setCssVariables(panel, {
        [vars.height]: '0px',
        [vars.width]: '0px'
    });

    invokeAnimationEnded(state.dotNetRef, 'close', true);
}

export function dispose(panel) {
    const state = panelStates.get(panel);
    if (!state) return;

    if (state.abortController) {
        state.abortController.abort();
    }

    if (state.beforeMatchHandler) {
        panel.removeEventListener('beforematch', state.beforeMatchHandler);
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

function invokeBeforeMatch(dotNetRef) {
    try {
        dotNetRef.invokeMethodAsync('OnBeforeMatch');
    } catch (e) { }
}
