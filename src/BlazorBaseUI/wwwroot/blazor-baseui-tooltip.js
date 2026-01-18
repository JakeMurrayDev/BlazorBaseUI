const STATE_KEY = Symbol.for('BlazorBaseUI.Tooltip.State');

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
    state.globalListenersInitialized = true;
}

function handleGlobalKeyDown(e) {
    if (e.key !== 'Escape') return;

    for (const [id, rootState] of state.roots) {
        if (rootState.isOpen && rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
            break;
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

    if (isOpen) {
        waitForPopupAndStartTransition(rootState, isOpen);
    } else {
        startTransition(rootState, isOpen);
    }
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

function setupAutoUpdate(positionerState) {
    const { positionerElement, triggerElement } = positionerState;
    if (!positionerElement || !triggerElement) return;

    cleanupAutoUpdate(positionerState);

    const update = () => {
        updatePositionInternal(positionerState);
    };

    const scrollParents = getScrollParents(triggerElement);
    scrollParents.forEach(parent => {
        parent.addEventListener('scroll', update, { passive: true });
    });

    window.addEventListener('resize', update, { passive: true });

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

    scrollParents.push(window);

    return scrollParents;
}

export function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking) {
    if (!positionerElement || !triggerElement) return;

    const positionerId = positionerElement.id || `positioner-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;

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

    const anchorHidden = triggerRect.right < 0 ||
        triggerRect.bottom < 0 ||
        triggerRect.left > viewportWidth ||
        triggerRect.top > viewportHeight;

    switch (effectiveSide) {
        case 'top':
            top = triggerRect.top - popupHeight - sideOffset;
            if (top < collisionPadding && triggerRect.bottom > 0 && triggerRect.bottom + popupHeight + sideOffset < viewportHeight - collisionPadding) {
                effectiveSide = 'bottom';
                top = triggerRect.bottom + sideOffset;
            }
            break;
        case 'bottom':
            top = triggerRect.bottom + sideOffset;
            if (top + popupHeight > viewportHeight - collisionPadding && triggerRect.top > popupHeight + sideOffset + collisionPadding) {
                effectiveSide = 'top';
                top = triggerRect.top - popupHeight - sideOffset;
            }
            break;
        case 'left':
            left = triggerRect.left - popupWidth - sideOffset;
            if (left < collisionPadding && triggerRect.right > 0 && triggerRect.right + popupWidth + sideOffset < viewportWidth - collisionPadding) {
                effectiveSide = 'right';
                left = triggerRect.right + sideOffset;
            }
            break;
        case 'right':
            left = triggerRect.right + sideOffset;
            if (left + popupWidth > viewportWidth - collisionPadding && triggerRect.left > popupWidth + sideOffset + collisionPadding) {
                effectiveSide = 'left';
                left = triggerRect.left - popupWidth - sideOffset;
            }
            break;
    }

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

        if (sticky) {
            if (top < collisionPadding) {
                top = collisionPadding;
            } else if (top + popupHeight > viewportHeight - collisionPadding) {
                top = viewportHeight - popupHeight - collisionPadding;
            }
        }
    }

    if (positionMethod === 'absolute') {
        const scrollX = window.scrollX || document.documentElement.scrollLeft;
        const scrollY = window.scrollY || document.documentElement.scrollTop;
        top += scrollY;
        left += scrollX;
    }

    positionerElement.style.position = positionMethod === 'absolute' ? 'absolute' : 'fixed';
    positionerElement.style.top = `${top}px`;
    positionerElement.style.left = `${left}px`;
    positionerElement.style.zIndex = '1000';

    positionerElement.style.setProperty('--anchor-width', `${triggerRect.width}px`);
    positionerElement.style.setProperty('--anchor-height', `${triggerRect.height}px`);
    positionerElement.style.setProperty('--available-width', `${viewportWidth}px`);
    positionerElement.style.setProperty('--available-height', `${viewportHeight}px`);

    positionerElement.setAttribute('data-side', effectiveSide);
    positionerElement.setAttribute('data-align', effectiveAlign);

    if (anchorHidden) {
        positionerElement.setAttribute('data-anchor-hidden', '');
    } else {
        positionerElement.removeAttribute('data-anchor-hidden');
    }

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
            arrowTop = positionerHeight - 1;
            arrowLeft = (positionerWidth - arrowWidth) / 2;
            arrowElement.style.transform = 'rotate(180deg)';
            break;
        case 'bottom':
            arrowTop = -arrowHeight + 1;
            arrowLeft = (positionerWidth - arrowWidth) / 2;
            arrowElement.style.transform = 'rotate(0deg)';
            break;
        case 'left':
            arrowLeft = positionerWidth - 1;
            arrowTop = (positionerHeight - arrowHeight) / 2;
            arrowElement.style.transform = 'rotate(90deg)';
            break;
        case 'right':
            arrowLeft = -arrowHeight + 1;
            arrowTop = (positionerHeight - arrowHeight) / 2;
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
