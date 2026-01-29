/**
 * BlazorBaseUI Menu Component
 *
 * Menu-specific functionality that builds on the shared floating infrastructure.
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
    // Find all open menus and track the deepest one and any menubar root
    let menubarRoot = null;
    let topmostRoot = null;
    let openMenuCount = 0;
    const openMenus = [];

    for (const [id, rootState] of state.roots) {
        if (rootState.isOpen && rootState.dotNetRef) {
            openMenus.push(rootState);
            openMenuCount++;

            // Find the deepest (topmost) menu for keyboard handling
            // Nested menus (submenus) take priority over parent menus
            // Among same type, prefer the later one (more recently opened = deeper nesting)
            // Only skip update if current topmostRoot is nested and this one is not
            if (!topmostRoot || !topmostRoot.isNested || rootState.isNested) {
                topmostRoot = rootState;
            }

            if (rootState.menubarElement) {
                menubarRoot = rootState;
            }
        }
    }

    if (!topmostRoot) return;

    // Use stored direction from the root state (passed explicitly from Blazor)
    const isRtl = topmostRoot.direction === 'rtl';

    if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();

        // If closeParentOnEsc is true on the topmost (submenu), close all menus in the chain
        if (topmostRoot.closeParentOnEsc && openMenuCount > 1) {
            // Close all open menus
            for (const rootState of openMenus) {
                rootState.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
            }
        } else {
            // Just close the topmost menu (the submenu)
            topmostRoot.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
        }
        return;
    }

    // Get current item info for submenu handling
    const currentItem = topmostRoot.popupElement ?
        getMenuItems(topmostRoot.popupElement)[topmostRoot.activeIndex ?? -1] : null;
    const isSubmenuTrigger = currentItem?.hasAttribute('aria-haspopup');
    const isInSubmenu = openMenuCount > 1;

    // Determine which arrow key opens/closes submenus based on direction
    // LTR: ArrowRight opens, ArrowLeft closes
    // RTL: ArrowLeft opens, ArrowRight closes
    const openSubmenuKey = isRtl ? 'ArrowLeft' : 'ArrowRight';
    const closeSubmenuKey = isRtl ? 'ArrowRight' : 'ArrowLeft';

    // Handle arrow key to open submenu (for vertical menus)
    if (e.key === openSubmenuKey && topmostRoot.orientation !== 'horizontal' && isSubmenuTrigger) {
        e.preventDefault();
        e.stopPropagation();
        currentItem.click();
        return;
    }

    // Handle arrow key to close submenu (for vertical menus when in a submenu)
    if (e.key === closeSubmenuKey && topmostRoot.orientation !== 'horizontal' && isInSubmenu) {
        e.preventDefault();
        e.stopPropagation();
        topmostRoot.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
        return;
    }

    // Handle ArrowLeft/Right for menubar navigation
    if (menubarRoot && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
        // For open submenu key on a submenu trigger in menubar context, open it
        if (e.key === openSubmenuKey && isSubmenuTrigger) {
            e.preventDefault();
            e.stopPropagation();
            currentItem.click();
            return;
        }

        // Navigate to sibling menubar item
        e.preventDefault();
        e.stopPropagation();

        // Direction for menubar navigation (also respects RTL)
        const navDirection = (e.key === 'ArrowRight') !== isRtl ? 1 : -1;
        navigateMenubarSibling(menubarRoot, navDirection);
        return;
    }

    // Handle arrow key navigation within menu
    if (topmostRoot.popupElement) {
        const items = getMenuItems(topmostRoot.popupElement);
        if (items.length === 0) return;

        const currentIndex = topmostRoot.activeIndex ?? -1;
        let newIndex = currentIndex;
        const isHorizontal = topmostRoot.orientation === 'horizontal';

        // Map arrow keys based on orientation
        const nextKey = isHorizontal ? 'ArrowRight' : 'ArrowDown';
        const prevKey = isHorizontal ? 'ArrowLeft' : 'ArrowUp';

        switch (e.key) {
            case 'ArrowDown':
            case 'ArrowRight':
                if ((isHorizontal && e.key === 'ArrowRight') || (!isHorizontal && e.key === 'ArrowDown')) {
                    e.preventDefault();
                    newIndex = currentIndex < items.length - 1 ? currentIndex + 1 : (topmostRoot.loopFocus ? 0 : currentIndex);
                }
                break;
            case 'ArrowUp':
            case 'ArrowLeft':
                if ((isHorizontal && e.key === 'ArrowLeft') || (!isHorizontal && e.key === 'ArrowUp')) {
                    e.preventDefault();
                    newIndex = currentIndex > 0 ? currentIndex - 1 : (topmostRoot.loopFocus ? items.length - 1 : currentIndex);
                }
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

function navigateMenubarSibling(menubarRoot, direction) {
    const menubarElement = menubarRoot.menubarElement;
    if (!menubarElement) return;

    // Get all menubar triggers
    const triggers = Array.from(menubarElement.querySelectorAll('[aria-haspopup="menu"]'));
    if (triggers.length === 0) return;

    // Find the current trigger (the one whose menu is open)
    const currentTrigger = menubarRoot.triggerElement;
    const currentIndex = triggers.indexOf(currentTrigger);
    if (currentIndex === -1) return;

    // Calculate next index (with wrapping)
    let nextIndex = currentIndex + direction;
    if (nextIndex < 0) nextIndex = triggers.length - 1;
    if (nextIndex >= triggers.length) nextIndex = 0;

    const nextTrigger = triggers[nextIndex];
    if (!nextTrigger || nextTrigger === currentTrigger) return;

    // Close current menu first
    menubarRoot.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });

    // Focus the next trigger and click it to open its menu
    setTimeout(() => {
        nextTrigger.focus();
        nextTrigger.click();
    }, 10);
}

function getTextDirection(element) {
    if (!element) return 'ltr';

    // Check computed style direction
    const computedDirection = getComputedStyle(element).direction;
    if (computedDirection === 'rtl') return 'rtl';

    // Also check dir attribute up the DOM tree
    let current = element;
    while (current) {
        const dir = current.getAttribute?.('dir');
        if (dir === 'rtl' || dir === 'ltr') return dir;
        current = current.parentElement;
    }

    // Default to document direction or 'ltr'
    return document.documentElement.getAttribute('dir') || 'ltr';
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

// ============================================================================
// Root Management
// ============================================================================

export function initializeRoot(rootId, dotNetRef, closeParentOnEsc, loopFocus, modal, menubarElement, orientation, highlightItemOnHover, direction, isNested) {
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
        releaseScrollLock: null,
        hoverInteraction: null,
        menubarElement: menubarElement || null,
        orientation: orientation || 'vertical',
        highlightItemOnHover: highlightItemOnHover ?? true,
        direction: direction || 'ltr',
        isNested: isNested || false
    });
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        // Release scroll lock if this menu had it
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }
        // Clean up hover interaction
        if (rootState.hoverInteraction) {
            rootState.hoverInteraction.cleanup();
        }
    }
    state.roots.delete(rootId);
}

// ============================================================================
// Hover Interaction Support
// ============================================================================

export async function initializeHoverInteraction(rootId, triggerElement, openDelay, closeDelay) {
    let rootState = state.roots.get(rootId);

    // If root state doesn't exist yet, wait with retries for it to be initialized
    // This handles the case where the trigger's OnAfterRender runs before the root's InitializeJsAsync completes
    // On Server-side Blazor, SignalR latency can make this take longer, so we allow more retries
    if (!rootState) {
        for (let attempt = 0; attempt < 30; attempt++) {
            await new Promise(resolve => setTimeout(resolve, 100));
            rootState = state.roots.get(rootId);
            if (rootState) break;
        }
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
        interactionId: `menu-hover-${rootId}`,
        triggerElement: rootState.triggerElement,
        floatingElement: rootState.popupElement,
        openDelay: openDelay || 0,
        closeDelay: closeDelay || 0,
        mouseOnly: true,
        useSafePolygon: true,
        safePolygonOptions: { blockPointerEvents: false },
        onOpen: (reason) => {
            // Skip if we're within the ignore period (e.g., after keyboard close)
            if (rootState.ignoreHoverUntil && Date.now() < rootState.ignoreHoverUntil) {
                return;
            }
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

// ============================================================================
// Open/Close State
// ============================================================================

export async function setRootOpen(rootId, isOpen, reason, highlightLast) {
    let rootState = state.roots.get(rootId);

    // If root state doesn't exist yet, wait briefly for it to be initialized
    // This handles the race condition on Server-side Blazor where setRootOpen
    // may be called before initializeRoot completes
    if (!rootState) {
        await new Promise(resolve => setTimeout(resolve, 50));
        rootState = state.roots.get(rootId);
        if (!rootState) return;
    }

    rootState.isOpen = isOpen;
    rootState.pendingOpen = isOpen;
    rootState.openReason = reason;
    rootState.highlightLast = highlightLast || false;

    // Sync with hover interaction
    if (rootState.hoverInteraction) {
        rootState.hoverInteraction.setOpen(isOpen);
    }

    if (isOpen) {
        // For menubar menus, don't auto-highlight - user must press arrow key first
        // For other menus, start with first item highlighted (accessibility best practice)
        // If highlightLast is true, we'll set the index after we know the item count
        rootState.activeIndex = rootState.menubarElement ? -1 : (highlightLast ? -2 : 0);

        // Apply scroll lock if modal and not opened via hover (guard against double acquisition)
        if (rootState.modal && reason !== 'trigger-hover' && !rootState.releaseScrollLock) {
            rootState.releaseScrollLock = acquireScrollLock(rootState.positionerElement);
        }

        waitForPopupAndStartTransition(rootState, isOpen);
    } else {
        // Release scroll lock if this menu acquired it
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }

        // Return focus to trigger when menu closes via keyboard or item click
        // Don't focus trigger for hover-based closes or outside clicks (user clicked elsewhere)
        const shouldFocusTrigger = reason === 'escape-key' || reason === 'item-press' || reason === 'close-press';
        if (shouldFocusTrigger && rootState.triggerElement) {
            // Use setTimeout to ensure focus happens after the menu is fully closed
            setTimeout(() => {
                if (rootState.triggerElement && document.contains(rootState.triggerElement)) {
                    rootState.triggerElement.focus();
                }
            }, 0);
        }

        // For keyboard closes, temporarily disable hover interaction to prevent immediate reopen
        // This handles the case where mouse is still hovering when Escape is pressed
        if (reason === 'escape-key' && rootState.hoverInteraction) {
            rootState.ignoreHoverUntil = Date.now() + 300;
        }

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

// ============================================================================
// Transition Handling
// ============================================================================

function waitForPopupAndStartTransition(rootState, isOpen) {
    const popupElement = rootState.popupElement;

    if (popupElement) {
        if (isOpen) {
            // Wait for menu items to be rendered before highlighting
            waitForItemsAndHighlight(rootState, popupElement);
        }
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
                if (isOpen) {
                    // Wait for menu items to be rendered before highlighting
                    waitForItemsAndHighlight(rootState, element);
                }
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

function waitForItemsAndHighlight(rootState, popupElement) {
    // -1 means no highlight (menubar), -2 means highlight last item
    if (rootState.activeIndex === -1) return;

    let attempts = 0;
    const maxAttempts = 10;

    function checkForItems() {
        attempts++;
        const items = getMenuItems(popupElement);

        if (items.length > 0) {
            // If activeIndex is -2, highlight the last item
            let indexToHighlight = rootState.activeIndex;
            if (indexToHighlight === -2) {
                indexToHighlight = items.length - 1;
                rootState.activeIndex = indexToHighlight;
                // Notify .NET of the actual index
                if (rootState.dotNetRef) {
                    rootState.dotNetRef.invokeMethodAsync('OnActiveIndexChange', indexToHighlight).catch(() => { });
                }
            }
            highlightItem(popupElement, items, indexToHighlight);
        } else if (attempts < maxAttempts && rootState.isOpen) {
            requestAnimationFrame(checkForItems);
        }
    }

    requestAnimationFrame(checkForItems);
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
                if (hasTransition) {
                    setupTransitionEndListener(rootState, isOpen);
                } else {
                    // No transition - immediately notify that transition is complete
                    if (rootState.dotNetRef) {
                        rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
                    }
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

async function setupTransitionEndListener(rootState, isOpen) {
    const popupElement = rootState.popupElement;
    if (!popupElement) return;

    const floating = await ensureFloatingModule();

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

export async function setTriggerElement(rootId, element) {
    let rootState = state.roots.get(rootId);

    // Wait for root state to be initialized if not yet available
    if (!rootState) {
        for (let attempt = 0; attempt < 10; attempt++) {
            await new Promise(resolve => setTimeout(resolve, 50));
            rootState = state.roots.get(rootId);
            if (rootState) break;
        }
        if (!rootState) return;
    }

    rootState.triggerElement = element;
}

export async function setPopupElement(rootId, element) {
    let rootState = state.roots.get(rootId);

    // Wait for root state to be initialized if not yet available
    if (!rootState) {
        for (let attempt = 0; attempt < 10; attempt++) {
            await new Promise(resolve => setTimeout(resolve, 50));
            rootState = state.roots.get(rootId);
            if (rootState) break;
        }
        if (!rootState) return;
    }

    rootState.popupElement = element;
    // Update hover interaction with the new popup element
    if (rootState.hoverInteraction && element) {
        rootState.hoverInteraction.setFloatingElement(element);
    }
}

// ============================================================================
// Positioning (delegated to shared floating module)
// ============================================================================

export async function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking, collisionAvoidance) {
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
        positionMethod: positionMethod || 'absolute',
        disableAnchorTracking: disableAnchorTracking || false,
        collisionAvoidance: collisionAvoidance || 'flip-shift'
    });

    if (positionerId) {
        state.positioners.set(positionerId, { positionerId });
    }

    return positionerId;
}

export async function updatePosition(positionerId, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, collisionAvoidance) {
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
        positionMethod: positionMethod || 'absolute',
        collisionAvoidance: collisionAvoidance || 'flip-shift'
    });
}

export async function disposePositioner(positionerId) {
    const floating = await ensureFloatingModule();
    floating.disposePositioner(positionerId);
    state.positioners.delete(positionerId);
}
