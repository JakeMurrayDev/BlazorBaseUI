import { acquireScrollLock } from './blazor-baseui-scroll-lock.js';

const STATE_KEY = Symbol.for('BlazorBaseUI.Dialog.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        dialogStack: [],
        globalListenersInitialized: false
    };
}
const state = window[STATE_KEY];

function initGlobalListeners() {
    if (state.globalListenersInitialized) return;

    document.addEventListener('keydown', handleGlobalKeyDown);
    state.globalListenersInitialized = true;
}

function handleGlobalKeyDown(e) {
    if (e.key !== 'Escape') return;
    if (state.dialogStack.length === 0) return;

    // Close the topmost open dialog
    const topmostRootId = state.dialogStack[state.dialogStack.length - 1];
    const rootState = state.roots.get(topmostRootId);

    if (rootState && rootState.isOpen && rootState.dotNetRef) {
        e.preventDefault();
        e.stopPropagation();
        rootState.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
    }
}

export function initializeRoot(rootId, dotNetRef, modal) {
    initGlobalListeners();

    state.roots.set(rootId, {
        dotNetRef,
        isOpen: false,
        modal: modal || 'true',
        triggerElement: null,
        popupElement: null,
        focusTrapCleanup: null,
        transitionCleanup: null,
        fallbackTimeoutId: null,
        pendingOpen: false,
        releaseScrollLock: null,
        outsideClickCleanup: null
    });
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        cleanupFocusTrap(rootState);
        cleanupTransition(rootState);
        cleanupOutsideClick(rootState);
        removeFromDialogStack(rootId);

        // Release scroll lock if this dialog had it
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }
    }
    state.roots.delete(rootId);
}

export function setRootOpen(rootId, isOpen) {
    const rootState = state.roots.get(rootId);
    if (!rootState) {
        return;
    }

    // Prevent duplicate calls with the same state
    if (rootState.isOpen === isOpen) {
        return;
    }

    rootState.isOpen = isOpen;
    rootState.pendingOpen = isOpen;

    if (isOpen) {
        addToDialogStack(rootId);

        // Lock scroll for modal dialogs (modal === 'true' only, not 'trap-focus')
        if (rootState.modal === 'true') {
            rootState.releaseScrollLock = acquireScrollLock(rootState.popupElement);
        }

        waitForPopupAndStartTransition(rootState, isOpen);
    } else {
        removeFromDialogStack(rootId);

        // Release scroll lock if this dialog acquired it
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }

        cleanupOutsideClick(rootState);
        cleanupFocusTrap(rootState);
        startTransition(rootState, isOpen);
    }
}

function addToDialogStack(rootId) {
    if (!state.dialogStack.includes(rootId)) {
        state.dialogStack.push(rootId);
        updateNestedDialogCounts();
    }
}

function removeFromDialogStack(rootId) {
    const index = state.dialogStack.indexOf(rootId);
    if (index !== -1) {
        state.dialogStack.splice(index, 1);
        updateNestedDialogCounts();
    }
}

function updateNestedDialogCounts() {
    const stackLength = state.dialogStack.length;

    state.dialogStack.forEach((rootId, index) => {
        const rootState = state.roots.get(rootId);
        if (rootState && rootState.dotNetRef) {
            // Number of dialogs above this one in the stack
            const nestedCount = stackLength - index - 1;
            rootState.dotNetRef.invokeMethodAsync('OnNestedDialogCountChange', nestedCount).catch(() => { });
        }
    });
}

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

function startTransition(rootState, isOpen) {
    const popupElement = rootState.popupElement;

    if (!popupElement) {
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
        return;
    }

    const hasTransition = checkForTransitionOrAnimation(popupElement);

    if (isOpen) {
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                if (rootState.pendingOpen !== isOpen) {
                    return;
                }

                // Set up focus trap for modal dialogs
                if (rootState.modal === 'true' || rootState.modal === 'trap-focus') {
                    setupFocusTrap(rootState);
                }

                // Set up outside click listener for non-modal dialogs
                if (rootState.modal === 'false') {
                    setupOutsideClickListener(rootState);
                }

                // Focus custom element or default behavior
                focusPopup(rootState, popupElement);

                if (hasTransition) {
                    setupTransitionEndListener(rootState, isOpen);
                }

                if (rootState.dotNetRef) {
                    rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
                }
            });
        });
    } else {
        if (hasTransition) {
            setupTransitionEndListener(rootState, isOpen);
        } else {
            if (rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
            }
        }

        // Return focus to custom element or trigger element
        returnFocus(rootState);
    }
}

