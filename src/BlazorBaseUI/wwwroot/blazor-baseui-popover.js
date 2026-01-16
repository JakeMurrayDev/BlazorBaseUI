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

export function initializeRoot(rootId, dotNetRef) {
    initGlobalListeners();

    state.roots.set(rootId, {
        dotNetRef,
        isOpen: false,
        triggerElement: null,
        positionerElement: null,
        popupElement: null
    });
}

export function disposeRoot(rootId) {
    state.roots.delete(rootId);
}

export function setRootOpen(rootId, isOpen) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.isOpen = isOpen;
    rootState.pendingOpen = isOpen;

    // For opening, we need to wait for the popup element to be available
    // The popup might not be rendered yet on first open
    if (isOpen) {
        waitForPopupAndStartTransition(rootState, isOpen);
    } else {
        // For closing, the popup element should already be available
        startTransition(rootState, isOpen);
    }
}

function waitForPopupAndStartTransition(rootState, isOpen) {
    const popupElement = rootState.popupElement;

    if (popupElement) {
        startTransition(rootState, isOpen);
        return;
    }

    // Popup not available yet, wait for it using requestAnimationFrame
    // This handles the case where the popup is being rendered for the first time
    let attempts = 0;
    const maxAttempts = 10; // Limit retries to avoid infinite loops

    function checkForPopup() {
        attempts++;
        const element = rootState.popupElement;

        if (element) {
            // Popup is now available, start the transition
            if (rootState.pendingOpen === isOpen) {
                startTransition(rootState, isOpen);
            }
        } else if (attempts < maxAttempts && rootState.pendingOpen === isOpen) {
            // Keep waiting
            requestAnimationFrame(checkForPopup);
        } else if (rootState.dotNetRef && rootState.pendingOpen === isOpen) {
            // Give up waiting, call OnStartingStyleApplied anyway so UI isn't stuck
            rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
        }
    }

    requestAnimationFrame(checkForPopup);
}

function startTransition(rootState, isOpen) {
    const popupElement = rootState.popupElement;

    if (!popupElement) {
        // No popup element, call transition end immediately
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
        return;
    }

    // Check if the element has transitions or animations
    const hasTransition = checkForTransitionOrAnimation(popupElement);

    if (isOpen) {
        // For opening: wait for the initial render with starting styles, then clear starting status
        // Use double rAF to ensure the browser has painted the initial state
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                // Guard against stale state - if pendingOpen changed, abort this transition
                if (rootState.pendingOpen !== isOpen) {
                    return;
                }
                if (hasTransition) {
                    setupTransitionEndListener(rootState, isOpen);
                }
                // Tell C# to clear the Starting status so the element transitions to open state
                if (rootState.dotNetRef) {
                    rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
                }
            });
        });
    } else {
        // For closing: set up listener immediately since element is already visible
        if (hasTransition) {
            setupTransitionEndListener(rootState, isOpen);
        } else {
            // No transition, call immediately
            if (rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
            }
        }
    }
}

function checkForTransitionOrAnimation(element) {
    const style = getComputedStyle(element);

    // Check for CSS transitions
    const transitionDuration = style.transitionDuration;
    const hasTransition = transitionDuration && transitionDuration !== '0s' && transitionDuration !== 'none';

    // Check for CSS animations
    const animationDuration = style.animationDuration;
    const animationName = style.animationName;
    const hasAnimation = animationName && animationName !== 'none' && animationDuration && animationDuration !== '0s';

    return hasTransition || hasAnimation;
}

function parseCssDuration(durationStr) {
    if (!durationStr || durationStr === 'none') return 0;

    // Handle comma-separated values (e.g., "0.3s, 0.5s") - take the max
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

    // Get transition duration + delay
    const transitionDuration = parseCssDuration(style.transitionDuration);
    const transitionDelay = parseCssDuration(style.transitionDelay);
    const totalTransition = transitionDuration + transitionDelay;

    // Get animation duration + delay
    const animationDuration = parseCssDuration(style.animationDuration);
    const animationDelay = parseCssDuration(style.animationDelay);
    const totalAnimation = animationDuration + animationDelay;

    // Return the maximum of transition or animation, with a buffer and bounds
    const maxDuration = Math.max(totalTransition, totalAnimation);
    const withBuffer = maxDuration + 50; // Add 50ms buffer
    const minTimeout = 100;
    const maxTimeout = 10000;

    return Math.max(minTimeout, Math.min(withBuffer, maxTimeout));
}

