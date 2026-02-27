/**
 * BlazorBaseUI Select Component
 *
 * Select-specific functionality that builds on the shared floating infrastructure.
 */

import { acquireScrollLock } from './blazor-baseui-scroll-lock.js';

let floatingModule = null;

async function ensureFloatingModule() {
    if (!floatingModule) {
        floatingModule = await import('./blazor-baseui-floating.js');
    }
    return floatingModule;
}

const STATE_KEY = Symbol.for('BlazorBaseUI.Select.State');

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
    let topmostRoot = null;

    for (const [id, rootState] of state.roots) {
        if (rootState.isOpen && rootState.dotNetRef) {
            topmostRoot = rootState;
        }
    }

    if (!topmostRoot) return;

    const isRtl = topmostRoot.direction === 'rtl';

    if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();
        topmostRoot.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
        return;
    }

    const popupEl = topmostRoot.popupElement;
    const listEl = topmostRoot.listElement;
    const containerEl = listEl || popupEl;
    if (!containerEl) return;

    const items = getNavigableItems(containerEl);
    if (items.length === 0) return;

    const currentIndex = topmostRoot.activeIndex;

    if (e.key === 'ArrowDown') {
        e.preventDefault();
        const nextIndex = findNextEnabledIndex(items, currentIndex, 1, topmostRoot.loopFocus);
        if (nextIndex !== -1) {
            setActiveItem(topmostRoot, items, nextIndex);
        }
    } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        const nextIndex = findNextEnabledIndex(items, currentIndex, -1, topmostRoot.loopFocus);
        if (nextIndex !== -1) {
            setActiveItem(topmostRoot, items, nextIndex);
        }
    } else if (e.key === 'Home') {
        e.preventDefault();
        const nextIndex = findNextEnabledIndex(items, -1, 1, false);
        if (nextIndex !== -1) {
            setActiveItem(topmostRoot, items, nextIndex);
        }
    } else if (e.key === 'End') {
        e.preventDefault();
        const nextIndex = findNextEnabledIndex(items, items.length, -1, false);
        if (nextIndex !== -1) {
            setActiveItem(topmostRoot, items, nextIndex);
        }
    } else if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        if (currentIndex >= 0 && currentIndex < items.length) {
            items[currentIndex].click();
        }
    } else if (e.key === 'Tab') {
        topmostRoot.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
    } else if (e.key.length === 1 && !e.ctrlKey && !e.altKey && !e.metaKey) {
        handleTypeahead(topmostRoot, items, e.key);
    }
}

function handleGlobalMouseDown(e) {
    for (const [id, rootState] of state.roots) {
        if (!rootState.isOpen || !rootState.dotNetRef) continue;

        const triggerEl = rootState.triggerElement;
        const popupEl = rootState.popupElement;

        if (triggerEl && triggerEl.contains(e.target)) continue;
        if (popupEl && popupEl.contains(e.target)) continue;

        for (const [posId, posState] of state.positioners) {
            if (posState.rootId === id && posState.element && posState.element.contains(e.target)) {
                return;
            }
        }

        rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
    }
}

function getNavigableItems(container) {
    return Array.from(container.querySelectorAll('[role="option"]'));
}

function findNextEnabledIndex(items, currentIndex, direction, loop) {
    const length = items.length;
    if (length === 0) return -1;

    let index = currentIndex + direction;

    for (let i = 0; i < length; i++) {
        if (index < 0) {
            index = loop ? length - 1 : 0;
            if (!loop) return -1;
        } else if (index >= length) {
            index = loop ? 0 : length - 1;
            if (!loop) return -1;
        }

        const item = items[index];
        if (!item.hasAttribute('aria-disabled') || item.getAttribute('aria-disabled') !== 'true') {
            return index;
        }

        index += direction;
    }

    return -1;
}

function handleTypeahead(rootState, items, char) {
    clearTimeout(rootState.typeaheadTimer);
    rootState.typeaheadBuffer += char.toLowerCase();

    rootState.typeaheadTimer = setTimeout(() => {
        rootState.typeaheadBuffer = '';
    }, 500);

    const startIndex = rootState.activeIndex >= 0 ? rootState.activeIndex : -1;

    for (let offset = 1; offset <= items.length; offset++) {
        const index = (startIndex + offset) % items.length;
        const item = items[index];

        if (item.hasAttribute('aria-disabled') && item.getAttribute('aria-disabled') === 'true') {
            continue;
        }

        const label = item.getAttribute('data-label') || item.textContent || '';
        if (label.toLowerCase().startsWith(rootState.typeaheadBuffer)) {
            setActiveItem(rootState, items, index);
            return;
        }
    }
}

