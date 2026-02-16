/**
 * BlazorBaseUI Popover Component
 *
 * Popover-specific functionality that builds on the shared floating infrastructure.
 */

import { acquireScrollLock } from './blazor-baseui-scroll-lock.js';

// Reference to shared floating module (loaded separately)
let floatingModule = null;

async function ensureFloatingModule() {
    if (!floatingModule) {
        floatingModule = await import('./blazor-baseui-floating.js');
    }
    return floatingModule;
}

const STATE_KEY = Symbol.for('BlazorBaseUI.Popover.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        positioners: new Map(),
        popups: new WeakMap(),
        globalListenersInitialized: false
    };
}
const state = window[STATE_KEY];

function initGlobalListeners() {
    if (state.globalListenersInitialized) return;

    document.addEventListener('keydown', handleGlobalKeyDown);
    document.addEventListener('mousedown', handleGlobalMouseDown);
    state.globalListenersInitialized = true;
}

// ============================================================================
// Hover Interaction Support
// ============================================================================

export async function initializeHoverInteraction(rootId, triggerElement, openDelay, closeDelay) {
    let rootState = state.roots.get(rootId);

    // If root state doesn't exist yet, wait briefly for it to be initialized
    if (!rootState) {
        await new Promise(resolve => setTimeout(resolve, 50));
        rootState = state.roots.get(rootId);
        if (!rootState) return;
    }

    // Store the trigger element if provided
    if (triggerElement) {
        rootState.triggerElement = triggerElement;
    }

    if (!rootState.triggerElement) return;

    const floating = await ensureFloatingModule();

    // Clean up existing hover interaction
    if (rootState.hoverInteraction) {
        rootState.hoverInteraction.cleanup();
    }

    rootState.hoverInteraction = floating.createHoverInteraction({
        interactionId: `popover-hover-${rootId}`,
        triggerElement: rootState.triggerElement,
        floatingElement: rootState.popupElement,
        openDelay: openDelay || 0,
        closeDelay: closeDelay || 0,
        mouseOnly: true,
        useSafePolygon: true,
        safePolygonOptions: { blockPointerEvents: false },
        onOpen: (reason) => {
            if (rootState.dotNetRef && !rootState.isOpen) {
                rootState.dotNetRef.invokeMethodAsync('OnHoverOpen').catch(() => { });
            }
        },
        onClose: (reason) => {
            if (rootState.dotNetRef && rootState.isOpen) {
                rootState.dotNetRef.invokeMethodAsync('OnHoverClose').catch(() => { });
            }
        }
    });
}

export function disposeHoverInteraction(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState?.hoverInteraction) {
        rootState.hoverInteraction.cleanup();
        rootState.hoverInteraction = null;
    }
}

export function updateHoverInteractionFloatingElement(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState?.hoverInteraction && rootState.popupElement) {
        rootState.hoverInteraction.setFloatingElement(rootState.popupElement);
    }
}

export function setHoverInteractionOpen(rootId, isOpen) {
    const rootState = state.roots.get(rootId);
    if (rootState?.hoverInteraction) {
        rootState.hoverInteraction.setOpen(isOpen);
    }
}

function handleGlobalKeyDown(e) {
    if (e.key !== 'Escape') return;

    // Close the topmost open popover
    for (const [id, rootState] of state.roots) {
        if (rootState.isOpen && rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
            break;
        }
    }
}

function handleGlobalMouseDown(e) {
    // Check each popover root to see if click was outside
    for (const [id, rootState] of state.roots) {
        if (!rootState.isOpen || !rootState.dotNetRef) continue;

        const { triggerElement, popupElement } = rootState;

        let clickedInsidePopup = false;
        let clickedOnTrigger = false;
        let clickedOnBackdrop = false;

        // Check if clicked inside any popup with role="dialog"
        if (popupElement && popupElement.contains(e.target)) {
            clickedInsidePopup = true;
        }

        // Check if clicked on the trigger
        if (triggerElement && triggerElement.contains(e.target)) {
            clickedOnTrigger = true;
        }

        // Check if clicked on a backdrop
        const backdrops = document.querySelectorAll('[role="presentation"]');
        for (const backdrop of backdrops) {
            if (backdrop === e.target || backdrop.contains(e.target)) {
                clickedOnBackdrop = true;
                break;
            }
        }

        // If click was outside this popover's elements, close it
        if (!clickedInsidePopup && !clickedOnTrigger && !clickedOnBackdrop) {
            rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
        }
    }
}