function focusPopup(rootState, popupElement) {
    if (!popupElement) return;

    // Use custom initial focus element if provided
    if (rootState.initialFocusElement) {
        rootState.initialFocusElement.focus();
        return;
    }

    // Find the first focusable element inside the popup
    const focusableSelector = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';
    const firstFocusable = popupElement.querySelector(focusableSelector);

    if (firstFocusable) {
        firstFocusable.focus();
    } else {
        // If no focusable element, focus the popup itself
        popupElement.focus();
    }
}

function returnFocus(rootState) {
    // Use custom final focus element if provided
    const focusTarget = rootState.finalFocusElement || rootState.triggerElement;

    if (focusTarget) {
        requestAnimationFrame(() => {
            focusTarget.focus();
        });
    }
}

function setupFocusTrap(rootState) {
    const popupElement = rootState.popupElement;
    if (!popupElement) return;

    cleanupFocusTrap(rootState);

    const focusableSelector = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';

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

function setupOutsideClickListener(rootState) {
    // Only skip for true modal, not for false or trap-focus
    if (rootState.modal === 'true') return;

    cleanupOutsideClick(rootState);

    const handleOutsideClick = (e) => {
        const popupElement = rootState.popupElement;
        const triggerElement = rootState.triggerElement;

        if (!popupElement) return;
        if (!rootState.isOpen) return;

        const clickedInsidePopup = popupElement.contains(e.target);
        const clickedOnTrigger = triggerElement && triggerElement.contains(e.target);

        if (!clickedInsidePopup && !clickedOnTrigger && rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
        }
    };

    // Use a small delay to avoid catching the click that opened the dialog
    setTimeout(() => {
        document.addEventListener('pointerdown', handleOutsideClick);
    }, 0);

    rootState.outsideClickCleanup = () => {
        document.removeEventListener('pointerdown', handleOutsideClick);
    };
}

function cleanupOutsideClick(rootState) {
    if (rootState.outsideClickCleanup) {
        rootState.outsideClickCleanup();
        rootState.outsideClickCleanup = null;
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

function checkForTransitionOrAnimation(element) {
    const style = getComputedStyle(element);

    const transitionDuration = parseCssDuration(style.transitionDuration);
    const hasTransition = transitionDuration > 0;

    const animationName = style.animationName;
    const animationDuration = parseCssDuration(style.animationDuration);
    const hasAnimation = animationName && animationName !== 'none' && animationDuration > 0;

    return hasTransition || hasAnimation;
}

function parseCssDuration(durationStr) {
    if (!durationStr || durationStr === 'none') return 0;

    const durations = durationStr.split(',').map(d => d.trim());
    let maxMs = 0;

    for (const duration of durations) {
        let ms = 0;
        if (duration.endsWith('ms')) {
            ms = parseFloat(duration);
        } else if (duration.endsWith('s')) {
            ms = parseFloat(duration) * 1000;
        }
        if (!isNaN(ms) && ms > maxMs) {
            maxMs = ms;
        }
    }

    return maxMs;
}

function getMaxTransitionDuration(element) {
    const style = getComputedStyle(element);

    const transitionDuration = parseCssDuration(style.transitionDuration);
    const transitionDelay = parseCssDuration(style.transitionDelay);
    const totalTransition = transitionDuration + transitionDelay;

    const animationDuration = parseCssDuration(style.animationDuration);
    const animationDelay = parseCssDuration(style.animationDelay);
    const totalAnimation = animationDuration + animationDelay;

    const maxDuration = Math.max(totalTransition, totalAnimation);
    const withBuffer = maxDuration + 50;
    const minTimeout = 100;
    const maxTimeout = 10000;

    return Math.max(minTimeout, Math.min(withBuffer, maxTimeout));
}

function setupTransitionEndListener(rootState, isOpen) {
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

    const fallbackTimeout = getMaxTransitionDuration(popupElement);
    rootState.fallbackTimeoutId = setTimeout(() => {
        if (!called && rootState.dotNetRef) {
            called = true;
            cleanup();
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
    }, fallbackTimeout);
}

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
    }
}

export function initializePopup(rootId, popupElement, dotNetRef, modal, initialFocusElement, finalFocusElement) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.popupElement = popupElement;
        rootState.modal = modal || 'true';
        rootState.initialFocusElement = initialFocusElement;
        rootState.finalFocusElement = finalFocusElement;
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

export function disposePopup(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        cleanupFocusTrap(rootState);
        rootState.popupElement = null;
    }
}
