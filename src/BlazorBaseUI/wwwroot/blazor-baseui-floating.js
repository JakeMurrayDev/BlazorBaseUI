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
// Floating UI Library Loading
// ============================================================================

const FLOATING_UI_KEY = Symbol.for('BlazorBaseUI.FloatingUI');

async function loadScript(src) {
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src="${src}"]`)) {
            resolve();
            return;
        }

        const script = document.createElement('script');
        script.src = src;
        script.onload = resolve;
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

const FOCUSABLE_ATTRIBUTE = 'data-floating-ui-focusable';

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
        disableAnchorTracking = false
    } = options;

    if (!positionerElement || !triggerElement) return null;

    const positionerId = positionerElement.id ||
        `positioner-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

    const positionerState = {
        positionerId,
        positionerElement,
        triggerElement,
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
        positionMethod
    } = positionerState;

    if (!positionerElement || !triggerElement) return;

    try {
        const FloatingUI = await ensureFloatingUI();

        const placement = toFloatingPlacement(side, align);

        // Build middleware array
        const middleware = [];

        // Offset middleware
        if (sideOffset !== 0 || alignOffset !== 0) {
            middleware.push(FloatingUI.offset({
                mainAxis: sideOffset,
                crossAxis: alignOffset
            }));
        }

        // Flip middleware (handles side collision)
        middleware.push(FloatingUI.flip({
            padding: collisionPadding
        }));

        // Shift middleware (handles alignment collision)
        if (sticky) {
            middleware.push(FloatingUI.shift({
                padding: collisionPadding,
                limiter: FloatingUI.limitShift()
            }));
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
        ancestorScroll = false
    } = options;

    const doc = getDocument(floatingElement);
    let isComposing = false;

    function handleKeyDown(event) {
        if (!escapeKey || event.key !== 'Escape') {
            return;
        }

        // Wait until IME is settled
        if (isComposing) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
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

    function handleOutsidePress(event) {
        if (!outsidePress) {
            return;
        }

        const target = getTarget(event);

        // Check if click is inside floating or trigger
        if (isEventTargetWithin(event, floatingElement) || isEventTargetWithin(event, triggerElement)) {
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

    // Attach event listeners
    doc.addEventListener('keydown', handleKeyDown);
    doc.addEventListener('compositionstart', handleCompositionStart);
    doc.addEventListener('compositionend', handleCompositionEnd);

    if (outsidePress) {
        doc.addEventListener('mousedown', handleOutsidePress);
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

            if (outsidePress) {
                doc.removeEventListener('mousedown', handleOutsidePress);
            }

            scrollCleanup?.();
            state.interactions.delete(interactionId);
        }
    };

    state.interactions.set(interactionId, interaction);
    return interaction;
}

// ============================================================================
// Export state for debugging
// ============================================================================

export function getFloatingState() {
    return state;
}