function setupTransitionEndListener(rootState, isOpen) {
    const popupElement = rootState.popupElement;
    if (!popupElement) return;

    // Clean up any existing listener and cancel pending fallback timeout
    if (rootState.transitionCleanup) {
        rootState.transitionCleanup();
        rootState.transitionCleanup = null;
    }
    if (rootState.fallbackTimeoutId) {
        clearTimeout(rootState.fallbackTimeoutId);
        rootState.fallbackTimeoutId = null;
    }

    let called = false;
    const handleEnd = (event) => {
        // Only handle events from the popup element itself, not children
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

    // Fallback timeout based on actual CSS duration
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

// Auto-update: track scroll and resize to reposition the positioner
function setupAutoUpdate(positionerState) {
    const { positionerElement, triggerElement } = positionerState;
    if (!positionerElement || !triggerElement) return;

    // Clean up existing listeners
    cleanupAutoUpdate(positionerState);

    const update = () => {
        updatePositionInternal(positionerState);
    };

    // Scroll listener on all scrollable ancestors
    const scrollParents = getScrollParents(triggerElement);
    scrollParents.forEach(parent => {
        parent.addEventListener('scroll', update, { passive: true });
    });

    // Resize listener
    window.addEventListener('resize', update, { passive: true });

    // Store cleanup info
    positionerState.cleanup = () => {
        scrollParents.forEach(parent => {
            parent.removeEventListener('scroll', update);
        });
        window.removeEventListener('resize', update);
    };
}

function cleanupAutoUpdate(positionerState) {
    if (positionerState.cleanup) {
        positionerState.cleanup();
        positionerState.cleanup = null;
    }
}

function getScrollParents(element) {
    const scrollParents = [];
    let current = element.parentElement;

    while (current) {
        const style = getComputedStyle(current);
        const overflow = style.overflow + style.overflowX + style.overflowY;
        if (/auto|scroll|overlay/.test(overflow)) {
            scrollParents.push(current);
        }
        current = current.parentElement;
    }

    // Always include window for document scroll
    scrollParents.push(window);

    return scrollParents;
}

export function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking) {
    if (!positionerElement || !triggerElement) return;

    // Generate a unique ID for this positioner
    const positionerId = positionerElement.id || `positioner-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

    const positionerState = {
        positionerId,
        positionerElement,
        triggerElement,
        side,
        align,
        sideOffset,
        alignOffset,
        collisionPadding,
        arrowPadding,
        arrowElement,
        sticky: sticky || false,
        positionMethod: positionMethod || 'fixed',
        disableAnchorTracking: disableAnchorTracking || false,
        cleanup: null
    };

    state.positioners.set(positionerId, positionerState);
    updatePositionInternal(positionerState);

    if (!disableAnchorTracking) {
        setupAutoUpdate(positionerState);
    }

    return positionerId;
}

export function updatePosition(positionerId, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, arrowPadding, arrowElement, sticky, positionMethod) {
    if (!positionerId || !triggerElement) return;

    let positionerState = state.positioners.get(positionerId);
    if (!positionerState) {
        return;
    }

    positionerState.triggerElement = triggerElement;
    positionerState.side = side;
    positionerState.align = align;
    positionerState.sideOffset = sideOffset;
    positionerState.alignOffset = alignOffset;
    positionerState.collisionPadding = collisionPadding;
    positionerState.arrowPadding = arrowPadding;
    positionerState.arrowElement = arrowElement;
    positionerState.sticky = sticky || false;
    positionerState.positionMethod = positionMethod || 'fixed';

    updatePositionInternal(positionerState);
}

export function disposePositioner(positionerId) {
    if (!positionerId) return;

    const positionerState = state.positioners.get(positionerId);
    if (positionerState) {
        cleanupAutoUpdate(positionerState);
        state.positioners.delete(positionerId);
    }
}

function updatePositionInternal(positionerState) {
    const { positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, arrowElement, sticky, positionMethod } = positionerState;

    if (!positionerElement || !triggerElement) return;

    const triggerRect = triggerElement.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    let effectiveSide = side;
    let effectiveAlign = align;

    let top = 0;
    let left = 0;

    const popupWidth = positionerElement.offsetWidth || 200;
    const popupHeight = positionerElement.offsetHeight || 100;

    // Check if anchor is hidden (outside viewport)
    const anchorHidden = triggerRect.right < 0 ||
        triggerRect.bottom < 0 ||
        triggerRect.left > viewportWidth ||
        triggerRect.top > viewportHeight;

    // Calculate position based on side (using viewport coordinates for fixed positioning)
    // Flip to opposite side only if trigger is visible and popup doesn't fit
    switch (effectiveSide) {
        case 'top':
            top = triggerRect.top - popupHeight - sideOffset;
            // Only flip if both the trigger is in viewport AND there's not enough space
            if (top < collisionPadding && triggerRect.bottom > 0 && triggerRect.bottom + popupHeight + sideOffset < viewportHeight - collisionPadding) {
                effectiveSide = 'bottom';
                top = triggerRect.bottom + sideOffset;
            }
            break;
        case 'bottom':
            top = triggerRect.bottom + sideOffset;
            // Only flip if both the trigger is in viewport AND there's not enough space
            if (top + popupHeight > viewportHeight - collisionPadding && triggerRect.top > popupHeight + sideOffset + collisionPadding) {
                effectiveSide = 'top';
                top = triggerRect.top - popupHeight - sideOffset;
            }
            break;
        case 'left':
            left = triggerRect.left - popupWidth - sideOffset;
            // Only flip if both the trigger is in viewport AND there's not enough space
            if (left < collisionPadding && triggerRect.right > 0 && triggerRect.right + popupWidth + sideOffset < viewportWidth - collisionPadding) {
                effectiveSide = 'right';
                left = triggerRect.right + sideOffset;
            }
            break;
        case 'right':
            left = triggerRect.right + sideOffset;
            // Only flip if both the trigger is in viewport AND there's not enough space
            if (left + popupWidth > viewportWidth - collisionPadding && triggerRect.left > popupWidth + sideOffset + collisionPadding) {
                effectiveSide = 'left';
                left = triggerRect.left - popupWidth - sideOffset;
            }
            break;
    }

    // Calculate alignment
    if (effectiveSide === 'top' || effectiveSide === 'bottom') {
        switch (effectiveAlign) {
            case 'start':
                left = triggerRect.left + alignOffset;
                break;
            case 'center':
                left = triggerRect.left + (triggerRect.width - popupWidth) / 2 + alignOffset;
                break;
            case 'end':
                left = triggerRect.right - popupWidth + alignOffset;
                break;
        }

        // Collision avoidance for alignment axis - only if sticky is true
        if (sticky) {
            if (left < collisionPadding) {
                left = collisionPadding;
            } else if (left + popupWidth > viewportWidth - collisionPadding) {
                left = viewportWidth - popupWidth - collisionPadding;
            }
        }
    } else {
        switch (effectiveAlign) {
            case 'start':
                top = triggerRect.top + alignOffset;
                break;
            case 'center':
                top = triggerRect.top + (triggerRect.height - popupHeight) / 2 + alignOffset;
                break;
            case 'end':
                top = triggerRect.bottom - popupHeight + alignOffset;
                break;
        }

        // Collision avoidance for alignment axis - only if sticky is true
        if (sticky) {
            if (top < collisionPadding) {
                top = collisionPadding;
            } else if (top + popupHeight > viewportHeight - collisionPadding) {
                top = viewportHeight - popupHeight - collisionPadding;
            }
        }
    }

    // For absolute positioning, adjust coordinates relative to containing block
    if (positionMethod === 'absolute') {
        const scrollX = window.scrollX || document.documentElement.scrollLeft;
        const scrollY = window.scrollY || document.documentElement.scrollTop;
        top += scrollY;
        left += scrollX;
    }

    // Apply position
    positionerElement.style.position = positionMethod === 'absolute' ? 'absolute' : 'fixed';
    positionerElement.style.top = `${top}px`;
    positionerElement.style.left = `${left}px`;
    positionerElement.style.zIndex = '1000';

    // Set CSS custom properties for dimensions
    positionerElement.style.setProperty('--anchor-width', `${triggerRect.width}px`);
    positionerElement.style.setProperty('--anchor-height', `${triggerRect.height}px`);
    positionerElement.style.setProperty('--available-width', `${viewportWidth}px`);
    positionerElement.style.setProperty('--available-height', `${viewportHeight}px`);

    positionerElement.setAttribute('data-side', effectiveSide);
    positionerElement.setAttribute('data-align', effectiveAlign);

    // Set anchor hidden attribute
    if (anchorHidden) {
        positionerElement.setAttribute('data-anchor-hidden', '');
    } else {
        positionerElement.removeAttribute('data-anchor-hidden');
    }

    // Calculate and set transform origin
    let transformOriginX, transformOriginY;
    if (effectiveSide === 'top' || effectiveSide === 'bottom') {
        transformOriginX = effectiveAlign === 'start' ? '0%' : effectiveAlign === 'end' ? '100%' : '50%';
        transformOriginY = effectiveSide === 'top' ? '100%' : '0%';
    } else {
        transformOriginX = effectiveSide === 'left' ? '100%' : '0%';
        transformOriginY = effectiveAlign === 'start' ? '0%' : effectiveAlign === 'end' ? '100%' : '50%';
    }
    positionerElement.style.setProperty('--transform-origin', `${transformOriginX} ${transformOriginY}`);

    if (arrowElement) {
        updateArrowPosition(arrowElement, effectiveSide, triggerRect, positionerElement, left, top);
    }
}

function updateArrowPosition(arrowElement, side, triggerRect, positionerElement, positionerLeft, positionerTop) {
    if (!arrowElement) return;

    const arrowWidth = arrowElement.offsetWidth || 20;
    const arrowHeight = arrowElement.offsetHeight || 10;
    const positionerWidth = positionerElement.offsetWidth;
    const positionerHeight = positionerElement.offsetHeight;

    let arrowTop = 0;
    let arrowLeft = 0;

    switch (side) {
        case 'top':
            // Arrow at bottom of positioner, pointing down
            arrowTop = positionerHeight - 1;
            arrowLeft = (positionerWidth - arrowWidth) / 2;
            arrowElement.style.transform = 'rotate(180deg)';
            break;
        case 'bottom':
            // Arrow at top of positioner, pointing up
            arrowTop = -arrowHeight + 1;
            arrowLeft = (positionerWidth - arrowWidth) / 2;
            arrowElement.style.transform = 'rotate(0deg)';
            break;
        case 'left':
            // Arrow at right of positioner, pointing right
            arrowLeft = positionerWidth - 1;
            arrowTop = (positionerHeight - arrowWidth) / 2;
            arrowElement.style.transform = 'rotate(90deg)';
            break;
        case 'right':
            // Arrow at left of positioner, pointing left
            arrowLeft = -arrowHeight + 1;
            arrowTop = (positionerHeight - arrowWidth) / 2;
            arrowElement.style.transform = 'rotate(-90deg)';
            break;
    }

    arrowElement.style.position = 'absolute';
    arrowElement.style.top = `${arrowTop}px`;
    arrowElement.style.left = `${arrowLeft}px`;
}

export function initializePopup(popupElement, dotNetRef) {
    if (!popupElement) return;

    const popupState = {
        popupElement,
        dotNetRef
    };

    state.popups.set(popupElement, popupState);
}

export function disposePopup(popupElement) {
    if (!popupElement) return;
    state.popups.delete(popupElement);
}

export function focusElement(element) {
    if (!element) return;
    element.focus();
}