function setActiveItem(rootState, items, index) {
    const container = rootState.listElement || rootState.popupElement;

    items.forEach((item, i) => {
        if (i === index) {
            item.setAttribute('data-highlighted', '');
            item.setAttribute('tabindex', '0');
            item.focus({ preventScroll: true });
            if (container) {
                const itemTop = item.offsetTop;
                const itemBottom = itemTop + item.offsetHeight;
                const scrollTop = container.scrollTop;
                const viewHeight = container.clientHeight;
                if (itemTop < scrollTop) {
                    container.scrollTop = itemTop;
                } else if (itemBottom > scrollTop + viewHeight) {
                    container.scrollTop = itemBottom - viewHeight;
                }
            }
        } else {
            item.removeAttribute('data-highlighted');
            item.setAttribute('tabindex', '-1');
        }
    });

    rootState.activeIndex = index;
    rootState.dotNetRef.invokeMethodAsync('OnActiveIndexChange', index).catch(() => { });
}

// ─── Align Item With Trigger ──────────────────────────────────────────

function applyAlignItemWithTrigger(positionerElement, triggerElement) {
    if (!positionerElement || !triggerElement) return;

    const selectedItem = positionerElement.querySelector('[data-selected]');
    if (!selectedItem) return;

    const currentTransform = positionerElement.style.transform || '';
    const cleanTransform = currentTransform.replace(/\s*translateY\([^)]+\)/, '');
    positionerElement.style.transform = cleanTransform;

    const positionerRect = positionerElement.getBoundingClientRect();

    if (positionerRect.width === 0 && positionerRect.height === 0) return;

    const triggerRect = triggerElement.getBoundingClientRect();
    const itemRect = selectedItem.getBoundingClientRect();

    const itemOffsetFromTop = itemRect.top - positionerRect.top;
    const targetTop = triggerRect.top - itemOffsetFromTop;

    const viewportHeight = window.innerHeight;
    const positionerHeight = positionerRect.height;

    let adjustedTop = targetTop;

    const padding = 8;
    if (adjustedTop < padding) {
        adjustedTop = padding;
    } else if (adjustedTop + positionerHeight > viewportHeight - padding) {
        adjustedTop = viewportHeight - positionerHeight - padding;
    }

    const delta = adjustedTop - positionerRect.top;

    if (Math.abs(delta) > 1) {
        positionerElement.style.transform = (cleanTransform ? cleanTransform + ' ' : '') + `translateY(${delta}px)`;
    }
}

// ─── Public API ───────────────────────────────────────────────────────

export function initializeRoot(rootId, dotNetRef, loopFocus, modal, direction) {
    initGlobalListeners();

    state.roots.set(rootId, {
        dotNetRef,
        isOpen: false,
        loopFocus: loopFocus ?? true,
        modal: modal ?? false,
        direction: direction ?? 'ltr',
        activeIndex: -1,
        triggerElement: null,
        popupElement: null,
        listElement: null,
        scrollLockCleanup: null,
        typeaheadBuffer: '',
        typeaheadTimer: null,
        scrollListener: null,
        continuousScrollInterval: null
    });
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        clearTimeout(rootState.typeaheadTimer);
        clearInterval(rootState.continuousScrollInterval);
        removeScrollListener(rootState);
        if (rootState.scrollLockCleanup) {
            rootState.scrollLockCleanup();
        }
        state.roots.delete(rootId);
    }
}

export function setRootOpen(rootId, isOpen, reason) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.isOpen = isOpen;

    if (isOpen) {
        if (rootState.modal) {
            rootState.scrollLockCleanup = acquireScrollLock();
        }

        function waitForPopupAndFocus() {
            let attempts = 0;
            const maxAttempts = 10;

            function check() {
                attempts++;
                const containerEl = rootState.listElement || rootState.popupElement;

                if (containerEl && document.contains(containerEl)) {
                    const items = getNavigableItems(containerEl);
                    if (items.length > 0) {
                        let targetIndex = -1;
                        for (let i = 0; i < items.length; i++) {
                            if (items[i].hasAttribute('data-selected')) {
                                targetIndex = i;
                                break;
                            }
                        }
                        if (targetIndex === -1) {
                            targetIndex = findNextEnabledIndex(items, -1, 1, false);
                        }
                        if (targetIndex >= 0) {
                            setActiveItem(rootState, items, targetIndex);
                        }
                        attachScrollListener(rootState);
                        notifyScrollArrowVisibility(rootState);
                        return;
                    }
                }

                if (attempts < maxAttempts && rootState.isOpen) {
                    requestAnimationFrame(check);
                }
            }

            requestAnimationFrame(check);
        }

        waitForPopupAndFocus();

        setTimeout(() => {
            if (rootState.dotNetRef && rootState.isOpen) {
                rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', true).catch(() => { });
            }
        }, 0);
    } else {
        if (rootState.scrollLockCleanup) {
            rootState.scrollLockCleanup();
            rootState.scrollLockCleanup = null;
        }

        clearInterval(rootState.continuousScrollInterval);
        rootState.continuousScrollInterval = null;
        removeScrollListener(rootState);

        rootState.activeIndex = -1;

        if (reason !== 'outside-press') {
            requestAnimationFrame(() => {
                if (rootState.triggerElement) {
                    rootState.triggerElement.focus();
                }
            });
        }

        setTimeout(() => {
            if (rootState.dotNetRef && !rootState.isOpen) {
                rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', false).catch(() => { });
            }
        }, 0);
    }
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