// ============================================================================
// Root Management
// ============================================================================

export function initializeRoot(rootId, dotNetRef, modal) {
    initGlobalListeners();

    state.roots.set(rootId, {
        dotNetRef,
        isOpen: false,
        modal: modal || 'false',
        triggerElement: null,
        positionerElement: null,
        popupElement: null,
        hoverInteraction: null,
        focusTrapCleanup: null,
        focusOutCleanup: null,
        releaseScrollLock: null,
        initialFocusElement: null,
        finalFocusElement: null,
        transitionCleanup: null,
        fallbackTimeoutId: null,
        pendingOpen: false,
        openReason: null
    });
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        // Clean up hover interaction
        if (rootState.hoverInteraction) {
            rootState.hoverInteraction.cleanup();
        }

        // Clean up focus trap
        cleanupFocusTrap(rootState);

        // Clean up focusout listener
        cleanupFocusOutListener(rootState);

        // Clean up transition listener
        cleanupTransition(rootState);

        // Release scroll lock
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }
    }
    state.roots.delete(rootId);
}

export function setRootOpen(rootId, isOpen, reason) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.isOpen = isOpen;
    rootState.pendingOpen = isOpen;
    rootState.openReason = reason;

    // Sync with hover interaction
    if (rootState.hoverInteraction) {
        rootState.hoverInteraction.setOpen(isOpen);
    }

    if (isOpen) {
        // Acquire scroll lock for modal popovers
        if (rootState.modal === 'true') {
            rootState.releaseScrollLock = acquireScrollLock(rootState.popupElement);
        }

        waitForPopupAndStartTransition(rootState, isOpen);
    } else {
        // Release scroll lock
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }

        // Clean up focus management
        cleanupFocusTrap(rootState);
        cleanupFocusOutListener(rootState);

        // Return focus to trigger or final focus element (skip for hover-opened popovers)
        if (reason !== 'trigger-hover') {
            returnFocus(rootState);
        }

        startTransition(rootState, isOpen);
    }
}

// ============================================================================
// Focus Management
// ============================================================================

const focusableSelector = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';

function setupFocusTrap(rootState) {
    const popupElement = rootState.popupElement;
    if (!popupElement) return;

    cleanupFocusTrap(rootState);

    const handleKeyDown = (e) => {
        if (e.key !== 'Tab') return;

        const focusableElements = popupElement.querySelectorAll(focusableSelector);
        const focusableArray = Array.from(focusableElements).filter(el =>
            !el.hasAttribute('disabled') && el.offsetParent !== null
        );

        if (focusableArray.length === 0) {
            e.preventDefault();
            return;
        }

        const firstFocusable = focusableArray[0];
        const lastFocusable = focusableArray[focusableArray.length - 1];

        if (e.shiftKey) {
            if (document.activeElement === firstFocusable || document.activeElement === popupElement) {
                e.preventDefault();
                lastFocusable.focus();
            }
        } else {
            if (document.activeElement === lastFocusable) {
                e.preventDefault();
                firstFocusable.focus();
            }
        }
    };

    popupElement.addEventListener('keydown', handleKeyDown);

    rootState.focusTrapCleanup = () => {
        popupElement.removeEventListener('keydown', handleKeyDown);
    };
}

function cleanupFocusTrap(rootState) {
    if (rootState.focusTrapCleanup) {
        rootState.focusTrapCleanup();
        rootState.focusTrapCleanup = null;
    }
}

function focusPopup(rootState, popupElement) {
    if (!popupElement) return;

    // Use custom initial focus element if provided
    if (rootState.initialFocusElement) {
        rootState.initialFocusElement.focus();
        return;
    }

    // Find the first focusable element inside the popup that is not disabled or hidden
    const candidates = popupElement.querySelectorAll(focusableSelector);
    const firstFocusable = Array.from(candidates).find(el =>
        !el.hasAttribute('disabled') && el.offsetParent !== null
    );

    if (firstFocusable) {
        firstFocusable.focus();
    } else {
        // If no focusable element, focus the popup itself
        popupElement.focus();
    }
}

