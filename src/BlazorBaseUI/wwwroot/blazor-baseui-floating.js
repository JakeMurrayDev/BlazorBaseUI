/**
 * BlazorBaseUI Floating Infrastructure
 *
 * Shared positioning and interaction utilities for floating elements.
 * Ported from Base UI's floating-ui-react implementation.
 * Uses vendored Floating UI library for positioning.
 */

const STATE_KEY = Symbol.for('BlazorBaseUI.Floating.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        trees: new Map(),
        positioners: new Map(),
        interactions: new Map(),
        floatingUIPromise: null
    };
}

const state = window[STATE_KEY];

// ============================================================================
// Browser Detection
// ============================================================================

export function isSafari() {
    return typeof navigator !== 'undefined' && /apple/i.test(navigator.vendor);
}

// ============================================================================
// Floating UI Library Loading
// ============================================================================

const FLOATING_UI_KEY = Symbol.for('BlazorBaseUI.FloatingUI');

async function loadScript(src) {
    return new Promise((resolve, reject) => {
        const existing = document.querySelector(`script[src="${src}"]`);
        if (existing) {
            // If already loaded, resolve immediately
            if (existing.dataset.loaded === 'true') {
                resolve();
                return;
            }
            // If still loading, wait for it
            existing.addEventListener('load', resolve);
            existing.addEventListener('error', reject);
            return;
        }

        const script = document.createElement('script');
        script.src = src;
        script.onload = () => {
            script.dataset.loaded = 'true';
            resolve();
        };
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

async function ensureFloatingUI() {
    if (window[FLOATING_UI_KEY]) {
        return window[FLOATING_UI_KEY];
    }

    if (state.floatingUIPromise) {
        return state.floatingUIPromise;
    }

    state.floatingUIPromise = (async () => {
        const basePath = './_content/BlazorBaseUI/vendor/';

        await loadScript(basePath + 'floating-ui.core.min.js');
        await loadScript(basePath + 'floating-ui.dom.min.js');

        if (typeof FloatingUIDOM !== 'undefined') {
            window[FLOATING_UI_KEY] = FloatingUIDOM;
            return FloatingUIDOM;
        }

        throw new Error('Failed to load Floating UI libraries');
    })();

    return state.floatingUIPromise;
}

// ============================================================================
// Constants
// ============================================================================

const TYPEABLE_SELECTOR = 'input:not([type="hidden"]):not([disabled]),' +
    '[contenteditable]:not([contenteditable="false"]),' +
    'textarea:not([disabled])';

// ============================================================================
// Event Emitter
// ============================================================================

export function createEventEmitter() {
    const map = new Map();
    return {
        emit(event, data) {
            map.get(event)?.forEach(listener => listener(data));
        },
        on(event, listener) {
            if (!map.has(event)) {
                map.set(event, new Set());
            }
            map.get(event).add(listener);
        },
        off(event, listener) {
            map.get(event)?.delete(listener);
        }
    };
}

// ============================================================================
// DOM Utilities
// ============================================================================

export function activeElement(doc) {
    let element = doc.activeElement;
    while (element?.shadowRoot?.activeElement != null) {
        element = element.shadowRoot.activeElement;
    }
    return element;
}

export function contains(parent, child) {
    if (!parent || !child) {
        return false;
    }

    const rootNode = child.getRootNode?.();

    // First, attempt with faster native method
    if (parent.contains(child)) {
        return true;
    }

    // Fallback to custom implementation with Shadow DOM support
    if (rootNode && rootNode.nodeType === 11) { // ShadowRoot
        let next = child;
        while (next) {
            if (parent === next) {
                return true;
            }
            next = next.parentNode || next.host;
        }
    }

    return false;
}

export function getTarget(event) {
    if ('composedPath' in event) {
        return event.composedPath()[0];
    }
    return event.target;
}

export function isEventTargetWithin(event, node) {
    if (node == null) {
        return false;
    }

    if ('composedPath' in event) {
        return event.composedPath().includes(node);
    }

    return event.target != null && node.contains(event.target);
}

export function isRootElement(element) {
    return element.matches('html,body');
}

export function getDocument(node) {
    return node?.ownerDocument || document;
}

export function isTypeableElement(element) {
    return element instanceof HTMLElement && element.matches(TYPEABLE_SELECTOR);
}

export function isMouseLikePointerType(pointerType, strict = false) {
    const pointer = pointerType;
    if (!pointer) {
        return !strict;
    }
    return pointer === 'mouse' || pointer === 'pen';
}

export function isHTMLElement(value) {
    return value instanceof HTMLElement;
}

export function isElement(value) {
    return value instanceof Element;
}

// ============================================================================
// Floating Tree Store
// ============================================================================

export class FloatingTreeStore {
    constructor() {
        this.nodes = [];
        this.events = createEventEmitter();
    }

    addNode(node) {
        this.nodes.push(node);
    }

    removeNode(node) {
        const index = this.nodes.findIndex(n => n === node);
        if (index !== -1) {
            this.nodes.splice(index, 1);
        }
    }

    getNodeChildren(nodeId) {
        return this.nodes.filter(node => node.parentId === nodeId);
    }

    getNodeAncestors(nodeId) {
        const ancestors = [];
        let current = this.nodes.find(n => n.id === nodeId);
        while (current?.parentId) {
            const parent = this.nodes.find(n => n.id === current.parentId);
            if (parent) {
                ancestors.push(parent);
                current = parent;
            } else {
                break;
            }
        }
        return ancestors;
    }
}

export function getFloatingTree(treeId) {
    if (!state.trees.has(treeId)) {
        state.trees.set(treeId, new FloatingTreeStore());
    }
    return state.trees.get(treeId);
}

export function disposeFloatingTree(treeId) {
    state.trees.delete(treeId);
}

// ============================================================================
// Timeout Utility
// ============================================================================

export class Timeout {
    constructor() {
        this.id = null;
    }

    start(ms, callback) {
        this.clear();
        this.id = setTimeout(callback, ms);
    }

    clear() {
        if (this.id !== null) {
            clearTimeout(this.id);
            this.id = null;
        }
    }
}

// ============================================================================
// Scroll Utilities
// ============================================================================

export function getScrollParents(element) {
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

export function getCollisionBounds(element, collisionBoundary) {
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

// ============================================================================
// Positioning Engine (using Floating UI)
// ============================================================================

/**
 * Converts our side/align to Floating UI placement.
 */
function toFloatingPlacement(side, align) {
    // Handle logical sides
    const isRtl = document.documentElement.dir === 'rtl';
    let physicalSide = side;
    if (side === 'inline-end') {
        physicalSide = isRtl ? 'left' : 'right';
    } else if (side === 'inline-start') {
        physicalSide = isRtl ? 'right' : 'left';
    }

    if (align === 'center') {
        return physicalSide;
    }
    return `${physicalSide}-${align}`;
}

/**
 * Parses a Floating UI placement back to side/align.
 */
function fromFloatingPlacement(placement) {
    const parts = placement.split('-');
    const side = parts[0];
    const align = parts[1] || 'center';
    return { side, align };
}

export async function initializePositioner(options) {
    const {
        positionerElement,
        triggerElement,
        virtualId = null,
        side = 'bottom',
        align = 'center',
        sideOffset = 0,
        alignOffset = 0,
        collisionPadding = 0,
        collisionBoundary = 'clipping-ancestors',
        arrowPadding = 0,
        arrowElement = null,
        sticky = false,
        positionMethod = 'absolute',
        disableAnchorTracking = false,
        collisionAvoidance = null
    } = options;

    // Support virtual element as alternative to DOM trigger
    const effectiveTrigger = virtualId
        ? virtualElements.get(virtualId)
        : triggerElement;

    if (!positionerElement || !effectiveTrigger) return null;

    const positionerId = positionerElement.id ||
        `positioner-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

    const positionerState = {
        positionerId,
        positionerElement,
        triggerElement: effectiveTrigger,
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
        collisionAvoidance,
        onPositionUpdated: options.onPositionUpdated || null,
        dotNetRef: options.dotNetRef || null,
        hasSideOffsetFn: options.hasSideOffsetFn || false,
        hasAlignOffsetFn: options.hasAlignOffsetFn || false,
        cleanup: null,
        computedSide: side,
        computedAlign: align
    };

    state.positioners.set(positionerId, positionerState);
    await updatePositionInternal(positionerState);

    if (!disableAnchorTracking) {
        await setupAutoUpdate(positionerState);
    }

    return positionerId;
}

export async function updatePositioner(positionerId, options) {
    const positionerState = state.positioners.get(positionerId);
    if (!positionerState) return;

    Object.assign(positionerState, options);
    await updatePositionInternal(positionerState);
}

export function disposePositioner(positionerId) {
    if (!positionerId) return;

    const positionerState = state.positioners.get(positionerId);
    if (positionerState) {
        cleanupAutoUpdate(positionerState);
        state.positioners.delete(positionerId);
    }
}

async function setupAutoUpdate(positionerState) {
    const { positionerElement, triggerElement } = positionerState;
    if (!positionerElement || !triggerElement) return;

    cleanupAutoUpdate(positionerState);

    try {
        const FloatingUI = await ensureFloatingUI();

        const cleanup = FloatingUI.autoUpdate(
            triggerElement,
            positionerElement,
            () => updatePositionInternal(positionerState),
            {
                ancestorScroll: true,
                ancestorResize: true,
                elementResize: true,
                layoutShift: true
            }
        );

        positionerState.cleanup = cleanup;
    } catch {
        // Fallback to manual scroll/resize listeners if Floating UI fails
        const update = () => updatePositionInternal(positionerState);
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
}

function cleanupAutoUpdate(positionerState) {
    if (positionerState.cleanup) {
        positionerState.cleanup();
        positionerState.cleanup = null;
    }
}

/**
 * Normalizes collision avoidance settings from either string format (menu)
 * or object format (popover/tooltip) into a consistent object.
 */
function normalizeCollisionAvoidance(collisionAvoidance) {
    if (!collisionAvoidance) {
        return { side: 'flip', align: 'flip', fallbackAxisSide: 'end' };
    }
    if (typeof collisionAvoidance === 'string') {
        switch (collisionAvoidance) {
            case 'none': return { side: 'none', align: 'none', fallbackAxisSide: 'none' };
            case 'shift': return { side: 'shift', align: 'shift', fallbackAxisSide: 'none' };
            case 'flip': return { side: 'flip', align: 'none', fallbackAxisSide: 'none' };
            case 'flip-shift': return { side: 'flip', align: 'shift', fallbackAxisSide: 'none' };
            default: return { side: 'flip', align: 'flip', fallbackAxisSide: 'end' };
        }
    }
    return {
        side: collisionAvoidance.side || 'flip',
        align: collisionAvoidance.align || 'flip',
        fallbackAxisSide: collisionAvoidance.fallbackAxisSide || 'end'
    };
}

async function updatePositionInternal(positionerState) {
    const {
        positionerElement,
        triggerElement,
        side,
        align,
        sideOffset,
        alignOffset,
        collisionPadding,
        arrowElement,
        sticky,
        positionMethod,
        collisionAvoidance
    } = positionerState;

    if (!positionerElement || !triggerElement) return;

    try {
        const FloatingUI = await ensureFloatingUI();

        const placement = toFloatingPlacement(side, align);
        const ca = normalizeCollisionAvoidance(collisionAvoidance);

        // Build middleware array
        const middleware = [];

        // Offset middleware
        if (positionerState.hasSideOffsetFn || positionerState.hasAlignOffsetFn) {
            const dotNetRef = positionerState.dotNetRef;
            middleware.push(FloatingUI.offset(async ({ rects, placement }) => {
                const parts = placement.split('-');
                const data = {
                    anchorWidth: rects.reference.width,
                    anchorHeight: rects.reference.height,
                    popupWidth: rects.floating.width,
                    popupHeight: rects.floating.height,
                    side: parts[0],
                    align: parts[1] || 'center'
                };
                const mainAxis = positionerState.hasSideOffsetFn
                    ? await dotNetRef.invokeMethodAsync('ComputeSideOffset', data)
                    : sideOffset;
                const crossAxis = positionerState.hasAlignOffsetFn
                    ? await dotNetRef.invokeMethodAsync('ComputeAlignOffset', data)
                    : alignOffset;
                return { mainAxis, crossAxis };
            }));
        } else if (sideOffset !== 0 || alignOffset !== 0) {
            middleware.push(FloatingUI.offset({
                mainAxis: sideOffset,
                crossAxis: alignOffset
            }));
        }

        // Flip middleware (handles side collision)
        const flipMiddleware = ca.side === 'none' ? null : FloatingUI.flip({
            padding: collisionPadding,
            mainAxis: ca.side === 'flip',
            crossAxis: ca.align === 'flip' ? 'alignment' : false,
            fallbackAxisSideDirection: ca.fallbackAxisSide
        });

        // Shift middleware (handles alignment collision)
        const shiftDisabled = ca.align === 'none' && ca.side !== 'shift';
        const crossAxisShiftEnabled = !shiftDisabled && (sticky || ca.side === 'shift');

        const shiftMiddleware = shiftDisabled ? null : FloatingUI.shift({
            padding: collisionPadding,
            mainAxis: ca.align !== 'none',
            crossAxis: crossAxisShiftEnabled,
            limiter: sticky ? undefined : FloatingUI.limitShift()
        });

        // Order matters: shift before flip when side/align is 'shift' or align is 'center'
        if (ca.side === 'shift' || ca.align === 'shift' || align === 'center') {
            if (shiftMiddleware) middleware.push(shiftMiddleware);
            if (flipMiddleware) middleware.push(flipMiddleware);
        } else {
            if (flipMiddleware) middleware.push(flipMiddleware);
            if (shiftMiddleware) middleware.push(shiftMiddleware);
        }

        // Size middleware for available space
        middleware.push(FloatingUI.size({
            padding: collisionPadding,
            apply({ availableWidth, availableHeight, rects }) {
                positionerElement.style.setProperty('--available-width', `${availableWidth}px`);
                positionerElement.style.setProperty('--available-height', `${availableHeight}px`);
                positionerElement.style.setProperty('--anchor-width', `${rects.reference.width}px`);
                positionerElement.style.setProperty('--anchor-height', `${rects.reference.height}px`);
            }
        }));

        // Hide middleware for anchor detection
        middleware.push(FloatingUI.hide({
            strategy: 'referenceHidden'
        }));

        // Arrow middleware
        if (arrowElement) {
            middleware.push(FloatingUI.arrow({
                element: arrowElement,
                padding: positionerState.arrowPadding || 0
            }));
        }

        const { x, y, placement: finalPlacement, middlewareData } = await FloatingUI.computePosition(
            triggerElement,
            positionerElement,
            {
                placement,
                strategy: positionMethod === 'absolute' ? 'absolute' : 'fixed',
                middleware
            }
        );

        // Parse the final placement
        const { side: effectiveSide, align: effectiveAlign } = fromFloatingPlacement(finalPlacement);

        // Apply position styles
        positionerElement.style.position = positionMethod === 'absolute' ? 'absolute' : 'fixed';
        positionerElement.style.left = `${x}px`;
        positionerElement.style.top = `${y}px`;
        positionerElement.style.zIndex = '1000';

        // Set CSS custom properties
        positionerElement.style.setProperty('--positioner-width', `${positionerElement.offsetWidth}px`);
        positionerElement.style.setProperty('--positioner-height', `${positionerElement.offsetHeight}px`);

        // Set data attributes
        positionerElement.setAttribute('data-side', effectiveSide);
        positionerElement.setAttribute('data-align', effectiveAlign);

        // Handle anchor hidden state
        if (middlewareData.hide?.referenceHidden) {
            positionerElement.setAttribute('data-anchor-hidden', '');
        } else {
            positionerElement.removeAttribute('data-anchor-hidden');
        }

        // Calculate transform origin
        let transformOriginX, transformOriginY;
        if (effectiveSide === 'top' || effectiveSide === 'bottom') {
            transformOriginX = effectiveAlign === 'start' ? '0%' : effectiveAlign === 'end' ? '100%' : '50%';
            transformOriginY = effectiveSide === 'top' ? '100%' : '0%';
        } else {
            transformOriginX = effectiveSide === 'left' ? '100%' : '0%';
            transformOriginY = effectiveAlign === 'start' ? '0%' : effectiveAlign === 'end' ? '100%' : '50%';
        }
        positionerElement.style.setProperty('--transform-origin', `${transformOriginX} ${transformOriginY}`);

        // Update arrow position if present
        // Floating UI's arrow middleware returns:
        // - x (horizontal offset) ONLY for vertical placements (top/bottom)
        // - y (vertical offset) ONLY for horizontal placements (left/right)
        // The perpendicular axis positioning (how far arrow extends from popup edge)
        // is handled by CSS using the data-side attribute, matching Base UI's approach.
        if (arrowElement && middlewareData.arrow) {
            const { x: arrowX, y: arrowY } = middlewareData.arrow;

            // Set data-side attribute for CSS-based perpendicular positioning
            arrowElement.setAttribute('data-side', effectiveSide);

            // Set position (matching Base UI's arrowStyles)
            arrowElement.style.position = 'absolute';

            // Set along-edge coordinate and clear perpendicular axis for CSS to handle
            // For top/bottom placements: set left (centering), clear top/bottom for CSS
            // For left/right placements: set top (centering), clear left/right for CSS
            if (effectiveSide === 'top' || effectiveSide === 'bottom') {
                arrowElement.style.left = arrowX != null ? `${arrowX}px` : '';
                arrowElement.style.right = '';
                arrowElement.style.top = '';
                arrowElement.style.bottom = '';
            } else {
                arrowElement.style.top = arrowY != null ? `${arrowY}px` : '';
                arrowElement.style.bottom = '';
                arrowElement.style.left = '';
                arrowElement.style.right = '';
            }
        }

        // Store computed placement for safePolygon
        positionerState.computedSide = effectiveSide;
        positionerState.computedAlign = effectiveAlign;

        // Notify C# of computed position values
        if (positionerState.onPositionUpdated) {
            positionerState.onPositionUpdated(effectiveSide, effectiveAlign, !!middlewareData.hide?.referenceHidden);
        }

    } catch {
        // Fallback to basic positioning if Floating UI fails
        updatePositionFallback(positionerState);
    }
}

/**
 * Fallback positioning when Floating UI is not available.
 */
function updatePositionFallback(positionerState) {
    const {
        positionerElement,
        triggerElement,
        side,
        align,
        sideOffset,
        positionMethod
    } = positionerState;

    if (!positionerElement || !triggerElement) return;

    const triggerRect = triggerElement.getBoundingClientRect();
    const popupWidth = positionerElement.offsetWidth || 200;
    const popupHeight = positionerElement.offsetHeight || 100;

    let top = 0;
    let left = 0;

    switch (side) {
        case 'top':
            top = triggerRect.top - popupHeight - sideOffset;
            break;
        case 'bottom':
            top = triggerRect.bottom + sideOffset;
            break;
        case 'left':
            left = triggerRect.left - popupWidth - sideOffset;
            break;
        case 'right':
            left = triggerRect.right + sideOffset;
            break;
    }

    if (side === 'top' || side === 'bottom') {
        switch (align) {
            case 'start':
                left = triggerRect.left;
                break;
            case 'center':
                left = triggerRect.left + (triggerRect.width - popupWidth) / 2;
                break;
            case 'end':
                left = triggerRect.right - popupWidth;
                break;
        }
    } else {
        switch (align) {
            case 'start':
                top = triggerRect.top;
                break;
            case 'center':
                top = triggerRect.top + (triggerRect.height - popupHeight) / 2;
                break;
            case 'end':
                top = triggerRect.bottom - popupHeight;
                break;
        }
    }

    if (positionMethod === 'absolute') {
        top += window.scrollY;
        left += window.scrollX;
    }

    positionerElement.style.position = positionMethod === 'absolute' ? 'absolute' : 'fixed';
    positionerElement.style.top = `${top}px`;
    positionerElement.style.left = `${left}px`;
    positionerElement.style.zIndex = '1000';

    positionerElement.setAttribute('data-side', side);
    positionerElement.setAttribute('data-align', align);

    positionerState.computedSide = side;
    positionerState.computedAlign = align;
}

// ============================================================================
// Transition Utilities
// ============================================================================

export function parseCssDuration(durationStr) {
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

export function checkForTransitionOrAnimation(element) {
    const style = getComputedStyle(element);
    const transitionDuration = parseCssDuration(style.transitionDuration);
    const hasTransition = transitionDuration > 0;
    const animationName = style.animationName;
    const animationDuration = parseCssDuration(style.animationDuration);
    const hasAnimation = animationName && animationName !== 'none' && animationDuration > 0;
    return hasTransition || hasAnimation;
}

export function getMaxTransitionDuration(element) {
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

// ============================================================================
// Safe Polygon Algorithm
// ============================================================================

/**
 * Checks if a point is inside a polygon using ray casting algorithm.
 * @param {[number, number]} point - [x, y] coordinates
 * @param {Array<[number, number]>} polygon - Array of [x, y] vertices
 * @returns {boolean}
 */
function isPointInPolygon(point, polygon) {
    const [x, y] = point;
    let isInside = false;
    const length = polygon.length;

    for (let i = 0, j = length - 1; i < length; j = i++) {
        const [xi, yi] = polygon[i] || [0, 0];
        const [xj, yj] = polygon[j] || [0, 0];
        const intersect = yi >= y !== yj >= y && x <= ((xj - xi) * (y - yi)) / (yj - yi) + xi;
        if (intersect) {
            isInside = !isInside;
        }
    }
    return isInside;
}

/**
 * Checks if a point is inside a rectangle.
 * @param {[number, number]} point - [x, y] coordinates
 * @param {Object} rect - Rectangle with x, y, width, height
 * @returns {boolean}
 */
function isPointInRect(point, rect) {
    return (
        point[0] >= rect.x &&
        point[0] <= rect.x + rect.width &&
        point[1] >= rect.y &&
        point[1] <= rect.y + rect.height
    );
}

/**
 * Creates a safe polygon handler for hover interactions.
 * This allows the user to move their cursor from the trigger to the floating element
 * without it closing prematurely.
 *
 * @param {Object} options
 * @param {number} options.buffer - Buffer zone around the polygon (default: 0.5)
 * @param {boolean} options.blockPointerEvents - Whether to block pointer events on other elements
 * @param {boolean} options.requireIntent - Whether to require cursor movement intent
 * @returns {Function} Handler function for mousemove events
 */
export function safePolygon(options = {}) {
    const { buffer = 0.5, blockPointerEvents = false, requireIntent = true } = options;

    const timeout = new Timeout();
    let hasLanded = false;
    let lastX = null;
    let lastY = null;
    let lastCursorTime = typeof performance !== 'undefined' ? performance.now() : 0;

    function getCursorSpeed(x, y) {
        const currentTime = performance.now();
        const elapsedTime = currentTime - lastCursorTime;

        if (lastX === null || lastY === null || elapsedTime === 0) {
            lastX = x;
            lastY = y;
            lastCursorTime = currentTime;
            return null;
        }

        const deltaX = x - lastX;
        const deltaY = y - lastY;
        const distance = Math.sqrt(deltaX * deltaX + deltaY * deltaY);
        const speed = distance / elapsedTime; // px / ms

        lastX = x;
        lastY = y;
        lastCursorTime = currentTime;

        return speed;
    }

    /**
     * @param {Object} context
     * @param {number} context.x - Last cursor X position when leaving reference
     * @param {number} context.y - Last cursor Y position when leaving reference
     * @param {string} context.placement - Current placement (top, bottom, left, right)
     * @param {HTMLElement} context.referenceElement - The trigger element
     * @param {HTMLElement} context.floatingElement - The floating element
     * @param {Function} context.onClose - Callback to close the floating element
     * @param {Object} context.tree - Optional floating tree for nested elements
     * @param {string} context.nodeId - Optional node ID in the tree
     */
    function handleClose(context) {
        const { x, y, placement, referenceElement, floatingElement, onClose, tree, nodeId } = context;

        return function onMouseMove(event) {
            function close() {
                timeout.clear();
                onClose();
            }

            timeout.clear();

            if (!referenceElement || !floatingElement || placement == null || x == null || y == null) {
                return;
            }

            const { clientX, clientY } = event;
            const clientPoint = [clientX, clientY];
            const target = getTarget(event);
            const isLeave = event.type === 'mouseleave';
            const isOverFloating = contains(floatingElement, target);
            const isOverReference = contains(referenceElement, target);
            const refRect = referenceElement.getBoundingClientRect();
            const floatRect = floatingElement.getBoundingClientRect();
            const side = placement.split('-')[0];
            const cursorLeaveFromRight = x > floatRect.right - floatRect.width / 2;
            const cursorLeaveFromBottom = y > floatRect.bottom - floatRect.height / 2;
            const isOverReferenceRect = isPointInRect(clientPoint, {
                x: refRect.left,
                y: refRect.top,
                width: refRect.width,
                height: refRect.height
            });
            const isFloatingWider = floatRect.width > refRect.width;
            const isFloatingTaller = floatRect.height > refRect.height;
            const left = (isFloatingWider ? refRect : floatRect).left;
            const right = (isFloatingWider ? refRect : floatRect).right;
            const top = (isFloatingTaller ? refRect : floatRect).top;
            const bottom = (isFloatingTaller ? refRect : floatRect).bottom;

            if (isOverFloating) {
                hasLanded = true;
                if (!isLeave) {
                    return;
                }
            }

            if (isOverReference) {
                hasLanded = false;
            }

            if (isOverReference && !isLeave) {
                hasLanded = true;
                return;
            }

            // Prevent overlapping floating element from being stuck in an open-close loop
            if (isLeave && event.relatedTarget instanceof Element && contains(floatingElement, event.relatedTarget)) {
                return;
            }

            // If any nested child is open, abort
            if (tree && nodeId) {
                const children = tree.getNodeChildren(nodeId);
                if (children.some(child => child.context?.open)) {
                    return;
                }
            }

            // If the pointer is leaving from the opposite side, close immediately
            if (
                (side === 'top' && y >= refRect.bottom - 1) ||
                (side === 'bottom' && y <= refRect.top + 1) ||
                (side === 'left' && x >= refRect.right - 1) ||
                (side === 'right' && x <= refRect.left + 1)
            ) {
                return close();
            }

            // Build rectangular trough polygon between trigger and floating element
            let rectPoly = [];
            switch (side) {
                case 'top':
                    rectPoly = [
                        [left, refRect.top + 1],
                        [left, floatRect.bottom - 1],
                        [right, floatRect.bottom - 1],
                        [right, refRect.top + 1]
                    ];
                    break;
                case 'bottom':
                    rectPoly = [
                        [left, floatRect.top + 1],
                        [left, refRect.bottom - 1],
                        [right, refRect.bottom - 1],
                        [right, floatRect.top + 1]
                    ];
                    break;
                case 'left':
                    rectPoly = [
                        [floatRect.right - 1, bottom],
                        [floatRect.right - 1, top],
                        [refRect.left + 1, top],
                        [refRect.left + 1, bottom]
                    ];
                    break;
                case 'right':
                    rectPoly = [
                        [refRect.right - 1, bottom],
                        [refRect.right - 1, top],
                        [floatRect.left + 1, top],
                        [floatRect.left + 1, bottom]
                    ];
                    break;
            }

            // Build the safe polygon from cursor position to floating element
            function getPolygon([px, py]) {
                switch (side) {
                    case 'top': {
                        const cursorPointOne = [
                            isFloatingWider ? px + buffer / 2 : cursorLeaveFromRight ? px + buffer * 4 : px - buffer * 4,
                            py + buffer + 1
                        ];
                        const cursorPointTwo = [
                            isFloatingWider ? px - buffer / 2 : cursorLeaveFromRight ? px + buffer * 4 : px - buffer * 4,
                            py + buffer + 1
                        ];
                        const commonPoints = [
                            [floatRect.left, cursorLeaveFromRight ? floatRect.bottom - buffer : isFloatingWider ? floatRect.bottom - buffer : floatRect.top],
                            [floatRect.right, cursorLeaveFromRight ? (isFloatingWider ? floatRect.bottom - buffer : floatRect.top) : floatRect.bottom - buffer]
                        ];
                        return [cursorPointOne, cursorPointTwo, ...commonPoints];
                    }
                    case 'bottom': {
                        const cursorPointOne = [
                            isFloatingWider ? px + buffer / 2 : cursorLeaveFromRight ? px + buffer * 4 : px - buffer * 4,
                            py - buffer
                        ];
                        const cursorPointTwo = [
                            isFloatingWider ? px - buffer / 2 : cursorLeaveFromRight ? px + buffer * 4 : px - buffer * 4,
                            py - buffer
                        ];
                        const commonPoints = [
                            [floatRect.left, cursorLeaveFromRight ? floatRect.top + buffer : isFloatingWider ? floatRect.top + buffer : floatRect.bottom],
                            [floatRect.right, cursorLeaveFromRight ? (isFloatingWider ? floatRect.top + buffer : floatRect.bottom) : floatRect.top + buffer]
                        ];
                        return [cursorPointOne, cursorPointTwo, ...commonPoints];
                    }
                    case 'left': {
                        const cursorPointOne = [
                            px + buffer + 1,
                            isFloatingTaller ? py + buffer / 2 : cursorLeaveFromBottom ? py + buffer * 4 : py - buffer * 4
                        ];
                        const cursorPointTwo = [
                            px + buffer + 1,
                            isFloatingTaller ? py - buffer / 2 : cursorLeaveFromBottom ? py + buffer * 4 : py - buffer * 4
                        ];
                        const commonPoints = [
                            [cursorLeaveFromBottom ? floatRect.right - buffer : isFloatingTaller ? floatRect.right - buffer : floatRect.left, floatRect.top],
                            [cursorLeaveFromBottom ? (isFloatingTaller ? floatRect.right - buffer : floatRect.left) : floatRect.right - buffer, floatRect.bottom]
                        ];
                        return [...commonPoints, cursorPointOne, cursorPointTwo];
                    }
                    case 'right': {
                        const cursorPointOne = [
                            px - buffer,
                            isFloatingTaller ? py + buffer / 2 : cursorLeaveFromBottom ? py + buffer * 4 : py - buffer * 4
                        ];
                        const cursorPointTwo = [
                            px - buffer,
                            isFloatingTaller ? py - buffer / 2 : cursorLeaveFromBottom ? py + buffer * 4 : py - buffer * 4
                        ];
                        const commonPoints = [
                            [cursorLeaveFromBottom ? floatRect.left + buffer : isFloatingTaller ? floatRect.left + buffer : floatRect.right, floatRect.top],
                            [cursorLeaveFromBottom ? (isFloatingTaller ? floatRect.left + buffer : floatRect.right) : floatRect.left + buffer, floatRect.bottom]
                        ];
                        return [cursorPointOne, cursorPointTwo, ...commonPoints];
                    }
                    default:
                        return [];
                }
            }

            // Check if cursor is in the rectangular trough
            if (isPointInPolygon([clientX, clientY], rectPoly)) {
                return;
            }

            // If already landed on floating and now outside reference rect, close
            if (hasLanded && !isOverReferenceRect) {
                return close();
            }

            // Check cursor intent (speed threshold)
            if (!isLeave && requireIntent) {
                const cursorSpeed = getCursorSpeed(event.clientX, event.clientY);
                const cursorSpeedThreshold = 0.1;
                if (cursorSpeed !== null && cursorSpeed < cursorSpeedThreshold) {
                    return close();
                }
            }

            // Check if cursor is in the safe polygon
            if (!isPointInPolygon([clientX, clientY], getPolygon([x, y]))) {
                close();
            } else if (!hasLanded && requireIntent) {
                // Give a small delay before closing if intent is required
                timeout.start(40, close);
            }
        };
    }

    handleClose.__options = { blockPointerEvents };
    return handleClose;
}

// ============================================================================
// Hover Interaction Module
// ============================================================================

/**
 * Creates a hover interaction handler for floating elements.
 *
 * @param {Object} options
 * @param {string} options.interactionId - Unique ID for this interaction
 * @param {HTMLElement} options.triggerElement - The trigger element
 * @param {HTMLElement} options.floatingElement - The floating element (can be null initially)
 * @param {Function} options.onOpen - Callback when opening
 * @param {Function} options.onClose - Callback when closing
 * @param {number} options.openDelay - Delay before opening (ms)
 * @param {number} options.closeDelay - Delay before closing (ms)
 * @param {boolean} options.mouseOnly - Only respond to mouse events
 * @param {boolean} options.useSafePolygon - Whether to use safe polygon
 * @param {Object} options.safePolygonOptions - Options for safe polygon
 * @returns {Object} Interaction controller with cleanup method
 */
export function createHoverInteraction(options) {
    const {
        interactionId,
        triggerElement,
        onOpen,
        onClose,
        openDelay = 0,
        closeDelay = 0,
        mouseOnly = false,
        useSafePolygon = true,
        safePolygonOptions = {}
    } = options;

    let floatingElement = options.floatingElement;
    let pointerType = null;
    let isOpen = false;
    let openTimeout = new Timeout();
    let closeTimeout = new Timeout();
    let lastCursorX = 0;
    let lastCursorY = 0;
    let mouseMoveHandler = null;
    let safePolygonHandler = useSafePolygon ? safePolygon(safePolygonOptions) : null;
    let placement = 'bottom';

    function handleMouseEnter(event) {
        if (mouseOnly && pointerType && !isMouseLikePointerType(pointerType)) {
            return;
        }

        closeTimeout.clear();

        if (openDelay > 0) {
            openTimeout.start(openDelay, () => {
                if (!isOpen) {
                    isOpen = true;
                    onOpen?.('trigger-hover');
                }
            });
        } else if (!isOpen) {
            isOpen = true;
            onOpen?.('trigger-hover');
        }
    }

    function handleMouseLeave(event) {
        openTimeout.clear();
        lastCursorX = event.clientX;
        lastCursorY = event.clientY;

        // If moving to floating element, don't close
        if (floatingElement && event.relatedTarget && contains(floatingElement, event.relatedTarget)) {
            return;
        }

        if (safePolygonHandler && floatingElement && isOpen) {
            // Set up safe polygon tracking
            cleanupMouseMove();

            // Get the current placement from the positioner
            const positionerState = Array.from(state.positioners.values())
                .find(p => p.floatingElement === floatingElement || p.positionerElement?.contains(floatingElement));
            if (positionerState) {
                placement = positionerState.computedSide || 'bottom';
            }

            mouseMoveHandler = safePolygonHandler({
                x: lastCursorX,
                y: lastCursorY,
                placement,
                referenceElement: triggerElement,
                floatingElement,
                onClose: () => {
                    cleanupMouseMove();
                    closeWithDelay();
                }
            });

            document.addEventListener('mousemove', mouseMoveHandler);
        } else {
            closeWithDelay();
        }
    }

    function handleFloatingMouseEnter() {
        closeTimeout.clear();
        cleanupMouseMove();
    }

    function handleFloatingMouseLeave(event) {
        // If moving back to trigger, don't close
        if (event.relatedTarget && contains(triggerElement, event.relatedTarget)) {
            return;
        }
        closeWithDelay();
    }

    function handlePointerDown(event) {
        pointerType = event.pointerType;
    }

    function closeWithDelay() {
        if (closeDelay > 0) {
            closeTimeout.start(closeDelay, () => {
                if (isOpen) {
                    isOpen = false;
                    onClose?.('trigger-hover');
                }
            });
        } else if (isOpen) {
            isOpen = false;
            onClose?.('trigger-hover');
        }
    }

    function cleanupMouseMove() {
        if (mouseMoveHandler) {
            document.removeEventListener('mousemove', mouseMoveHandler);
            mouseMoveHandler = null;
        }
    }

    // Attach event listeners to trigger
    triggerElement.addEventListener('mouseenter', handleMouseEnter);
    triggerElement.addEventListener('mouseleave', handleMouseLeave);
    triggerElement.addEventListener('pointerdown', handlePointerDown);

    const interaction = {
        id: interactionId,

        setFloatingElement(element) {
            // Remove listeners from old element
            if (floatingElement) {
                floatingElement.removeEventListener('mouseenter', handleFloatingMouseEnter);
                floatingElement.removeEventListener('mouseleave', handleFloatingMouseLeave);
            }

            floatingElement = element;

            // Attach listeners to new element
            if (floatingElement) {
                floatingElement.addEventListener('mouseenter', handleFloatingMouseEnter);
                floatingElement.addEventListener('mouseleave', handleFloatingMouseLeave);
            }
        },

        setOpen(open) {
            isOpen = open;
        },

        cleanup() {
            openTimeout.clear();
            closeTimeout.clear();
            cleanupMouseMove();

            triggerElement.removeEventListener('mouseenter', handleMouseEnter);
            triggerElement.removeEventListener('mouseleave', handleMouseLeave);
            triggerElement.removeEventListener('pointerdown', handlePointerDown);

            if (floatingElement) {
                floatingElement.removeEventListener('mouseenter', handleFloatingMouseEnter);
                floatingElement.removeEventListener('mouseleave', handleFloatingMouseLeave);
            }

            state.interactions.delete(interactionId);
        }
    };

    state.interactions.set(interactionId, interaction);
    return interaction;
}

/**
 * Gets an existing hover interaction by ID.
 */
export function getHoverInteraction(interactionId) {
    return state.interactions.get(interactionId);
}

// ============================================================================
// Dismiss Interaction Module
// ============================================================================

/**
 * Creates a dismiss interaction handler for floating elements.
 *
 * @param {Object} options
 * @param {string} options.interactionId - Unique ID for this interaction
 * @param {HTMLElement} options.triggerElement - The trigger element
 * @param {HTMLElement} options.floatingElement - The floating element
 * @param {Function} options.onDismiss - Callback when dismissing
 * @param {boolean} options.escapeKey - Whether to dismiss on Escape key
 * @param {boolean} options.outsidePress - Whether to dismiss on outside press
 * @param {boolean} options.ancestorScroll - Whether to dismiss on ancestor scroll
 * @param {string} [options.treeId] - FloatingTree ID for tree-aware dismiss
 * @param {string} [options.nodeId] - Node ID within the FloatingTree
 * @param {boolean|Object} [options.bubbles] - Bubble prevention configuration
 * @param {string|Object|Function} [options.outsidePressEvent='sloppy'] - Outside press event mode
 * @returns {Object} Interaction controller with cleanup method
 */
export function createDismissInteraction(options) {
    const {
        interactionId,
        triggerElement,
        floatingElement,
        onDismiss,
        escapeKey = true,
        outsidePress = true,
        ancestorScroll = false,
        treeId = null,
        nodeId = null,
        bubbles = undefined,
        outsidePressEvent = 'sloppy'
    } = options;

    const doc = getDocument(floatingElement);
    let isComposing = false;
    let currentPointerType = '';

    // Normalize and store bubble flags
    const normalizedBubbles = normalizeBubblesProp(bubbles);
    if (treeId && nodeId) {
        setNodeDismissBubbles(treeId, nodeId, normalizedBubbles);
    }

    // Check if any open child prevents bubbling
    function shouldPreventBubble(type) {
        if (!treeId || !nodeId) return false;
        const tree = state.trees.get(treeId);
        if (!tree) return false;

        const children = tree.getNodeChildren(nodeId);
        for (const child of children) {
            if (!child.context?.open) continue;
            if (type === 'escape' && child.__escapeKeyBubbles === false) return true;
            if (type === 'outsidePress' && child.__outsidePressBubbles === false) return true;
        }
        return false;
    }

    // Resolve which event type to use for outside press
    function resolveOutsidePressEvent() {
        if (typeof outsidePressEvent === 'function') {
            return resolveEventType(outsidePressEvent());
        }
        return resolveEventType(outsidePressEvent);
    }

    function resolveEventType(mode) {
        if (typeof mode === 'object' && mode !== null) {
            const isTouch = currentPointerType === 'touch';
            return isTouch ? (mode.touch || 'pointerdown') : (mode.mouse || 'pointerdown');
        }
        if (mode === 'sloppy') {
            return currentPointerType === 'touch' ? '__touch_state_machine__' : 'pointerdown';
        }
        return mode || 'mousedown';
    }

    function handleKeyDown(event) {
        if (!escapeKey || event.key !== 'Escape') {
            return;
        }

        // Wait until IME is settled
        if (isComposing) {
            return;
        }

        if (shouldPreventBubble('escape')) {
            return;
        }

        event.preventDefault();

        // Stop propagation if escape doesn't bubble
        if (!normalizedBubbles.escapeKey) {
            event.stopPropagation();
        }

        onDismiss?.('escape-key');
    }

    function handleCompositionStart() {
        isComposing = true;
    }

    function handleCompositionEnd() {
        // Safari fires compositionend before keydown
        setTimeout(() => {
            isComposing = false;
        }, 5);
    }

    function closeOnPressOutside(event) {
        if (shouldPreventBubble('outsidePress')) {
            return;
        }

        const target = getTarget(event);

        // Check if click is inside floating or trigger
        if (isEventTargetWithin(event, floatingElement) || isEventTargetWithin(event, triggerElement)) {
            return;
        }

        // Check if target is in any registered trigger
        if (isTargetInRegisteredTriggers(interactionId, event)) {
            return;
        }

        // Check if target is a third-party element
        if (isThirdPartyElement(target, floatingElement)) {
            return;
        }

        // Check if click is on scrollbar
        if (isHTMLElement(target)) {
            const style = getComputedStyle(target);
            const scrollRe = /auto|scroll/;
            const isScrollableX = scrollRe.test(style.overflowX);
            const isScrollableY = scrollRe.test(style.overflowY);

            const canScrollX = isScrollableX && target.clientWidth > 0 && target.scrollWidth > target.clientWidth;
            const canScrollY = isScrollableY && target.clientHeight > 0 && target.scrollHeight > target.clientHeight;

            const isRTL = style.direction === 'rtl';

            const pressedVerticalScrollbar = canScrollY && (isRTL
                ? event.offsetX <= target.offsetWidth - target.clientWidth
                : event.offsetX > target.clientWidth);

            const pressedHorizontalScrollbar = canScrollX && event.offsetY > target.clientHeight;

            if (pressedVerticalScrollbar || pressedHorizontalScrollbar) {
                return;
            }
        }

        onDismiss?.('outside-press');
    }

    function handleScroll(event) {
        onDismiss?.('ancestor-scroll');
    }

    // Touch dismiss state machine
    const touchState = {
        startTime: 0,
        startX: 0,
        startY: 0,
        dismissOnTouchEnd: false,
        dismissOnMouseDown: true,
        timeout: null
    };

    function handleTouchStart(event) {
        const touch = event.touches[0];
        if (!touch) return;

        touchState.startTime = Date.now();
        touchState.startX = touch.clientX;
        touchState.startY = touch.clientY;
        touchState.dismissOnTouchEnd = false;
        touchState.dismissOnMouseDown = true;

        // 1-second timeout: long press = no dismiss
        if (touchState.timeout) clearTimeout(touchState.timeout);
        touchState.timeout = setTimeout(() => {
            touchState.dismissOnTouchEnd = false;
            touchState.dismissOnMouseDown = false;
            touchState.timeout = null;
        }, 1000);
    }

    function handleTouchMove(event) {
        const touch = event.touches[0];
        if (!touch) return;

        const dx = touch.clientX - touchState.startX;
        const dy = touch.clientY - touchState.startY;
        const distance = Math.sqrt(dx * dx + dy * dy);

        if (distance > 10) {
            // Fast scroll gesture — immediate dismiss
            if (touchState.timeout) {
                clearTimeout(touchState.timeout);
                touchState.timeout = null;
            }
            closeOnPressOutside(event);
            touchState.dismissOnTouchEnd = false;
            touchState.dismissOnMouseDown = false;
            return;
        }

        if (distance > 5) {
            // Scroll gesture — arm for touchend dismiss
            touchState.dismissOnTouchEnd = true;
        }
    }

    function handleTouchEnd(event) {
        if (touchState.timeout) {
            clearTimeout(touchState.timeout);
            touchState.timeout = null;
        }

        if (touchState.dismissOnTouchEnd) {
            closeOnPressOutside(event);
        }

        touchState.dismissOnTouchEnd = false;
    }

    // Track pointer type
    function handlePointerType(event) {
        currentPointerType = event.pointerType || '';
    }

    // Determine which event to listen for outside press
    function handlePointerDown(event) {
        // Skip touch pointer events — deferred to touch state machine
        if (event.pointerType === 'touch') return;
        closeOnPressOutside(event);
    }

    // Attach event listeners
    doc.addEventListener('keydown', handleKeyDown);
    doc.addEventListener('compositionstart', handleCompositionStart);
    doc.addEventListener('compositionend', handleCompositionEnd);
    doc.addEventListener('pointerdown', handlePointerType, true);

    let outsidePressCleanup = null;
    if (outsidePress) {
        const isDynamic = typeof outsidePressEvent === 'function' || typeof outsidePressEvent === 'object';
        const resolvedEvent = resolveOutsidePressEvent();

        if (isDynamic || resolvedEvent === '__touch_state_machine__' || outsidePressEvent === 'sloppy') {
            // Sloppy/dynamic mode: pointerdown for mouse, touch state machine for touch
            doc.addEventListener('pointerdown', handlePointerDown);
            doc.addEventListener('touchstart', handleTouchStart, { capture: true, passive: true });
            doc.addEventListener('touchmove', handleTouchMove, { capture: true, passive: true });
            doc.addEventListener('touchend', handleTouchEnd, { capture: true, passive: true });

            outsidePressCleanup = () => {
                doc.removeEventListener('pointerdown', handlePointerDown);
                doc.removeEventListener('touchstart', handleTouchStart, { capture: true });
                doc.removeEventListener('touchmove', handleTouchMove, { capture: true });
                doc.removeEventListener('touchend', handleTouchEnd, { capture: true });
            };
        } else {
            // Specific event mode (static string)
            doc.addEventListener(resolvedEvent, closeOnPressOutside);
            outsidePressCleanup = () => {
                doc.removeEventListener(resolvedEvent, closeOnPressOutside);
            };
        }
    }

    let scrollCleanup = null;
    if (ancestorScroll && triggerElement) {
        const scrollParents = getScrollParents(triggerElement);
        scrollParents.forEach(parent => {
            parent.addEventListener('scroll', handleScroll, { passive: true });
        });
        scrollCleanup = () => {
            scrollParents.forEach(parent => {
                parent.removeEventListener('scroll', handleScroll);
            });
        };
    }

    const interaction = {
        id: interactionId,

        cleanup() {
            doc.removeEventListener('keydown', handleKeyDown);
            doc.removeEventListener('compositionstart', handleCompositionStart);
            doc.removeEventListener('compositionend', handleCompositionEnd);
            doc.removeEventListener('pointerdown', handlePointerType, true);

            outsidePressCleanup?.();
            scrollCleanup?.();

            // Clean up registered triggers
            triggerMaps.delete(interactionId);

            // Clean up touch timeout
            if (touchState.timeout) clearTimeout(touchState.timeout);

            state.interactions.delete(interactionId);
        }
    };

    state.interactions.set(interactionId, interaction);
    return interaction;
}

// ============================================================================
// Tabbable Utilities (enableFocusInside / disableFocusInside)
// ============================================================================

/**
 * CSS selector matching all natively tabbable HTML elements.
 * Consolidates the duplicated selectors from dialog/popover JS modules.
 */
export const TABBABLE_SELECTOR =
    'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), ' +
    'select:not([disabled]), textarea:not([disabled]), ' +
    '[tabindex]:not([tabindex="-1"]), [contenteditable]:not([contenteditable="false"])';

/**
 * Returns all tabbable elements within a container, ordered by DOM position.
 * Excludes elements with negative tabindex unless they have data-tabindex.
 */
export function getTabbableElements(container) {
    if (!container) return [];
    const elements = Array.from(container.querySelectorAll(TABBABLE_SELECTOR));
    return elements.filter(el => {
        if (el.hasAttribute('data-tabindex')) return true;
        const tabindex = el.getAttribute('tabindex');
        if (tabindex !== null && parseInt(tabindex, 10) < 0) return false;
        if (el.disabled) return false;
        if (el.offsetParent === null && getComputedStyle(el).position !== 'fixed') return false;
        return true;
    });
}

/**
 * Disables focus on all tabbable elements inside a container by setting
 * tabindex="-1" and preserving original values in data-tabindex attribute.
 * Idempotent — calling multiple times has no additional effect.
 */
export function disableFocusInside(container) {
    if (!container) return;
    const elements = getTabbableElements(container);
    for (const el of elements) {
        if (el.hasAttribute('data-tabindex')) continue;
        const currentTabindex = el.getAttribute('tabindex');
        el.setAttribute('data-tabindex', currentTabindex ?? '');
        el.setAttribute('tabindex', '-1');
    }
}

/**
 * Re-enables focus on all elements inside a container that were previously
 * disabled by disableFocusInside. Restores original tabindex from data-tabindex.
 */
export function enableFocusInside(container) {
    if (!container) return;
    const elements = container.querySelectorAll('[data-tabindex]');
    for (const el of elements) {
        const originalTabindex = el.getAttribute('data-tabindex');
        el.removeAttribute('data-tabindex');
        if (originalTabindex === '') {
            el.removeAttribute('tabindex');
        } else {
            el.setAttribute('tabindex', originalTabindex);
        }
    }
}

/**
 * Given the currently focused element, returns the next tabbable element
 * within the floating element scope. Wraps around if at the end.
 */
export function getNextTabbable(currentElement, container) {
    const tabbable = getTabbableElements(container);
    if (tabbable.length === 0) return null;
    const index = tabbable.indexOf(currentElement);
    if (index === -1 || index === tabbable.length - 1) return tabbable[0];
    return tabbable[index + 1];
}

/**
 * Given the currently focused element, returns the previous tabbable element
 * within the floating element scope. Wraps around if at the start.
 */
export function getPreviousTabbable(currentElement, container) {
    const tabbable = getTabbableElements(container);
    if (tabbable.length === 0) return null;
    const index = tabbable.indexOf(currentElement);
    if (index <= 0) return tabbable[tabbable.length - 1];
    return tabbable[index - 1];
}

// ============================================================================
// Focus Guard Elements
// ============================================================================

const focusGuardPairs = new Map();
let focusGuardCounter = 0;

/**
 * Initializes a pair of focus guard elements around a floating element.
 * Creates before/after guard references and attaches focus redirect handlers.
 */
export function initializeFocusGuards(options) {
    const {
        floatingElement,
        triggerElement = null,
        modal = true,
        onClose = null
    } = options;

    const guardPairId = `guard-${++focusGuardCounter}`;
    const guards = floatingElement.parentElement?.querySelectorAll('[data-base-ui-focus-guard]');
    if (!guards || guards.length < 2) {
        // Guards not rendered yet; store config for deferred init
        focusGuardPairs.set(guardPairId, {
            floatingElement, triggerElement, modal, onClose,
            beforeGuard: null, afterGuard: null, cleanup: null
        });
        return guardPairId;
    }

    const beforeGuard = guards[0];
    const afterGuard = guards[guards.length - 1];

    function handleBeforeGuardFocus() {
        const tabbable = getTabbableElements(floatingElement);
        if (tabbable.length > 0) {
            tabbable[tabbable.length - 1].focus();
        } else {
            floatingElement.focus();
        }
    }

    function handleAfterGuardFocus() {
        if (modal) {
            const tabbable = getTabbableElements(floatingElement);
            if (tabbable.length > 0) {
                tabbable[0].focus();
            } else {
                floatingElement.focus();
            }
        } else {
            onClose?.();
        }
    }

    beforeGuard.addEventListener('focus', handleBeforeGuardFocus);
    afterGuard.addEventListener('focus', handleAfterGuardFocus);

    const cleanup = () => {
        beforeGuard.removeEventListener('focus', handleBeforeGuardFocus);
        afterGuard.removeEventListener('focus', handleAfterGuardFocus);
    };

    focusGuardPairs.set(guardPairId, {
        floatingElement, triggerElement, modal, onClose,
        beforeGuard, afterGuard, cleanup
    });

    return guardPairId;
}

/**
 * Removes focus guard event listeners and cleans up internal state.
 * Does NOT remove DOM elements (Blazor manages FocusGuard.razor lifecycle).
 */
export function disposeFocusGuards(guardPairId) {
    const pair = focusGuardPairs.get(guardPairId);
    if (!pair) return;
    pair.cleanup?.();
    focusGuardPairs.delete(guardPairId);
}

/**
 * Updates the focus guard mode after initialization.
 */
export function updateFocusGuardMode(guardPairId, modal, onClose) {
    const pair = focusGuardPairs.get(guardPairId);
    if (!pair) return;
    pair.modal = modal;
    pair.onClose = onClose;
    // Re-initialize handlers with updated mode
    pair.cleanup?.();
    if (pair.beforeGuard && pair.afterGuard) {
        const result = initializeFocusGuards({
            floatingElement: pair.floatingElement,
            triggerElement: pair.triggerElement,
            modal,
            onClose
        });
        const newPair = focusGuardPairs.get(result);
        if (newPair) {
            pair.cleanup = newPair.cleanup;
            pair.beforeGuard = newPair.beforeGuard;
            pair.afterGuard = newPair.afterGuard;
            focusGuardPairs.delete(result);
        }
    }
}

/**
 * Checks whether focus is currently inside the guarded scope
 * (between the before and after guard elements, inclusive).
 */
export function isFocusInsideGuardedScope(guardPairId) {
    const pair = focusGuardPairs.get(guardPairId);
    if (!pair || !pair.floatingElement) return false;
    const doc = getDocument(pair.floatingElement);
    const active = activeElement(doc);
    if (!active) return false;
    return contains(pair.floatingElement, active) ||
        active === pair.beforeGuard ||
        active === pair.afterGuard;
}

// ============================================================================
// markOthers (inert/aria-hidden management)
// ============================================================================

const counterMaps = {
    'aria-hidden': new WeakMap(),
    'inert': new WeakMap(),
    'none': new WeakMap()
};
const markerMap = new WeakMap();
const uncontrolledElementsSet = new WeakSet();
let lockCount = 0;

function getCounterMap(control) {
    if (control === 'aria-hidden') return counterMaps['aria-hidden'];
    if (control === 'inert') return counterMaps['inert'];
    return counterMaps['none'];
}

function unwrapHost(node) {
    let current = node;
    while (current?.getRootNode() instanceof ShadowRoot) {
        current = current.getRootNode().host;
    }
    return current;
}

function applyAttributeToOthers(avoidElements, body, ariaHidden, inert) {
    const controlAttribute = inert ? 'inert' : ariaHidden ? 'aria-hidden' : null;
    const counterMap = getCounterMap(controlAttribute ?? 'none');
    const markedElements = [];

    // Build set of ancestor elements to skip
    const avoidAncestors = new Set();
    for (const el of avoidElements) {
        let current = unwrapHost(el);
        while (current && current !== body) {
            avoidAncestors.add(current);
            current = current.parentElement;
        }
    }

    // Walk body children recursively
    function walkAndMark(parent) {
        for (const child of parent.children) {
            // Skip the avoid elements and their ancestors
            if (avoidAncestors.has(child) || avoidElements.includes(child)) {
                // If this is an ancestor of an avoid element, walk its children
                // but do NOT recurse into avoid elements themselves (their children should stay interactive)
                if (avoidAncestors.has(child) && !avoidElements.includes(child)) {
                    walkAndMark(child);
                }
                continue;
            }

            // Skip script elements and aria-live regions
            if (child.tagName === 'SCRIPT' || child.hasAttribute('aria-live')) {
                continue;
            }

            // Check if element already has the attribute (uncontrolled)
            if (controlAttribute && child.hasAttribute(controlAttribute) && !markerMap.has(child)) {
                uncontrolledElementsSet.add(child);
                continue;
            }

            // Apply attribute
            const currentCount = counterMap.get(child) || 0;
            if (currentCount === 0) {
                if (controlAttribute) {
                    child.setAttribute(controlAttribute, controlAttribute === 'aria-hidden' ? 'true' : '');
                }
                child.setAttribute('data-base-ui-inert', '');
            }
            counterMap.set(child, currentCount + 1);

            const markerCount = markerMap.get(child) || 0;
            markerMap.set(child, markerCount + 1);

            markedElements.push(child);
        }
    }

    walkAndMark(body);
    lockCount++;

    return function cleanup() {
        for (const child of markedElements) {
            const currentCount = counterMap.get(child) || 0;
            const newCount = currentCount - 1;

            if (newCount <= 0) {
                counterMap.delete(child);
                if (controlAttribute && !uncontrolledElementsSet.has(child)) {
                    child.removeAttribute(controlAttribute);
                }
            } else {
                counterMap.set(child, newCount);
            }

            const markerCount = markerMap.get(child) || 0;
            const newMarkerCount = markerCount - 1;
            if (newMarkerCount <= 0) {
                markerMap.delete(child);
                child.removeAttribute('data-base-ui-inert');
            } else {
                markerMap.set(child, newMarkerCount);
            }
        }

        lockCount--;
        if (lockCount === 0) {
            // Reset all maps when no more active callers
            counterMaps['aria-hidden'] = new WeakMap();
            counterMaps['inert'] = new WeakMap();
            counterMaps['none'] = new WeakMap();
        }
    };
}

/**
 * Marks all DOM elements outside the specified avoid elements as aria-hidden
 * or inert, creating a modal accessibility barrier.
 */
export function markOthers(avoidElements, ariaHidden = false, inert = false) {
    const body = document.body;
    if (!body) return () => {};
    return applyAttributeToOthers(avoidElements, body, ariaHidden, inert);
}

/**
 * Checks whether the browser supports the native `inert` attribute.
 */
export function supportsInert() {
    return 'inert' in HTMLElement.prototype;
}

// ============================================================================
// FloatingFocusManager
// ============================================================================

const focusManagers = new Map();
let focusManagerCounter = 0;

/**
 * Creates a generalized floating focus manager that handles modal/non-modal
 * focus trapping, initial focus, return focus, focus restoration, and
 * aria-modal management.
 */
export function createFloatingFocusManager(options) {
    const {
        floatingElement,
        triggerElement = null,
        modal = true,
        initialFocus = true,
        initialFocusSelector = null,
        returnFocus = true,
        restoreFocus = false,
        restoreFocusMode = null,
        closeOnFocusOut = true,
        interactionType = '',
        onClose = null,
        treeId = null,
        nodeId = null,
        insideElements = []
    } = options;

    const managerId = `fm-${++focusManagerCounter}`;
    const doc = getDocument(floatingElement);
    const previouslyFocusedElement = activeElement(doc);

    let markOthersCleanup = null;
    let guardPairId = null;
    let focusOutCleanup = null;
    let mutationObserver = null;

    // Apply aria-modal in modal mode
    if (modal) {
        floatingElement.setAttribute('aria-modal', 'true');
    }

    // Mark other elements as inert/aria-hidden in modal mode
    if (modal) {
        const avoidElements = [floatingElement, ...insideElements];
        if (triggerElement) avoidElements.push(triggerElement);
        markOthersCleanup = markOthers(avoidElements, true, supportsInert());
    }

    // Initialize focus guards
    guardPairId = initializeFocusGuards({
        floatingElement,
        triggerElement,
        modal,
        onClose
    });

    // Handle initial focus
    function setInitialFocus() {
        if (initialFocus === false) return;

        // Touch interaction suppression — don't focus first tabbable on touch
        // to prevent virtual keyboard from appearing
        const isTouchInteraction = interactionType === 'touch';

        if (typeof initialFocusSelector === 'string' && initialFocusSelector) {
            const target = floatingElement.querySelector(initialFocusSelector);
            if (target) {
                target.focus();
                return;
            }
        }

        if (initialFocus instanceof HTMLElement) {
            initialFocus.focus();
            return;
        }

        if (isTouchInteraction) {
            // Focus the container itself to maintain focus context
            // without triggering virtual keyboard
            floatingElement.focus();
            return;
        }

        // Default: focus first tabbable element
        const tabbable = getTabbableElements(floatingElement);
        if (tabbable.length > 0) {
            tabbable[0].focus();
        } else {
            floatingElement.focus();
        }
    }

    // Delay initial focus to next frame to ensure DOM is ready
    requestAnimationFrame(() => {
        setInitialFocus();
    });

    // Setup focus restoration when focused element is removed from DOM
    if (restoreFocus) {
        mutationObserver = new MutationObserver(() => {
            const active = activeElement(doc);
            if (!active || active === doc.body) {
                if (restoreFocusMode === 'popup') {
                    floatingElement.focus();
                } else {
                    const tabbable = getTabbableElements(floatingElement);
                    if (tabbable.length > 0) {
                        tabbable[0].focus();
                    } else {
                        floatingElement.focus();
                    }
                }
            }
        });
        mutationObserver.observe(floatingElement, { childList: true, subtree: true });
    }

    // Setup closeOnFocusOut for non-modal
    if (!modal && closeOnFocusOut) {
        function handleFocusOut(event) {
            const relatedTarget = event.relatedTarget;
            if (!relatedTarget) return;
            if (contains(floatingElement, relatedTarget)) return;
            if (triggerElement && contains(triggerElement, relatedTarget)) return;
            for (const el of insideElements) {
                if (contains(el, relatedTarget)) return;
            }
            onClose?.();
        }
        floatingElement.addEventListener('focusout', handleFocusOut);
        focusOutCleanup = () => floatingElement.removeEventListener('focusout', handleFocusOut);
    }

    const manager = {
        id: managerId,
        floatingElement,
        triggerElement,
        modal,
        returnFocus,
        previouslyFocusedElement,
        guardPairId,
        insideElements,
        onClose,
        markOthersCleanup,
        focusOutCleanup,

        dispose(shouldReturnFocus = true) {
            // Remove aria-modal
            floatingElement.removeAttribute('aria-modal');

            // Clean up markOthers
            this.markOthersCleanup?.();

            // Clean up focus guards
            if (guardPairId) disposeFocusGuards(guardPairId);

            // Clean up focus out listener
            this.focusOutCleanup?.();

            // Clean up mutation observer
            mutationObserver?.disconnect();

            // Return focus
            if (shouldReturnFocus && this.returnFocus !== false) {
                requestAnimationFrame(() => {
                    if (this.returnFocus instanceof HTMLElement) {
                        this.returnFocus.focus();
                    } else if (this.previouslyFocusedElement) {
                        this.previouslyFocusedElement.focus();
                    } else if (triggerElement) {
                        triggerElement.focus();
                    }
                });
            }

            focusManagers.delete(managerId);
        }
    };

    focusManagers.set(managerId, manager);
    return managerId;
}

/**
 * Disposes a floating focus manager.
 */
export function disposeFloatingFocusManager(managerId, shouldReturnFocus = true) {
    const manager = focusManagers.get(managerId);
    if (!manager) return;
    manager.dispose(shouldReturnFocus);
}

/**
 * Updates focus manager options after creation.
 */
export function updateFloatingFocusManager(managerId, options) {
    const manager = focusManagers.get(managerId);
    if (!manager) return;
    if (options.returnFocus !== undefined) manager.returnFocus = options.returnFocus;

    const modalChanged = options.modal !== undefined && options.modal !== manager.modal;
    const insideElementsChanged = options.insideElements !== undefined;

    if (modalChanged) {
        manager.modal = options.modal;
        if (options.modal) {
            manager.floatingElement.setAttribute('aria-modal', 'true');
        } else {
            manager.floatingElement.removeAttribute('aria-modal');
        }
    }

    if (insideElementsChanged) {
        manager.insideElements = options.insideElements;
    }

    // Re-apply markOthers when modal or insideElements change
    if (modalChanged || insideElementsChanged) {
        manager.markOthersCleanup?.();
        manager.markOthersCleanup = null;

        if (manager.modal) {
            const avoidElements = [manager.floatingElement, ...manager.insideElements];
            if (manager.triggerElement) avoidElements.push(manager.triggerElement);
            manager.markOthersCleanup = markOthers(avoidElements, true, supportsInert());
        }
    }

    // Toggle focusout listener when modal changes
    if (modalChanged) {
        manager.focusOutCleanup?.();
        manager.focusOutCleanup = null;

        if (!manager.modal && manager.onClose) {
            function handleFocusOut(event) {
                const relatedTarget = event.relatedTarget;
                if (!relatedTarget) return;
                if (contains(manager.floatingElement, relatedTarget)) return;
                if (manager.triggerElement && contains(manager.triggerElement, relatedTarget)) return;
                for (const el of manager.insideElements) {
                    if (contains(el, relatedTarget)) return;
                }
                manager.onClose?.();
            }
            manager.floatingElement.addEventListener('focusout', handleFocusOut);
            manager.focusOutCleanup = () => manager.floatingElement.removeEventListener('focusout', handleFocusOut);
        }
    }
}

// ============================================================================
// Tree-Aware Bubble Prevention
// ============================================================================

/**
 * Normalizes the bubbles prop into separate escape and outsidePress booleans.
 */
function normalizeBubblesProp(bubbles) {
    if (bubbles === undefined || bubbles === true) {
        return { escapeKey: true, outsidePress: true };
    }
    if (bubbles === false) {
        return { escapeKey: false, outsidePress: false };
    }
    return {
        escapeKey: bubbles.escapeKey !== undefined ? bubbles.escapeKey : true,
        outsidePress: bubbles.outsidePress !== undefined ? bubbles.outsidePress : true
    };
}

/**
 * Sets the bubble prevention flags for a node in the floating tree.
 */
export function setNodeDismissBubbles(treeId, nodeId, options) {
    const tree = state.trees.get(treeId);
    if (!tree) return;

    const node = tree.nodes.find(n => n.id === nodeId);
    if (!node) return;

    const normalized = normalizeBubblesProp(options);
    node.__escapeKeyBubbles = normalized.escapeKey;
    node.__outsidePressBubbles = normalized.outsidePress;
}

// ============================================================================
// List Navigation Utilities
// ============================================================================

/**
 * CSS selector matching navigable list items.
 */
export const NAVIGABLE_ITEM_SELECTOR =
    '[role="option"], [role="menuitem"], [role="menuitemcheckbox"], ' +
    '[role="menuitemradio"], [data-base-ui-list-item]';

/**
 * Creates a shared item registry for tracking list items.
 */
export function createItemRegistry() {
    const items = new Map();
    let orderedItems = [];
    let needsSort = false;

    function sortItems() {
        if (!needsSort) return;
        orderedItems = Array.from(items.values()).sort((a, b) => {
            if (!a.element || !b.element) return 0;
            const position = a.element.compareDocumentPosition(b.element);
            if (position & Node.DOCUMENT_POSITION_FOLLOWING) return -1;
            if (position & Node.DOCUMENT_POSITION_PRECEDING) return 1;
            return 0;
        });
        orderedItems.forEach((item, i) => item.index = i);
        needsSort = false;
    }

    return {
        registerItem(id, element, label, disabled) {
            items.set(id, { id, element, label, disabled, index: items.size });
            needsSort = true;
        },

        unregisterItem(id) {
            items.delete(id);
            needsSort = true;
        },

        getItems() {
            sortItems();
            return [...orderedItems];
        },

        getItemByIndex(index) {
            sortItems();
            return orderedItems[index] ?? null;
        },

        getItemCount() {
            return items.size;
        },

        getDisabledIndices() {
            sortItems();
            return orderedItems
                .filter(item => item.disabled)
                .map(item => item.index);
        },

        updateItemDisabled(id, disabled) {
            const item = items.get(id);
            if (item) item.disabled = disabled;
        },

        dispose() {
            items.clear();
            orderedItems = [];
        }
    };
}

/**
 * Creates a grid cell map for multi-column navigation.
 */
export function createGridCellMap(itemCount, cols) {
    return {
        getPosition(index) {
            const row = Math.floor(index / cols);
            const col = index % cols;
            return [row, col];
        },
        getIndex(row, col) {
            return row * cols + col;
        },
        getRowCount() {
            return Math.ceil(itemCount / cols);
        }
    };
}

/**
 * Calculates the target index for a grid navigation action.
 */
export function getGridNavigatedIndex(options) {
    const {
        currentIndex,
        itemCount,
        cols,
        direction,
        loop = false,
        disabledIndices = [],
        rtl = false
    } = options;

    const grid = createGridCellMap(itemCount, cols);
    const [row, col] = grid.getPosition(currentIndex);
    const rows = grid.getRowCount();

    let targetRow = row;
    let targetCol = col;

    const effectiveDirection = rtl
        ? (direction === 'left' ? 'right' : direction === 'right' ? 'left' : direction)
        : direction;

    switch (effectiveDirection) {
        case 'up':
            targetRow = row - 1;
            if (targetRow < 0) targetRow = loop ? rows - 1 : -1;
            break;
        case 'down':
            targetRow = row + 1;
            if (targetRow >= rows) targetRow = loop ? 0 : -1;
            break;
        case 'left':
            targetCol = col - 1;
            if (targetCol < 0) {
                if (loop) {
                    targetCol = cols - 1;
                    targetRow = row - 1;
                    if (targetRow < 0) targetRow = rows - 1;
                } else {
                    return -1;
                }
            }
            break;
        case 'right':
            targetCol = col + 1;
            if (targetCol >= cols) {
                if (loop) {
                    targetCol = 0;
                    targetRow = row + 1;
                    if (targetRow >= rows) targetRow = 0;
                } else {
                    return -1;
                }
            }
            break;
    }

    if (targetRow < 0) return -1;

    let targetIndex = grid.getIndex(targetRow, targetCol);
    if (targetIndex >= itemCount) {
        if (loop) targetIndex = targetIndex % itemCount;
        else return -1;
    }

    // Skip disabled indices
    if (disabledIndices.includes(targetIndex)) {
        const nextDir = (effectiveDirection === 'up' || effectiveDirection === 'left') ? -1 : 1;
        return findNextEnabledIndex(targetIndex, itemCount, disabledIndices, nextDir, loop);
    }

    return targetIndex;
}

/**
 * Finds the next enabled index in a list, skipping disabled indices.
 */
export function findNextEnabledIndex(currentIndex, itemCount, disabledIndices, direction, loop) {
    let index = currentIndex + direction;
    let steps = 0;

    while (steps < itemCount) {
        if (index < 0) {
            if (loop) index = itemCount - 1;
            else return -1;
        }
        if (index >= itemCount) {
            if (loop) index = 0;
            else return -1;
        }
        if (!disabledIndices.includes(index)) {
            return index;
        }
        index += direction;
        steps++;
    }

    return -1;
}

/**
 * Applies active/highlight state to a list item element and removes from previous.
 */
export function applyActiveState(listElement, itemElement, virtual) {
    // Remove previous active state
    const prev = listElement.querySelector('[data-base-ui-active]');
    if (prev) {
        prev.removeAttribute('data-base-ui-active');
        if (!virtual) prev.setAttribute('tabindex', '-1');
    }

    if (itemElement) {
        itemElement.setAttribute('data-base-ui-active', '');
        if (virtual) {
            listElement.setAttribute('aria-activedescendant', itemElement.id || '');
        } else {
            itemElement.setAttribute('tabindex', '0');
            itemElement.focus();
        }
    } else if (virtual) {
        listElement.removeAttribute('aria-activedescendant');
    }
}

/**
 * Creates a list navigation controller for keyboard-driven item navigation.
 */
export function createListNavigation(options) {
    const {
        listElement,
        registry,
        orientation = 'vertical',
        cols = 1,
        loop = false,
        allowEscape = false,
        virtual = false,
        focusItemOnOpen = true,
        focusItemOnHover = true,
        scrollIntoView = true,
        nested = false,
        rtl = false,
        onNavigate = null,
        onActivate = null,
        treeId = null,
        nodeId = null
    } = options;

    const navId = `nav-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    let activeIndex = -1;
    const isGrid = cols > 1;

    function getArrowDirection(key) {
        const isVertical = orientation === 'vertical' || orientation === 'both';
        const isHorizontal = orientation === 'horizontal' || orientation === 'both';

        const effectiveKey = rtl
            ? (key === 'ArrowLeft' ? 'ArrowRight' : key === 'ArrowRight' ? 'ArrowLeft' : key)
            : key;

        if (isGrid) {
            switch (effectiveKey) {
                case 'ArrowUp': return 'up';
                case 'ArrowDown': return 'down';
                case 'ArrowLeft': return 'left';
                case 'ArrowRight': return 'right';
            }
            return null;
        }

        if (isVertical && effectiveKey === 'ArrowDown') return 'down';
        if (isVertical && effectiveKey === 'ArrowUp') return 'up';
        if (isHorizontal && effectiveKey === 'ArrowRight') return 'right';
        if (isHorizontal && effectiveKey === 'ArrowLeft') return 'left';

        // Nested submenu: arrow key opens/closes
        if (nested) {
            if (effectiveKey === 'ArrowRight') return 'open';
            if (effectiveKey === 'ArrowLeft') return 'close';
        }

        return null;
    }

    function navigateTo(index) {
        if (index < 0 || index >= registry.getItemCount()) return;
        activeIndex = index;
        const item = registry.getItemByIndex(index);
        if (!item) return;
        applyActiveState(listElement, item.element, virtual);
        if (scrollIntoView && item.element) {
            item.element.scrollIntoView?.({ block: 'nearest' });
        }
        onNavigate?.(index);
    }

    function handleKeyDown(event) {
        const itemCount = registry.getItemCount();
        if (itemCount === 0) return;

        const disabledIndices = registry.getDisabledIndices();

        if (event.key === 'Home') {
            event.preventDefault();
            const first = findNextEnabledIndex(-1, itemCount, disabledIndices, 1, false);
            if (first !== -1) navigateTo(first);
            return;
        }

        if (event.key === 'End') {
            event.preventDefault();
            const last = findNextEnabledIndex(itemCount, itemCount, disabledIndices, -1, false);
            if (last !== -1) navigateTo(last);
            return;
        }

        if (event.key === 'Enter' || event.key === ' ') {
            if (activeIndex >= 0) {
                event.preventDefault();
                onActivate?.(activeIndex);
            }
            return;
        }

        const direction = getArrowDirection(event.key);
        if (!direction) return;

        if (direction === 'open' || direction === 'close') {
            // Let parent handle submenu open/close
            return;
        }

        event.preventDefault();

        if (isGrid) {
            const target = getGridNavigatedIndex({
                currentIndex: activeIndex < 0 ? 0 : activeIndex,
                itemCount,
                cols,
                direction,
                loop,
                disabledIndices,
                rtl
            });
            if (target !== -1) navigateTo(target);
        } else {
            const dir = (direction === 'down' || direction === 'right') ? 1 : -1;
            if (activeIndex < 0) {
                const first = dir === 1
                    ? findNextEnabledIndex(-1, itemCount, disabledIndices, 1, loop)
                    : findNextEnabledIndex(itemCount, itemCount, disabledIndices, -1, loop);
                if (first !== -1) navigateTo(first);
            } else {
                const next = findNextEnabledIndex(activeIndex, itemCount, disabledIndices, dir, loop);
                if (next !== -1) {
                    navigateTo(next);
                } else if (allowEscape) {
                    activeIndex = -1;
                    applyActiveState(listElement, null, virtual);
                }
            }
        }
    }

    function handlePointerMove(event, index) {
        if (!focusItemOnHover) return;
        const item = registry.getItemByIndex(index);
        if (item && !item.disabled) {
            navigateTo(index);
        }
    }

    function setActiveIndex(index) {
        navigateTo(index);
    }

    function getActiveIndex() {
        return activeIndex;
    }

    // Handle focus-on-open
    if (focusItemOnOpen === true) {
        requestAnimationFrame(() => {
            const disabledIndices = registry.getDisabledIndices();
            const first = findNextEnabledIndex(-1, registry.getItemCount(), disabledIndices, 1, false);
            if (first !== -1) navigateTo(first);
        });
    } else if (focusItemOnOpen === 'selected') {
        // Caller must provide selected index via setActiveIndex
    }

    return {
        navId,
        handleKeyDown,
        handlePointerMove,
        setActiveIndex,
        getActiveIndex,
        dispose() {
            activeIndex = -1;
            applyActiveState(listElement, null, virtual);
        }
    };
}

// ============================================================================
// Typeahead
// ============================================================================

/**
 * Creates a typeahead controller for type-ahead matching in lists.
 */
export function createTypeahead(options) {
    const {
        registry,
        resetMs = 750,
        ignoreKeys = [],
        findMatch = null,
        onMatch = null,
        onTypingChange = null
    } = options;

    const typeaheadId = `ta-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    let stringRef = '';
    let prevIndex = -1;
    let resetTimeout = null;
    let isTyping = false;

    function setTyping(value) {
        if (isTyping !== value) {
            isTyping = value;
            onTypingChange?.(value);
        }
    }

    function handleKeyDown(event) {
        // Ignore modifier keys
        if (event.ctrlKey || event.metaKey || event.altKey) return;

        // Only process single characters
        if (event.key.length !== 1) return;

        // Check ignore list
        if (ignoreKeys.includes(event.key)) return;

        // Handle space: prevent closing popup if mid-search
        if (event.key === ' ' && stringRef.length > 0 && !stringRef.startsWith(' ')) {
            event.preventDefault();
        }

        const items = registry.getItems();
        if (items.length === 0) return;

        // Clear existing timeout
        if (resetTimeout) clearTimeout(resetTimeout);

        // Same-character cycling
        if (stringRef === event.key) {
            // Reset buffer and advance from current match
            stringRef = '';
            const cycleResult = findMatchingItem(items, event.key, prevIndex + 1);
            if (cycleResult !== -1) {
                prevIndex = cycleResult;
                onMatch?.(cycleResult);
                startResetTimer();
                return;
            }
        }

        stringRef += event.key;
        setTyping(true);

        // Custom match function
        if (findMatch) {
            const labels = items.map(item => item.label || '');
            const matchLabel = findMatch(labels, stringRef);
            if (matchLabel) {
                const matchIndex = items.findIndex(item => (item.label || '') === matchLabel);
                if (matchIndex !== -1 && !items[matchIndex].disabled) {
                    prevIndex = matchIndex;
                    onMatch?.(matchIndex);
                }
            }
        } else {
            // Default: search from prevIndex + 1, wrapping around
            const result = findMatchingItem(items, stringRef, prevIndex + 1);
            if (result !== -1) {
                prevIndex = result;
                onMatch?.(result);
            }
        }

        startResetTimer();
    }

    function findMatchingItem(items, search, startFrom) {
        const lowerSearch = search.toLocaleLowerCase();
        const count = items.length;

        // Search from startFrom to end
        for (let i = 0; i < count; i++) {
            const index = (startFrom + i) % count;
            const item = items[index];
            if (item.disabled) continue;
            const label = (item.label || '').toLocaleLowerCase();
            if (label.startsWith(lowerSearch)) return index;
        }

        return -1;
    }

    function startResetTimer() {
        resetTimeout = setTimeout(() => {
            stringRef = '';
            setTyping(false);
            resetTimeout = null;
        }, resetMs);
    }

    function reset() {
        if (resetTimeout) clearTimeout(resetTimeout);
        stringRef = '';
        setTyping(false);
    }

    return {
        typeaheadId,
        handleKeyDown,
        reset,
        isTyping: () => isTyping,
        dispose() {
            if (resetTimeout) clearTimeout(resetTimeout);
            stringRef = '';
            isTyping = false;
        }
    };
}

// ============================================================================
// Multi-Trigger Element Map
// ============================================================================

const triggerMaps = new Map();

/**
 * Registers a trigger element with a dismiss interaction.
 */
export function registerTriggerElement(interactionId, triggerId, element) {
    if (!triggerMaps.has(interactionId)) {
        triggerMaps.set(interactionId, new Map());
    }
    triggerMaps.get(interactionId).set(triggerId, element);
}

/**
 * Unregisters a trigger element from a dismiss interaction.
 */
export function unregisterTriggerElement(interactionId, triggerId) {
    const map = triggerMaps.get(interactionId);
    if (!map) return;
    map.delete(triggerId);
    if (map.size === 0) triggerMaps.delete(interactionId);
}

/**
 * Checks if an event target is within any registered trigger for a dismiss interaction.
 */
function isTargetInRegisteredTriggers(interactionId, event) {
    const map = triggerMaps.get(interactionId);
    if (!map) return false;
    for (const element of map.values()) {
        if (isEventTargetWithin(event, element)) return true;
    }
    return false;
}

// ============================================================================
// Virtual Element / clientPoint
// ============================================================================

const virtualElements = new Map();
let virtualCounter = 0;

/**
 * Creates a virtual element at specified coordinates that conforms to
 * FloatingUI's VirtualElement interface.
 */
export function createVirtualElement(x, y) {
    const virtualId = `virtual-${++virtualCounter}`;

    const virtualElement = {
        virtualId,
        _x: x,
        _y: y,
        getBoundingClientRect() {
            return {
                x: this._x,
                y: this._y,
                top: this._y,
                left: this._x,
                bottom: this._y,
                right: this._x,
                width: 0,
                height: 0
            };
        },
        update(newX, newY) {
            this._x = newX;
            this._y = newY;
        }
    };

    virtualElements.set(virtualId, virtualElement);
    return virtualElement;
}

/**
 * Updates the coordinates of an existing virtual element.
 */
export function updateVirtualElement(virtualId, x, y) {
    const ve = virtualElements.get(virtualId);
    if (!ve) return;
    ve._x = x;
    ve._y = y;
}

/**
 * Disposes a virtual element and cleans up internal state.
 */
export function disposeVirtualElement(virtualId) {
    virtualElements.delete(virtualId);
}

const clientPointInteractions = new Map();

/**
 * Creates an interaction that tracks pointer position and updates
 * a virtual element's coordinates accordingly.
 */
export function createClientPointInteraction(options) {
    const {
        enabled = true,
        axis = 'both',
        floatingElement,
        virtualId,
        onUpdate = null
    } = options;

    const interactionId = `cp-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    const doc = getDocument(floatingElement);

    function handlePointerMove(event) {
        if (!enabled) return;
        const ve = virtualElements.get(virtualId);
        if (!ve) return;

        const newX = (axis === 'both' || axis === 'x') ? event.clientX : ve._x;
        const newY = (axis === 'both' || axis === 'y') ? event.clientY : ve._y;

        ve._x = newX;
        ve._y = newY;
        onUpdate?.(newX, newY);
    }

    doc.addEventListener('pointermove', handlePointerMove);

    const interaction = {
        interactionId,
        dispose() {
            doc.removeEventListener('pointermove', handlePointerMove);
            clientPointInteractions.delete(interactionId);
        }
    };

    clientPointInteractions.set(interactionId, interaction);
    return interaction;
}

// ============================================================================
// getOverflowAncestors Standalone
// ============================================================================

/**
 * Alias for getScrollParents that matches FloatingUI's naming convention.
 */
export function getOverflowAncestors(element) {
    return getScrollParents(element);
}

// ============================================================================
// Third-Party Element Detection
// ============================================================================

/**
 * Checks whether an event target is a third-party element injected into
 * the DOM after the floating element rendered.
 */
export function isThirdPartyElement(target, floatingElement) {
    if (!target || !floatingElement) return false;

    // Walk up to find the root ancestor
    let root = target;
    while (root.parentElement) {
        root = root.parentElement;
    }

    // If the root is not the document element, it's detached — treat as third-party
    if (root !== document.documentElement && root !== document) return true;

    // Check if the target's root contains any data-base-ui-inert markers
    const inertMarkers = document.querySelectorAll('[data-base-ui-inert]');
    if (inertMarkers.length === 0) return false;

    // If target is a direct ancestor of the floating element (e.g., overlay), not third-party
    if (contains(target, floatingElement)) return false;

    // Check if the target itself or its ancestors have inert markers
    // If they don't, and inert markers exist, this element was injected after markOthers ran
    let current = target;
    while (current && current !== document.body) {
        if (current.hasAttribute('data-base-ui-inert')) return false;
        current = current.parentElement;
    }

    // The element's ancestors have no inert markers — it was injected after modal opened
    return true;
}

// ============================================================================
// FloatingDelayGroup
// ============================================================================

const delayGroups = new Map();
let delayGroupCounter = 0;

/**
 * Creates a delay group for coordinating open/close delays across
 * multiple floating elements (typically tooltips).
 */
export function createDelayGroup(options) {
    const { delay, timeoutMs = 0 } = options;

    const groupId = `dg-${++delayGroupCounter}`;
    const openDelay = typeof delay === 'number' ? delay : (delay?.open ?? 0);
    const closeDelay = typeof delay === 'number' ? delay : (delay?.close ?? 0);

    const group = {
        groupId,
        openDelay,
        closeDelay,
        timeoutMs,
        members: new Map(),
        isInstantPhase: false,
        currentOpenId: null,
        timeoutHandle: null,

        getDelay() {
            if (this.isInstantPhase) {
                return { open: 0, close: this.closeDelay };
            }
            return { open: this.openDelay, close: this.closeDelay };
        },

        dispose() {
            if (this.timeoutHandle) clearTimeout(this.timeoutHandle);
            this.members.clear();
            delayGroups.delete(groupId);
        }
    };

    delayGroups.set(groupId, group);
    return group;
}

/**
 * Registers a floating element as a member of a delay group.
 */
export function registerDelayGroupMember(groupId, interactionId, callbacks) {
    const group = delayGroups.get(groupId);
    if (!group) return;

    group.members.set(interactionId, {
        ...callbacks,
        interactionId
    });
}

/**
 * Unregisters a floating element from a delay group.
 */
export function unregisterDelayGroupMember(groupId, interactionId) {
    const group = delayGroups.get(groupId);
    if (!group) return;

    group.members.delete(interactionId);

    // If no members open, start timeout to exit instant phase
    if (group.isInstantPhase && group.currentOpenId === interactionId) {
        group.currentOpenId = null;
        if (group.timeoutMs > 0) {
            group.timeoutHandle = setTimeout(() => {
                group.isInstantPhase = false;
                group.members.forEach(member => member.setIsInstantPhase?.(false));
                group.timeoutHandle = null;
            }, group.timeoutMs);
        } else {
            group.isInstantPhase = false;
            group.members.forEach(member => member.setIsInstantPhase?.(false));
        }
    }
}

/**
 * Disposes a delay group and all its internal state.
 */
export function disposeDelayGroup(groupId) {
    const group = delayGroups.get(groupId);
    if (!group) return;
    group.dispose();
}

// ============================================================================
// Export state for debugging
// ============================================================================

export function getFloatingState() {
    return state;
}