export function setListElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.listElement = element;
    }
}

export function setActiveIndex(rootId, index) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.activeIndex = index;
    }
}

export function focusTrigger(element) {
    if (element) element.focus();
}

export function clearHighlights(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    const containerEl = rootState.listElement || rootState.popupElement;
    if (!containerEl) return;

    const items = containerEl.querySelectorAll('[role="option"]');
    items.forEach(item => {
        item.removeAttribute('data-highlighted');
        item.setAttribute('tabindex', '-1');
    });

    rootState.activeIndex = -1;
}

// ─── Scroll Arrow Helpers ─────────────────────────────────────────────

function checkScrollArrows(rootState) {
    const containerEl = rootState.listElement || rootState.popupElement;
    if (!containerEl) return { up: false, down: false };

    const { scrollTop, scrollHeight, clientHeight } = containerEl;
    return {
        up: scrollTop > 0,
        down: Math.round(scrollTop + clientHeight) < scrollHeight
    };
}

function notifyScrollArrowVisibility(rootState) {
    if (!rootState.dotNetRef) return;

    const visibility = checkScrollArrows(rootState);
    rootState.dotNetRef.invokeMethodAsync('OnScrollArrowVisibilityChange', visibility.up, visibility.down).catch(() => { });
}

function attachScrollListener(rootState) {
    removeScrollListener(rootState);

    const containerEl = rootState.listElement || rootState.popupElement;
    if (!containerEl) return;

    const handler = () => notifyScrollArrowVisibility(rootState);
    containerEl.addEventListener('scroll', handler, { passive: true });
    rootState.scrollListener = { element: containerEl, handler };
}

function removeScrollListener(rootState) {
    if (rootState.scrollListener) {
        rootState.scrollListener.element.removeEventListener('scroll', rootState.scrollListener.handler);
        rootState.scrollListener = null;
    }
}

export function startContinuousScroll(rootId, direction) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    clearInterval(rootState.continuousScrollInterval);

    const containerEl = rootState.listElement || rootState.popupElement;
    if (!containerEl) return;

    const scrollStep = () => {
        const items = getNavigableItems(containerEl);
        if (items.length === 0) return;

        if (direction === 'up') {
            const scrollTop = containerEl.scrollTop;
            let targetItem = null;
            for (let i = 0; i < items.length; i++) {
                if (items[i].offsetTop >= scrollTop) {
                    targetItem = items[Math.max(0, i - 1)];
                    break;
                }
            }
            if (targetItem) {
                containerEl.scrollTop = targetItem.offsetTop;
            } else {
                containerEl.scrollTop = 0;
            }
        } else {
            const scrollBottom = containerEl.scrollTop + containerEl.clientHeight;
            let targetItem = null;
            for (let i = items.length - 1; i >= 0; i--) {
                const itemBottom = items[i].offsetTop + items[i].offsetHeight;
                if (itemBottom <= scrollBottom) {
                    targetItem = items[Math.min(items.length - 1, i + 1)];
                    break;
                }
            }
            if (targetItem) {
                containerEl.scrollTop = targetItem.offsetTop + targetItem.offsetHeight - containerEl.clientHeight;
            } else {
                containerEl.scrollTop = containerEl.scrollHeight - containerEl.clientHeight;
            }
        }

        const visibility = checkScrollArrows(rootState);
        if ((direction === 'up' && !visibility.up) || (direction === 'down' && !visibility.down)) {
            clearInterval(rootState.continuousScrollInterval);
            rootState.continuousScrollInterval = null;
        }
    };

    scrollStep();
    rootState.continuousScrollInterval = setInterval(scrollStep, 40);
}

export function stopContinuousScroll(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    clearInterval(rootState.continuousScrollInterval);
    rootState.continuousScrollInterval = null;
}

// ─── Positioner API ───────────────────────────────────────────────────

export async function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking, collisionAvoidance, alignItemWithTrigger) {
    const fm = await ensureFloatingModule();

    const positionerId = await fm.initializePositioner({
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
        collisionAvoidance: collisionAvoidance || 'flip-shift'
    });

    if (alignItemWithTrigger) {
        applyAlignItemWithTrigger(positionerElement, triggerElement);
    }

    return positionerId;
}

export async function updatePosition(positionerId, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, collisionAvoidance, alignItemWithTrigger) {
    const fm = await ensureFloatingModule();

    await fm.updatePositioner(positionerId, {
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
        collisionAvoidance: collisionAvoidance || 'flip-shift'
    });

    if (alignItemWithTrigger) {
        const posState = state.positioners.get(positionerId);
        const positionerElement = posState?.element;
        if (positionerElement) {
            applyAlignItemWithTrigger(positionerElement, triggerElement);
        }
    }
}

export async function disposePositioner(positionerId) {
    const fm = await ensureFloatingModule();
    fm.disposePositioner(positionerId);
}