function returnFocus(rootState) {
    // Use custom final focus element if provided, otherwise trigger element
    const focusTarget = rootState.finalFocusElement || rootState.triggerElement;

    if (focusTarget) {
        requestAnimationFrame(() => {
            focusTarget.focus();
        });
    }
}

function setupFocusOutListener(rootState) {
    const popupElement = rootState.popupElement;
    if (!popupElement) return;

    cleanupFocusOutListener(rootState);

    const handleFocusOut = (e) => {
        if (!rootState.isOpen) return;

        const relatedTarget = e.relatedTarget;

        // If focus moved to null (e.g., outside browser), don't close
        if (!relatedTarget) return;

        const { triggerElement, popupElement: popup } = rootState;

        const isInsidePopup = popup && popup.contains(relatedTarget);
        const isOnTrigger = triggerElement && triggerElement.contains(relatedTarget);

        if (!isInsidePopup && !isOnTrigger && rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnFocusOut').catch(() => { });
        }
    };

    popupElement.addEventListener('focusout', handleFocusOut);

    rootState.focusOutCleanup = () => {
        popupElement.removeEventListener('focusout', handleFocusOut);
    };
}

function cleanupFocusOutListener(rootState) {
    if (rootState.focusOutCleanup) {
        rootState.focusOutCleanup();
        rootState.focusOutCleanup = null;
    }
}

export function setInitialFocusElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.initialFocusElement = element;

        // If popup is already open, focus the element now
        if (rootState.isOpen && element) {
            element.focus();
        }
    }
}

export function setFinalFocusElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.finalFocusElement = element;
    }
}

export function focusElement(element) {
    if (!element) return;
    element.focus();
}

// ============================================================================
// Transition Handling
// ============================================================================

function waitForPopupAndStartTransition(rootState, isOpen) {
    const popupElement = rootState.popupElement;

    if (popupElement) {
        startTransition(rootState, isOpen);
        return;
    }

    let attempts = 0;
    const maxAttempts = 10;

    function checkForPopup() {
        attempts++;
        const element = rootState.popupElement;

        if (element) {
            // Update hover interaction with the new popup element
            if (rootState.hoverInteraction) {
                rootState.hoverInteraction.setFloatingElement(element);
            }
            if (rootState.pendingOpen === isOpen) {
                startTransition(rootState, isOpen);
            }
        } else if (attempts < maxAttempts && rootState.pendingOpen === isOpen) {
            requestAnimationFrame(checkForPopup);
        } else if (rootState.dotNetRef && rootState.pendingOpen === isOpen) {
            rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
        }
    }

    requestAnimationFrame(checkForPopup);
}

async function startTransition(rootState, isOpen) {
    const popupElement = rootState.popupElement;

    if (!popupElement) {
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
        return;
    }

    const floating = await ensureFloatingModule();
    const hasTransition = floating.checkForTransitionOrAnimation(popupElement);

    if (isOpen) {
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                if (rootState.pendingOpen !== isOpen) {
                    return;
                }

                // Skip focus management for hover-opened popovers
                const isHoverOpen = rootState.openReason === 'trigger-hover';

                if (!isHoverOpen) {
                    // Set up focus trap for modal/trap-focus popovers
                    if (rootState.modal === 'true' || rootState.modal === 'trap-focus') {
                        setupFocusTrap(rootState);
                    }

                    // Set up focusout listener for non-modal popovers
                    if (rootState.modal === 'false') {
                        setupFocusOutListener(rootState);
                    }

                    // Focus the popup or initial focus element
                    focusPopup(rootState, popupElement);
                }

                if (hasTransition) {
                    setupTransitionEndListener(rootState, isOpen, floating);
                }
                if (rootState.dotNetRef) {
                    rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
                }
            });
        });
    } else {
        if (hasTransition) {
            setupTransitionEndListener(rootState, isOpen, floating);
        } else {
            if (rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
            }
        }
    }
}

function cleanupTransition(rootState) {
    if (rootState.transitionCleanup) {
        rootState.transitionCleanup();
        rootState.transitionCleanup = null;
    }
    if (rootState.fallbackTimeoutId) {
        clearTimeout(rootState.fallbackTimeoutId);
        rootState.fallbackTimeoutId = null;
    }
}

