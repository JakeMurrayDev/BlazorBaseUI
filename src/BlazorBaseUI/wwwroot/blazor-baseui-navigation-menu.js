/**
 * BlazorBaseUI Navigation Menu Component
 *
 * Handles hover delays, outside click dismiss, escape key,
 * transition detection, and positioning via shared floating module.
 */

// Reference to shared floating module (loaded separately)
let floatingModule = null;

async function ensureFloatingModule() {
    if (!floatingModule) {
        floatingModule = await import('./blazor-baseui-floating.js');
    }
    return floatingModule;
}

const STATE_KEY = Symbol.for('BlazorBaseUI.NavigationMenu.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        positioners: new Map(),
        globalListenersInitialized: false
    };
}
const state = window[STATE_KEY];

function initGlobalListeners() {
    if (state.globalListenersInitialized) return;

    document.addEventListener('keydown', handleGlobalKeyDown, { capture: true });
    document.addEventListener('mousedown', handleGlobalMouseDown);
    state.globalListenersInitialized = true;
}

function handleGlobalKeyDown(e) {
    // Find the open navigation menu root
    let openRoot = null;
    for (const [id, rootState] of state.roots) {
        if (rootState.isOpen && rootState.dotNetRef) {
            openRoot = rootState;
            break;
        }
    }

    if (!openRoot) return;

    if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();
        openRoot.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
    }
}

function handleGlobalMouseDown(e) {
    for (const [id, rootState] of state.roots) {
        if (!rootState.isOpen || !rootState.dotNetRef) continue;

        // Check if the click is inside any trigger or popup for this root
        let isInside = false;

        for (const [itemValue, triggerEl] of rootState.triggerElements) {
            if (triggerEl && triggerEl.contains(e.target)) {
                isInside = true;
                break;
            }
        }

        if (!isInside && rootState.popupElement && rootState.popupElement.contains(e.target)) {
            isInside = true;
        }

        if (!isInside && rootState.viewportElement && rootState.viewportElement.contains(e.target)) {
            isInside = true;
        }

        if (!isInside && rootState.contentElements) {
            for (const [, contentEl] of rootState.contentElements) {
                if (contentEl && contentEl.contains(e.target)) {
                    isInside = true;
                    break;
                }
            }
        }

        if (!isInside) {
            rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
        }
    }
}

// --- Root Management ---

export function initializeRoot(rootId, dotNetRef, orientation, delay, closeDelay) {
    initGlobalListeners();

    const rootState = {
        rootId,
        dotNetRef,
        orientation,
        delay: delay ?? 200,
        closeDelay: closeDelay ?? 300,
        isOpen: false,
        triggerElements: new Map(),
        popupElement: null,
        viewportElement: null,
        openTimer: null,
        closeTimer: null
    };

    state.roots.set(rootId, rootState);
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        clearTimeout(rootState.openTimer);
        clearTimeout(rootState.closeTimer);

        // Remove hover listeners from all triggers
        for (const [itemValue, triggerEl] of rootState.triggerElements) {
            if (triggerEl) {
                removeHoverListeners(rootState, itemValue, triggerEl);
            }
        }

        // Remove hover listeners from all content elements
        if (rootState.contentElements) {
            for (const [, contentEl] of rootState.contentElements) {
                if (contentEl) {
                    removeContentHoverListeners(contentEl);
                }
            }
        }

        // Remove hover listeners from popup element
        if (rootState.popupElement) {
            removePopupHoverListeners(rootState.popupElement);
        }

        // Remove hover listeners from viewport element
        if (rootState.viewportElement) {
            removeViewportHoverListeners(rootState.viewportElement);
        }

        state.roots.delete(rootId);
    }
}

export function setRootValue(rootId, value) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.isOpen = value != null;
}

// --- Trigger Element Management ---

