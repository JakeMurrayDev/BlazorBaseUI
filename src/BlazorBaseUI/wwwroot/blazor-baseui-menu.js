const STATE_KEY = Symbol.for('BlazorBaseUI.Menu.State');

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
    // Find the topmost open menu
    let topmostRoot = null;
    for (const [id, rootState] of state.roots) {
        if (rootState.isOpen && rootState.dotNetRef) {
            topmostRoot = rootState;
        }
    }

    if (!topmostRoot) return;

    if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();
        topmostRoot.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
        return;
    }

    // Handle arrow key navigation
    if (topmostRoot.popupElement) {
        const items = getMenuItems(topmostRoot.popupElement);
        if (items.length === 0) return;

        const currentIndex = topmostRoot.activeIndex ?? -1;
        let newIndex = currentIndex;

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                newIndex = currentIndex < items.length - 1 ? currentIndex + 1 : (topmostRoot.loopFocus ? 0 : currentIndex);
                break;
            case 'ArrowUp':
                e.preventDefault();
                newIndex = currentIndex > 0 ? currentIndex - 1 : (topmostRoot.loopFocus ? items.length - 1 : currentIndex);
                break;
            case 'Home':
                e.preventDefault();
                newIndex = 0;
                break;
            case 'End':
                e.preventDefault();
                newIndex = items.length - 1;
                break;
            case 'Enter':
            case ' ':
                e.preventDefault();
                if (currentIndex >= 0 && currentIndex < items.length) {
                    items[currentIndex].click();
                }
                return;
            default:
                // Typeahead: find item starting with pressed character
                // Use data-label attribute if set, otherwise fall back to textContent
                if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
                    const char = e.key.toLowerCase();
                    const startIndex = currentIndex + 1;
                    for (let i = 0; i < items.length; i++) {
                        const idx = (startIndex + i) % items.length;
                        const label = items[idx].getAttribute('data-label');
                        const text = (label ?? items[idx].textContent)?.trim().toLowerCase() || '';
                        if (text.startsWith(char)) {
                            newIndex = idx;
                            break;
                        }
                    }
                }
                break;
        }

        if (newIndex !== currentIndex && newIndex >= 0 && newIndex < items.length) {
            topmostRoot.activeIndex = newIndex;
            highlightItem(topmostRoot.popupElement, items, newIndex);
            topmostRoot.dotNetRef.invokeMethodAsync('OnActiveIndexChange', newIndex).catch(() => { });
        }
    }
}

function getMenuItems(popupElement) {
    if (!popupElement) return [];

    const selector = '[role="menuitem"]:not([aria-disabled="true"]):not([disabled]), ' +
                     '[role="menuitemcheckbox"]:not([aria-disabled="true"]):not([disabled]), ' +
                     '[role="menuitemradio"]:not([aria-disabled="true"]):not([disabled])';

    return Array.from(popupElement.querySelectorAll(selector));
}

function highlightItem(popupElement, items, index) {
    items.forEach((item, i) => {
        if (i === index) {
            item.setAttribute('data-highlighted', '');
            item.setAttribute('tabindex', '0');
            item.focus();
        } else {
            item.removeAttribute('data-highlighted');
            item.setAttribute('tabindex', '-1');
        }
    });
}

function handleGlobalMouseDown(e) {
    for (const [id, rootState] of state.roots) {
        if (!rootState.isOpen || !rootState.dotNetRef) continue;

        const { triggerElement, popupElement } = rootState;

        let clickedInsidePopup = false;
        let clickedOnTrigger = false;

        if (popupElement && popupElement.contains(e.target)) {
            clickedInsidePopup = true;
        }

        if (triggerElement && triggerElement.contains(e.target)) {
            clickedOnTrigger = true;
        }

        const allMenuPopups = document.querySelectorAll('[role="menu"]');
        for (const popup of allMenuPopups) {
            if (popup.contains(e.target)) {
                clickedInsidePopup = true;
                break;
            }
        }

        if (!clickedInsidePopup && !clickedOnTrigger) {
            rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
        }
    }
}

export function initializeRoot(rootId, dotNetRef, closeParentOnEsc, loopFocus, modal) {
    initGlobalListeners();

    state.roots.set(rootId, {
        dotNetRef,
        isOpen: false,
        triggerElement: null,
        positionerElement: null,
        popupElement: null,
        activeIndex: -1,
        loopFocus: loopFocus ?? true,
        closeParentOnEsc: closeParentOnEsc || false,
        modal: modal ?? true,
        scrollLocked: false
    });
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        unlockScroll(rootState);
    }
    state.roots.delete(rootId);
}