function setupTransitionEndListener(rootState, isOpen, floating) {
    const popupElement = rootState.popupElement;
    if (!popupElement) return;

    cleanupTransition(rootState);

    let called = false;
    const handleEnd = (event) => {
        if (event.target !== popupElement) return;
        if (called) return;
        called = true;
        cleanup();
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
    };

    const cleanup = () => {
        popupElement.removeEventListener('transitionend', handleEnd);
        popupElement.removeEventListener('animationend', handleEnd);
        if (rootState.fallbackTimeoutId) {
            clearTimeout(rootState.fallbackTimeoutId);
            rootState.fallbackTimeoutId = null;
        }
        rootState.transitionCleanup = null;
    };

    popupElement.addEventListener('transitionend', handleEnd);
    popupElement.addEventListener('animationend', handleEnd);

    rootState.transitionCleanup = cleanup;

    const fallbackTimeout = floating.getMaxTransitionDuration(popupElement);
    rootState.fallbackTimeoutId = setTimeout(() => {
        if (!called && rootState.dotNetRef) {
            called = true;
            cleanup();
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
    }, fallbackTimeout);
}

// ============================================================================
// Element References
// ============================================================================

export function setTriggerElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.triggerElement = element;
    }
}

export function setPopupElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.popupElement = element;
        // Update hover interaction with the new popup element
        if (rootState.hoverInteraction && element) {
            rootState.hoverInteraction.setFloatingElement(element);
        }
    }
}

// ============================================================================
// Positioning (delegated to shared floating module)
// ============================================================================

export async function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking, collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback) {
    const floating = await ensureFloatingModule();

    const positionerId = await floating.initializePositioner({
        positionerElement,
        triggerElement,
        side,
        align,
        sideOffset,
        alignOffset,
        collisionPadding,
        collisionBoundary: collisionBoundary || 'clipping-ancestors',
        arrowPadding,
        arrowElement,
        sticky: sticky || false,
        positionMethod: positionMethod || 'fixed',
        disableAnchorTracking: disableAnchorTracking || false,
        collisionAvoidance: {
            side: collisionAvoidanceSide || 'flip',
            align: collisionAvoidanceAlign || 'shift',
            fallbackAxisSide: collisionAvoidanceFallback || 'none'
        }
    });

    if (positionerId) {
        state.positioners.set(positionerId, { positionerId });
    }

    return positionerId;
}

export async function updatePosition(positionerId, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback) {
    const floating = await ensureFloatingModule();

    await floating.updatePositioner(positionerId, {
        triggerElement,
        side,
        align,
        sideOffset,
        alignOffset,
        collisionPadding,
        collisionBoundary: collisionBoundary || 'clipping-ancestors',
        arrowPadding,
        arrowElement,
        sticky: sticky || false,
        positionMethod: positionMethod || 'fixed',
        collisionAvoidance: {
            side: collisionAvoidanceSide || 'flip',
            align: collisionAvoidanceAlign || 'shift',
            fallbackAxisSide: collisionAvoidanceFallback || 'none'
        }
    });
}

export async function disposePositioner(positionerId) {
    const floating = await ensureFloatingModule();
    floating.disposePositioner(positionerId);
    state.positioners.delete(positionerId);
}

// ============================================================================
// Popup Management
// ============================================================================

export function initializePopup(rootId, popupElement, dotNetRef, modal, initialFocusElement, finalFocusElement) {
    if (!popupElement) return;

    // Store modal and focus elements on root state
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.modal = modal || 'false';
        rootState.initialFocusElement = initialFocusElement || null;
        rootState.finalFocusElement = finalFocusElement || null;
    }

    const popupState = {
        popupElement,
        dotNetRef
    };

    state.popups.set(popupElement, popupState);

    const updatePopupDimensions = () => {
        const width = popupElement.offsetWidth;
        const height = popupElement.offsetHeight;
        popupElement.style.setProperty('--popup-width', `${width}px`);
        popupElement.style.setProperty('--popup-height', `${height}px`);
    };

    updatePopupDimensions();

    if (typeof ResizeObserver !== 'undefined') {
        const resizeObserver = new ResizeObserver(updatePopupDimensions);
        resizeObserver.observe(popupElement);
        popupState.resizeObserver = resizeObserver;
    }
}

export function disposePopup(popupElement) {
    if (!popupElement) return;
    const popupState = state.popups.get(popupElement);
    if (popupState?.resizeObserver) {
        popupState.resizeObserver.disconnect();
    }
    state.popups.delete(popupElement);
}