export function setTriggerElement(rootId, itemValue, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    // Remove old listeners if replacing
    const oldElement = rootState.triggerElements.get(itemValue);
    if (oldElement) {
        removeHoverListeners(rootState, itemValue, oldElement);
    }

    rootState.triggerElements.set(itemValue, element);
    addHoverListeners(rootState, itemValue, element);
}

export function disposeTriggerElement(rootId, itemValue) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    const element = rootState.triggerElements.get(itemValue);
    if (element) {
        removeHoverListeners(rootState, itemValue, element);
    }
    rootState.triggerElements.delete(itemValue);
}

function addHoverListeners(rootState, itemValue, element) {
    const onEnter = () => {
        clearTimeout(rootState.closeTimer);
        rootState.closeTimer = null;

        rootState.openTimer = setTimeout(() => {
            rootState.dotNetRef.invokeMethodAsync('OnHoverOpen', itemValue).catch(() => { });
        }, rootState.delay);
    };

    const onLeave = () => {
        clearTimeout(rootState.openTimer);
        rootState.openTimer = null;

        rootState.closeTimer = setTimeout(() => {
            rootState.dotNetRef.invokeMethodAsync('OnHoverClose').catch(() => { });
        }, rootState.closeDelay);
    };

    element._navMenuEnter = onEnter;
    element._navMenuLeave = onLeave;
    element.addEventListener('mouseenter', onEnter);
    element.addEventListener('mouseleave', onLeave);
}

function removeHoverListeners(rootState, itemValue, element) {
    if (element._navMenuEnter) {
        element.removeEventListener('mouseenter', element._navMenuEnter);
        delete element._navMenuEnter;
    }
    if (element._navMenuLeave) {
        element.removeEventListener('mouseleave', element._navMenuLeave);
        delete element._navMenuLeave;
    }
}

// --- Content Element Management ---

export function setContentElement(rootId, itemValue, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    if (!rootState.contentElements) {
        rootState.contentElements = new Map();
    }

    const oldElement = rootState.contentElements.get(itemValue);
    if (oldElement) {
        removeContentHoverListeners(oldElement);
    }

    const onEnter = () => {
        clearTimeout(rootState.closeTimer);
        rootState.closeTimer = null;
    };
    const onLeave = () => {
        rootState.closeTimer = setTimeout(() => {
            rootState.dotNetRef.invokeMethodAsync('OnHoverClose').catch(() => { });
        }, rootState.closeDelay);
    };

    element._navMenuContentEnter = onEnter;
    element._navMenuContentLeave = onLeave;
    element.addEventListener('mouseenter', onEnter);
    element.addEventListener('mouseleave', onLeave);

    rootState.contentElements.set(itemValue, element);
}

export function disposeContentElement(rootId, itemValue) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !rootState.contentElements) return;

    const element = rootState.contentElements.get(itemValue);
    if (element) {
        removeContentHoverListeners(element);
    }
    rootState.contentElements.delete(itemValue);
}

function removeContentHoverListeners(element) {
    if (element._navMenuContentEnter) {
        element.removeEventListener('mouseenter', element._navMenuContentEnter);
        delete element._navMenuContentEnter;
    }
    if (element._navMenuContentLeave) {
        element.removeEventListener('mouseleave', element._navMenuContentLeave);
        delete element._navMenuContentLeave;
    }
}

// --- Popup / Viewport Element Management ---

export function setPopupElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    // Clean up old popup listeners
    if (rootState.popupElement) {
        removePopupHoverListeners(rootState.popupElement);
    }

    rootState.popupElement = element;

    // Cancel close timer when entering popup area
    if (element) {
        const onEnter = () => {
            clearTimeout(rootState.closeTimer);
            rootState.closeTimer = null;
        };
        const onLeave = () => {
            rootState.closeTimer = setTimeout(() => {
                rootState.dotNetRef.invokeMethodAsync('OnHoverClose').catch(() => { });
            }, rootState.closeDelay);
        };

        element._navMenuPopupEnter = onEnter;
        element._navMenuPopupLeave = onLeave;
        element.addEventListener('mouseenter', onEnter);
        element.addEventListener('mouseleave', onLeave);
    }
}