// Scroll lock state shared across all menu roots
if (!state.scrollLock) {
    state.scrollLock = {
        lockCount: 0,
        originalStyles: null
    };
}

function lockScroll(rootState) {
    if (rootState.scrollLocked) return;
    
    rootState.scrollLocked = true;
    state.scrollLock.lockCount++;
    
    // Only apply styles on first lock
    if (state.scrollLock.lockCount === 1) {
        const scrollbarWidth = window.innerWidth - document.documentElement.clientWidth;
        
        state.scrollLock.originalStyles = {
            overflow: document.body.style.overflow,
            paddingRight: document.body.style.paddingRight
        };
        
        document.body.style.overflow = 'hidden';
        if (scrollbarWidth > 0) {
            document.body.style.paddingRight = `${scrollbarWidth}px`;
        }
        
        document.documentElement.setAttribute('data-scroll-locked', '');
    }
}

function unlockScroll(rootState) {
    if (!rootState.scrollLocked) return;
    
    rootState.scrollLocked = false;
    state.scrollLock.lockCount--;
    
    // Only restore styles when all locks are released
    if (state.scrollLock.lockCount === 0 && state.scrollLock.originalStyles) {
        document.body.style.overflow = state.scrollLock.originalStyles.overflow;
        document.body.style.paddingRight = state.scrollLock.originalStyles.paddingRight;
        state.scrollLock.originalStyles = null;
        
        document.documentElement.removeAttribute('data-scroll-locked');
    }
}

export function setRootOpen(rootId, isOpen, reason) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.isOpen = isOpen;
    rootState.pendingOpen = isOpen;
    rootState.openReason = reason;

    if (isOpen) {
        rootState.activeIndex = -1;
        
        // Apply scroll lock if modal and not opened via hover
        if (rootState.modal && reason !== 'trigger-hover') {
            lockScroll(rootState);
        }
        
        waitForPopupAndStartTransition(rootState, isOpen);
    } else {
        // Remove scroll lock when closing
        unlockScroll(rootState);
        
        startTransition(rootState, isOpen);
    }
}

