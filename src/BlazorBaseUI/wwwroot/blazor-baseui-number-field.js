const STATE_KEY = Symbol.for('BlazorBaseUI.NumberField.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const state = window[STATE_KEY];

const CHANGE_VALUE_TICK_DELAY = 60;
const START_AUTO_CHANGE_DELAY = 400;
const TOUCH_TIMEOUT = 50;
const MAX_POINTER_MOVES_AFTER_TOUCH = 3;
const SCROLLING_POINTER_MOVE_DISTANCE = 8;
const DEFAULT_STEP = 1;

export function initialize(inputElement, dotNetRef, config) {
    if (!inputElement) return;

    const elementState = {
        dotNetRef,
        config,
        wheelHandler: null,
        autoChangeState: null
    };

    state.set(inputElement, elementState);
}

export function dispose(inputElement) {
    if (!inputElement) return;

    const elementState = state.get(inputElement);
    if (elementState) {
        if (elementState.wheelHandler) {
            inputElement.removeEventListener('wheel', elementState.wheelHandler);
        }
        stopAutoChangeInternal(elementState);
    }

    state.delete(inputElement);
}

export function updateConfig(inputElement, config) {
    if (!inputElement) return;

    const elementState = state.get(inputElement);
    if (elementState) {
        elementState.config = config;
    }
}

export function registerWheelListener(inputElement, dotNetRef, disabled, readOnly) {
    if (!inputElement) return;

    let elementState = state.get(inputElement);
    if (!elementState) {
        elementState = { dotNetRef, config: {}, wheelHandler: null, autoChangeState: null };
        state.set(inputElement, elementState);
    }

    elementState.dotNetRef = dotNetRef;

    if (elementState.wheelHandler) {
        inputElement.removeEventListener('wheel', elementState.wheelHandler);
    }

    if (disabled || readOnly) return;

    elementState.wheelHandler = (event) => {
        if (event.ctrlKey || document.activeElement !== inputElement) {
            return;
        }

        event.preventDefault();

        const direction = event.deltaY > 0 ? -1 : 1;
        const altKey = event.altKey;
        const shiftKey = event.shiftKey;

        dotNetRef.invokeMethodAsync('OnWheelChange', direction, altKey, shiftKey);
    };

    inputElement.addEventListener('wheel', elementState.wheelHandler, { passive: false });
}

export function unregisterWheelListener(inputElement) {
    if (!inputElement) return;

    const elementState = state.get(inputElement);
    if (elementState && elementState.wheelHandler) {
        inputElement.removeEventListener('wheel', elementState.wheelHandler);
        elementState.wheelHandler = null;
    }
}

export function startAutoChange(inputElement, dotNetRef, isIncrement, step, min, max) {
    if (!inputElement) return;

    let elementState = state.get(inputElement);
    if (!elementState) {
        elementState = { dotNetRef, config: {}, wheelHandler: null, autoChangeState: null };
        state.set(inputElement, elementState);
    }

    stopAutoChangeInternal(elementState);

    elementState.autoChangeState = {
        isIncrement,
        step,
        min,
        max,
        tickTimeout: null,
        tickInterval: null,
        isPressing: true
    };

    const tick = () => {
        if (!elementState.autoChangeState || !elementState.autoChangeState.isPressing) return;
        dotNetRef.invokeMethodAsync('OnAutoChangeTick', isIncrement);
    };

    tick();

    elementState.autoChangeState.tickTimeout = setTimeout(() => {
        if (!elementState.autoChangeState) return;
        elementState.autoChangeState.tickInterval = setInterval(tick, CHANGE_VALUE_TICK_DELAY);
    }, START_AUTO_CHANGE_DELAY);

    const handlePointerUp = () => {
        stopAutoChangeInternal(elementState);
        dotNetRef.invokeMethodAsync('OnAutoChangeEnd', isIncrement);
        window.removeEventListener('pointerup', handlePointerUp);
    };

    window.addEventListener('pointerup', handlePointerUp, { once: true });
}

export function stopAutoChange(inputElement) {
    if (!inputElement) return;

    const elementState = state.get(inputElement);
    if (elementState) {
        stopAutoChangeInternal(elementState);
    }
}

function stopAutoChangeInternal(elementState) {
    if (!elementState || !elementState.autoChangeState) return;

    if (elementState.autoChangeState.tickTimeout) {
        clearTimeout(elementState.autoChangeState.tickTimeout);
    }
    if (elementState.autoChangeState.tickInterval) {
        clearInterval(elementState.autoChangeState.tickInterval);
    }
    elementState.autoChangeState = null;
}

export function initializeScrubArea(scrubAreaElement, dotNetRef, config) {
    if (!scrubAreaElement) return;

    const elementState = {
        dotNetRef,
        config,
        isScrubbingRef: false,
        virtualCursorCoords: { x: 0, y: 0 },
        visualScaleRef: 1,
        cumulativeDelta: 0,
        boundHandlers: null,
        cursorElement: null
    };

    state.set(scrubAreaElement, elementState);

    scrubAreaElement.addEventListener('touchstart', (event) => {
        if (event.touches.length === 1) {
            event.preventDefault();
        }
    }, { passive: false });
}

export function disposeScrubArea(scrubAreaElement) {
    if (!scrubAreaElement) return;

    const elementState = state.get(scrubAreaElement);
    if (elementState && elementState.boundHandlers) {
        window.removeEventListener('pointerup', elementState.boundHandlers.pointerUp, true);
        window.removeEventListener('pointermove', elementState.boundHandlers.pointerMove, true);
    }

    state.delete(scrubAreaElement);
}

export function startScrub(scrubAreaElement, dotNetRef, cursorElement, config, clientX, clientY, isTouch) {
    if (!scrubAreaElement) return { success: false };

    let elementState = state.get(scrubAreaElement);
    if (!elementState) {
        elementState = {
            dotNetRef,
            config,
            isScrubbingRef: false,
            virtualCursorCoords: { x: 0, y: 0 },
            visualScaleRef: 1,
            cumulativeDelta: 0,
            boundHandlers: null,
            cursorElement: null
        };
        state.set(scrubAreaElement, elementState);
    }

    elementState.dotNetRef = dotNetRef;
    elementState.config = config;
    elementState.cursorElement = cursorElement;
    elementState.isScrubbingRef = true;
    elementState.cumulativeDelta = 0;

    if (cursorElement) {
        const initialCoords = {
            x: clientX - cursorElement.offsetWidth / 2,
            y: clientY - cursorElement.offsetHeight / 2
        };
        elementState.virtualCursorCoords = initialCoords;
        updateCursorTransformInternal(cursorElement, initialCoords.x, initialCoords.y, elementState.visualScaleRef);
    }

    if (elementState.boundHandlers) {
        window.removeEventListener('pointerup', elementState.boundHandlers.pointerUp, true);
        window.removeEventListener('pointermove', elementState.boundHandlers.pointerMove, true);
    }

    elementState.boundHandlers = {
        pointerMove: (e) => handleScrubPointerMove(scrubAreaElement, e),
        pointerUp: (e) => handleScrubPointerUp(scrubAreaElement, e)
    };

    window.addEventListener('pointermove', elementState.boundHandlers.pointerMove, true);
    window.addEventListener('pointerup', elementState.boundHandlers.pointerUp, true);

    let pointerLockDenied = false;
    if (!isTouch && !isWebKit()) {
        try {
            document.body.requestPointerLock();
        } catch {
            pointerLockDenied = true;
        }
    }

    return { success: true, pointerLockDenied };
}

function handleScrubPointerMove(scrubAreaElement, event) {
    const elementState = state.get(scrubAreaElement);
    if (!elementState || !elementState.isScrubbingRef || !elementState.config) return;

    event.preventDefault();

    const config = elementState.config;
    const { movementX, movementY } = event;

    if (elementState.cursorElement) {
        const rect = getViewportRect(config.teleportDistance, scrubAreaElement);
        const coords = elementState.virtualCursorCoords;
        const newCoords = {
            x: Math.round(coords.x + movementX),
            y: Math.round(coords.y + movementY)
        };

        const cursorWidth = elementState.cursorElement.offsetWidth;
        const cursorHeight = elementState.cursorElement.offsetHeight;

        if (newCoords.x + cursorWidth / 2 < rect.x) {
            newCoords.x = rect.width - cursorWidth / 2;
        } else if (newCoords.x + cursorWidth / 2 > rect.width) {
            newCoords.x = rect.x - cursorWidth / 2;
        }

        if (newCoords.y + cursorHeight / 2 < rect.y) {
            newCoords.y = rect.height - cursorHeight / 2;
        } else if (newCoords.y + cursorHeight / 2 > rect.height) {
            newCoords.y = rect.y - cursorHeight / 2;
        }

        elementState.virtualCursorCoords = newCoords;
        updateCursorTransformInternal(elementState.cursorElement, newCoords.x, newCoords.y, elementState.visualScaleRef);
    }

    const isVertical = config.direction === 'vertical';
    elementState.cumulativeDelta += isVertical ? movementY : movementX;

    if (Math.abs(elementState.cumulativeDelta) >= config.pixelSensitivity) {
        elementState.cumulativeDelta = 0;
        const dValue = isVertical ? -movementY : movementX;

        if (dValue !== 0) {
            const direction = dValue >= 0 ? 1 : -1;
            elementState.dotNetRef.invokeMethodAsync('OnScrubMove', direction, event.altKey, event.shiftKey);
        }
    }
}

function handleScrubPointerUp(scrubAreaElement, event) {
    const elementState = state.get(scrubAreaElement);
    if (!elementState) return;

    try {
        document.exitPointerLock();
    } catch { }

    if (elementState.boundHandlers) {
        window.removeEventListener('pointermove', elementState.boundHandlers.pointerMove, true);
        window.removeEventListener('pointerup', elementState.boundHandlers.pointerUp, true);
    }

    elementState.isScrubbingRef = false;
    elementState.boundHandlers = null;

    if (elementState.dotNetRef) {
        elementState.dotNetRef.invokeMethodAsync('OnScrubEnd');
    }
}

function getViewportRect(teleportDistance, element) {
    const vv = window.visualViewport;
    if (teleportDistance != null) {
        const rect = element.getBoundingClientRect();
        const centerX = rect.left + rect.width / 2;
        const centerY = rect.top + rect.height / 2;
        return {
            x: centerX - teleportDistance,
            y: centerY - teleportDistance,
            width: centerX + teleportDistance,
            height: centerY + teleportDistance
        };
    }
    return {
        x: vv?.offsetLeft ?? 0,
        y: vv?.offsetTop ?? 0,
        width: vv?.width ?? window.innerWidth,
        height: vv?.height ?? window.innerHeight
    };
}

function updateCursorTransformInternal(cursorElement, x, y, scale) {
    if (cursorElement) {
        cursorElement.style.transform = `translate3d(${x}px,${y}px,0) scale(${1 / scale})`;
    }
}

export function updateCursorTransform(cursorElement, x, y, scale) {
    updateCursorTransformInternal(cursorElement, x, y, scale);
}

export function focusInput(inputElement) {
    if (inputElement) {
        inputElement.focus({ preventScroll: true });
    }
}

export function setInputValue(inputElement, value) {
    if (inputElement) {
        inputElement.value = value;
    }
}

export function setSelectionRange(inputElement, start, end) {
    if (inputElement && typeof inputElement.setSelectionRange === 'function') {
        try {
            inputElement.setSelectionRange(start, end);
        } catch { }
    }
}

export function getSelectionStart(inputElement) {
    if (inputElement) {
        return inputElement.selectionStart;
    }
    return 0;
}

export function getSelectionEnd(inputElement) {
    if (inputElement) {
        return inputElement.selectionEnd;
    }
    return 0;
}

function isWebKit() {
    return /AppleWebKit/.test(navigator.userAgent) && !/Chrome/.test(navigator.userAgent);
}

export function isWebKitBrowser() {
    return isWebKit();
}

export function isFirefoxBrowser() {
    return /Firefox/.test(navigator.userAgent);
}