function removePopupHoverListeners(element) {
    if (element._navMenuPopupEnter) {
        element.removeEventListener('mouseenter', element._navMenuPopupEnter);
        delete element._navMenuPopupEnter;
    }
    if (element._navMenuPopupLeave) {
        element.removeEventListener('mouseleave', element._navMenuPopupLeave);
        delete element._navMenuPopupLeave;
    }
}

export function setViewportElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    // Clean up old viewport listeners
    if (rootState.viewportElement) {
        removeViewportHoverListeners(rootState.viewportElement);
    }

    rootState.viewportElement = element;

    // Also keep open when hovering viewport
    if (element) {
        const onEnter = () => {
            clearTimeout(rootState.closeTimer);
            rootState.closeTimer = null;
        };
        const onLeave = () => {
            rootState.closeTimer = setTimeout(() => {
                rootState.dotNetRef.invokeMethodAsync('OnHoverClose').catch(() => { });
            }, rootState.closeDelay);
        };

        element._navMenuViewportEnter = onEnter;
        element._navMenuViewportLeave = onLeave;
        element.addEventListener('mouseenter', onEnter);
        element.addEventListener('mouseleave', onLeave);
    }
}

function removeViewportHoverListeners(element) {
    if (element._navMenuViewportEnter) {
        element.removeEventListener('mouseenter', element._navMenuViewportEnter);
        delete element._navMenuViewportEnter;
    }
    if (element._navMenuViewportLeave) {
        element.removeEventListener('mouseleave', element._navMenuViewportLeave);
        delete element._navMenuViewportLeave;
    }
}

// --- Positioning ---

if (state.positionerIdCounter == null) {
    state.positionerIdCounter = 0;
}

export async function initializePositioner(
    positionerElement,
    anchorElement,
    side,
    align,
    sideOffset,
    alignOffset,
    collisionPadding,
    collisionBoundary,
    arrowPadding,
    arrowElement,
    sticky,
    positionMethod,
    disableAnchorTracking,
    collisionAvoidance
) {
    const floating = await ensureFloatingModule();
    const id = `nav-pos-${++state.positionerIdCounter}`;

    const positionerState = {
        id,
        positionerElement,
        anchorElement,
        cleanup: null,
        arrowElement,
        options: { side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, collisionAvoidance }
    };

    state.positioners.set(id, positionerState);

    await floating.computeAndApplyPosition(
        positionerElement,
        anchorElement,
        positionerState.options
    );

    if (!disableAnchorTracking) {
        positionerState.cleanup = floating.autoUpdate(
            anchorElement,
            positionerElement,
            () => {
                floating.computeAndApplyPosition(
                    positionerState.positionerElement,
                    positionerState.anchorElement,
                    { ...positionerState.options, arrowElement: positionerState.arrowElement }
                );
            }
        );
    }

    return id;
}

export async function updatePosition(
    positionerId,
    anchorElement,
    side,
    align,
    sideOffset,
    alignOffset,
    collisionPadding,
    collisionBoundary,
    arrowPadding,
    arrowElement,
    sticky,
    positionMethod,
    collisionAvoidance
) {
    const positionerState = state.positioners.get(positionerId);
    if (!positionerState) return;

    positionerState.arrowElement = arrowElement;
    positionerState.anchorElement = anchorElement;
    positionerState.options = { side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, collisionAvoidance };

    const floating = await ensureFloatingModule();
    await floating.computeAndApplyPosition(
        positionerState.positionerElement,
        anchorElement,
        positionerState.options
    );
}

export function disposePositioner(positionerId) {
    const positionerState = state.positioners.get(positionerId);
    if (!positionerState) return;

    if (positionerState.cleanup) {
        positionerState.cleanup();
    }

    state.positioners.delete(positionerId);
}