export function setActiveIndex(rootId, index) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.activeIndex = index;

    if (rootState.popupElement) {
        const items = getMenuItems(rootState.popupElement);
        if (index >= 0 && index < items.length) {
            highlightItem(rootState.popupElement, items, index);
        }
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

// Positioning functions
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

function getCollisionBounds(element, collisionBoundary) {
    const viewportBounds = {
        top: 0,
        left: 0,
        right: window.innerWidth,
        bottom: window.innerHeight,
        width: window.innerWidth,
        height: window.innerHeight
    };

    if (collisionBoundary === 'viewport') {
        return viewportBounds;
    }

    let bounds = { ...viewportBounds };
    let current = element.parentElement;

    while (current && current !== document.body) {
        const style = getComputedStyle(current);
        const overflow = style.overflow + style.overflowX + style.overflowY;

        if (/auto|scroll|hidden/.test(overflow)) {
            const rect = current.getBoundingClientRect();
            bounds = {
                top: Math.max(bounds.top, rect.top),
                left: Math.max(bounds.left, rect.left),
                right: Math.min(bounds.right, rect.right),
                bottom: Math.min(bounds.bottom, rect.bottom),
                width: Math.min(bounds.right, rect.right) - Math.max(bounds.left, rect.left),
                height: Math.min(bounds.bottom, rect.bottom) - Math.max(bounds.top, rect.top)
            };
        }
        current = current.parentElement;
    }

    return bounds;
}

export function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking) {
    if (!positionerElement || !triggerElement) return;

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
        collisionBoundary: collisionBoundary || 'clipping-ancestors',
        arrowPadding,
        arrowElement,
        sticky: sticky || false,
        positionMethod: positionMethod || 'absolute',
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

export function updatePosition(positionerId, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod) {
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
    positionerState.collisionBoundary = collisionBoundary || 'clipping-ancestors';
    positionerState.arrowPadding = arrowPadding;
    positionerState.arrowElement = arrowElement;
    positionerState.sticky = sticky || false;
    positionerState.positionMethod = positionMethod || 'absolute';

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
    const { positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowElement, sticky, positionMethod } = positionerState;

    if (!positionerElement || !triggerElement) return;

    const triggerRect = triggerElement.getBoundingClientRect();
    const bounds = getCollisionBounds(triggerElement, collisionBoundary);

    let effectiveSide = side;
    let effectiveAlign = align;

    let top = 0;
    let left = 0;

    const popupWidth = positionerElement.offsetWidth || 200;
    const popupHeight = positionerElement.offsetHeight || 100;

    const anchorHidden = triggerRect.right < bounds.left ||
        triggerRect.bottom < bounds.top ||
        triggerRect.left > bounds.right ||
        triggerRect.top > bounds.bottom;

    // Handle logical sides (inline-end, inline-start)
    const isRtl = getComputedStyle(positionerElement).direction === 'rtl';
    let physicalSide = effectiveSide;

    if (effectiveSide === 'inline-end') {
        physicalSide = isRtl ? 'left' : 'right';
    } else if (effectiveSide === 'inline-start') {
        physicalSide = isRtl ? 'right' : 'left';
    }

    switch (physicalSide) {
        case 'top':
            top = triggerRect.top - popupHeight - sideOffset;
            if (top < bounds.top + collisionPadding && triggerRect.bottom > bounds.top && triggerRect.bottom + popupHeight + sideOffset < bounds.bottom - collisionPadding) {
                effectiveSide = 'bottom';
                physicalSide = 'bottom';
                top = triggerRect.bottom + sideOffset;
            }
            break;
        case 'bottom':
            top = triggerRect.bottom + sideOffset;
            if (top + popupHeight > bounds.bottom - collisionPadding && triggerRect.top > popupHeight + sideOffset + bounds.top + collisionPadding) {
                effectiveSide = 'top';
                physicalSide = 'top';
                top = triggerRect.top - popupHeight - sideOffset;
            }
            break;
        case 'left':
            left = triggerRect.left - popupWidth - sideOffset;
            if (left < bounds.left + collisionPadding && triggerRect.right > bounds.left && triggerRect.right + popupWidth + sideOffset < bounds.right - collisionPadding) {
                effectiveSide = 'right';
                physicalSide = 'right';
                left = triggerRect.right + sideOffset;
            }
            break;
        case 'right':
            left = triggerRect.right + sideOffset;
            if (left + popupWidth > bounds.right - collisionPadding && triggerRect.left > popupWidth + sideOffset + bounds.left + collisionPadding) {
                effectiveSide = 'left';
                physicalSide = 'left';
                left = triggerRect.left - popupWidth - sideOffset;
            }
            break;
    }

    if (physicalSide === 'top' || physicalSide === 'bottom') {
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
            if (left < bounds.left + collisionPadding) {
                left = bounds.left + collisionPadding;
            } else if (left + popupWidth > bounds.right - collisionPadding) {
                left = bounds.right - popupWidth - collisionPadding;
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
            if (top < bounds.top + collisionPadding) {
                top = bounds.top + collisionPadding;
            } else if (top + popupHeight > bounds.bottom - collisionPadding) {
                top = bounds.bottom - popupHeight - collisionPadding;
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
    positionerElement.style.setProperty('--available-width', `${bounds.width}px`);
    positionerElement.style.setProperty('--available-height', `${bounds.height}px`);
    positionerElement.style.setProperty('--positioner-width', `${popupWidth}px`);
    positionerElement.style.setProperty('--positioner-height', `${popupHeight}px`);

    positionerElement.setAttribute('data-side', effectiveSide);
    positionerElement.setAttribute('data-align', effectiveAlign);

    if (anchorHidden) {
        positionerElement.setAttribute('data-anchor-hidden', '');
    } else {
        positionerElement.removeAttribute('data-anchor-hidden');
    }

    let transformOriginX, transformOriginY;
    if (physicalSide === 'top' || physicalSide === 'bottom') {
        transformOriginX = effectiveAlign === 'start' ? '0%' : effectiveAlign === 'end' ? '100%' : '50%';
        transformOriginY = physicalSide === 'top' ? '100%' : '0%';
    } else {
        transformOriginX = physicalSide === 'left' ? '100%' : '0%';
        transformOriginY = effectiveAlign === 'start' ? '0%' : effectiveAlign === 'end' ? '100%' : '50%';
    }
    positionerElement.style.setProperty('--transform-origin', `${transformOriginX} ${transformOriginY}`);

    if (arrowElement) {
        updateArrowPosition(arrowElement, physicalSide, triggerRect, positionerElement, left, top);
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
