const STATE_KEY = Symbol.for('BlazorBaseUI.Slider.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const state = window[STATE_KEY];

export function initialize(controlElement, dotNetRef, disabled, readOnly, orientation) {
    if (!controlElement) return;

    const elementState = {
        dotNetRef,
        disabled,
        readOnly,
        orientation,
        dragging: false,
        pressedThumbIndex: -1,
        thumbCenterOffset: 0,
        pressedValues: null,
        latestValues: null,
        controlRect: null,
        config: null,
        thumbElements: [],
        indicatorElement: null,
        boundHandlers: null
    };

    state.set(controlElement, elementState);
}

export function dispose(controlElement) {
    if (!controlElement) return;

    const elementState = state.get(controlElement);
    if (elementState && elementState.boundHandlers) {
        document.removeEventListener('pointermove', elementState.boundHandlers.pointerMove);
        document.removeEventListener('pointerup', elementState.boundHandlers.pointerUp);
        document.removeEventListener('pointercancel', elementState.boundHandlers.pointerUp);
    }

    state.delete(controlElement);
}

export function updateState(controlElement, disabled, readOnly, orientation) {
    if (!controlElement) return;

    const elementState = state.get(controlElement);
    if (elementState) {
        elementState.disabled = disabled;
        elementState.readOnly = readOnly;
        elementState.orientation = orientation;
    }
}

export function startDrag(controlElement, dotNetRef, config, thumbElements, indicatorElement, clientX, clientY) {
    if (!controlElement) return null;

    // Convert thumbElements to a proper array (it comes from C# as array-like)
    const thumbArray = thumbElements ? Array.from(thumbElements).filter(el => el != null) : [];
    
    if (thumbArray.length === 0) {
        return null;
    }

    // Use the passed indicator element directly
    const indicator = indicatorElement;

    let elementState = state.get(controlElement);
    if (!elementState) {
        elementState = {
            dotNetRef,
            disabled: config.disabled,
            readOnly: config.readOnly,
            orientation: config.orientation,
            dragging: false,
            pressedThumbIndex: -1,
            thumbCenterOffset: 0,
            pressedValues: null,
            latestValues: null,
            controlRect: null,
            config: null,
            thumbElements: [],
            indicatorElement: null,
            boundHandlers: null
        };
        state.set(controlElement, elementState);
    }

    // Clean up any previous drag state
    if (elementState.boundHandlers) {
        document.removeEventListener('pointermove', elementState.boundHandlers.pointerMove);
        document.removeEventListener('pointerup', elementState.boundHandlers.pointerUp);
        document.removeEventListener('pointercancel', elementState.boundHandlers.pointerUp);
    }

    elementState.dotNetRef = dotNetRef;
    elementState.config = config;
    elementState.thumbElements = thumbArray;
    elementState.indicatorElement = indicator;
    elementState.pressedValues = [...config.values];
    elementState.latestValues = [...config.values];
    elementState.dragging = true;

    // Check if click was directly on a thumb element
    const clickedThumbIndex = findClickedThumbIndex(thumbArray, clientX, clientY);
    const wasClickOnThumb = clickedThumbIndex >= 0;
    
    // Find closest thumb based on pointer position (for track clicks)
    const closestIndex = wasClickOnThumb ? clickedThumbIndex : findClosestThumbByPosition(thumbArray, clientX, clientY, config.orientation);
    elementState.pressedThumbIndex = closestIndex;

    // Only calculate thumb center offset when clicking directly on a thumb
    // For track clicks, we want the value calculated from the actual click position
    const thumbEl = thumbArray[closestIndex];
    if (thumbEl) {
        if (wasClickOnThumb) {
            elementState.thumbCenterOffset = getThumbCenterOffsetInternal(thumbEl, clientX, clientY, config.orientation);
        } else {
            elementState.thumbCenterOffset = 0;
        }       
    }

    // Calculate initial value from pointer position
    const newValue = calculateFingerValueInternal(controlElement, clientX, clientY, config, elementState.thumbCenterOffset);
    if (newValue !== null) {
        const result = resolveCollision(config, elementState.latestValues, elementState.pressedValues, closestIndex, newValue);
        elementState.latestValues = result.values;
        elementState.pressedThumbIndex = result.thumbIndex;

        updateThumbPositions(elementState, config);
        updateIndicatorPosition(elementState, config);
    }

    // Set up event handlers
    if (!elementState.boundHandlers) {
        elementState.boundHandlers = {
            pointerMove: (e) => handlePointerMove(controlElement, e),
            pointerUp: (e) => handlePointerUp(controlElement, e)
        };
    }

    document.addEventListener('pointermove', elementState.boundHandlers.pointerMove);
    document.addEventListener('pointerup', elementState.boundHandlers.pointerUp);
    document.addEventListener('pointercancel', elementState.boundHandlers.pointerUp);

    // Ensure values are proper numbers for C# deserialization
    const safeValues = (elementState.latestValues || config.values || [0]).map(v => {
        const num = Number(v);
        return Number.isFinite(num) ? num : 0;
    });

    return {
        thumbIndex: elementState.pressedThumbIndex >= 0 ? elementState.pressedThumbIndex : 0,
        values: safeValues
    };
}

function findClickedThumbIndex(thumbElements, clientX, clientY) {
    if (!thumbElements || thumbElements.length === 0) return -1;

    for (let i = 0; i < thumbElements.length; i++) {
        const thumb = thumbElements[i];
        if (!thumb) continue;

        const rect = thumb.getBoundingClientRect();
        if (rect.width === 0 && rect.height === 0) continue;

        // Check if click point is within the thumb's bounding rect
        if (clientX >= rect.left && clientX <= rect.right &&
            clientY >= rect.top && clientY <= rect.bottom) {
            return i;
        }
    }

    return -1;
}

function findClosestThumbByPosition(thumbElements, clientX, clientY, orientation) {
    if (!thumbElements || thumbElements.length === 0) return 0;
    if (thumbElements.length === 1) return 0;

    const vertical = orientation === 'vertical';
    let closestIndex = 0;
    let minDistance = Infinity;

    for (let i = 0; i < thumbElements.length; i++) {
        const thumb = thumbElements[i];
        if (!thumb) continue;

        const rect = thumb.getBoundingClientRect();
        if (rect.width === 0 && rect.height === 0) continue;

        const midpoint = vertical
            ? (rect.top + rect.bottom) / 2
            : (rect.left + rect.right) / 2;

        const fingerPos = vertical ? clientY : clientX;
        const distance = Math.abs(fingerPos - midpoint);

        if (distance < minDistance) {
            minDistance = distance;
            closestIndex = i;
        }
    }

    return closestIndex;
}

function arraysEqual(a, b) {
    if (!a || !b) return false;
    if (a.length !== b.length) return false;
    for (let i = 0; i < a.length; i++) {
        if (Math.abs(a[i] - b[i]) > 1e-10) return false;
    }
    return true;
}

function handlePointerMove(controlElement, e) {
    const elementState = state.get(controlElement);
    if (!elementState || !elementState.dragging || !elementState.config) return;

    const config = elementState.config;
    const newValue = calculateFingerValueInternal(controlElement, e.clientX, e.clientY, config, elementState.thumbCenterOffset);

    if (newValue === null) return;

    const result = resolveCollision(config, elementState.latestValues, elementState.pressedValues, elementState.pressedThumbIndex, newValue);

    if (!validateMinimumDistance(result.values, config.step, config.minStepsBetweenValues)) return;

    const valuesChanged = !arraysEqual(result.values, elementState.latestValues);

    if (config.collisionBehavior === 'swap' && result.didSwap) {
        elementState.pressedThumbIndex = result.thumbIndex;
        focusThumbInput(elementState.thumbElements[result.thumbIndex]);
    }

    elementState.latestValues = result.values;

    updateThumbPositions(elementState, config);
    updateIndicatorPosition(elementState, config);

    if (valuesChanged && config.notifyOnMove && elementState.dotNetRef) {
        const safeValues = result.values.map(v => {
            const num = Number(v);
            return Number.isFinite(num) ? num : 0;
        });
        elementState.dotNetRef.invokeMethodAsync('OnDragMove', safeValues, elementState.pressedThumbIndex);
    }
}

function handlePointerUp(controlElement, e) {
    const elementState = state.get(controlElement);
    if (!elementState) return;

    if (elementState.boundHandlers) {
        document.removeEventListener('pointermove', elementState.boundHandlers.pointerMove);
        document.removeEventListener('pointerup', elementState.boundHandlers.pointerUp);
        document.removeEventListener('pointercancel', elementState.boundHandlers.pointerUp);
    }

    const wasDragging = elementState.dragging;
    const finalValues = elementState.latestValues ? [...elementState.latestValues] : null;
    const finalThumbIndex = elementState.pressedThumbIndex;

    elementState.dragging = false;
    elementState.pressedThumbIndex = -1;
    elementState.thumbCenterOffset = 0;
    elementState.pressedValues = null;
    elementState.latestValues = null;

    if (wasDragging && finalValues && elementState.dotNetRef) {
        const safeValues = finalValues.map(v => {
            const num = Number(v);
            return Number.isFinite(num) ? num : 0;
        });
        elementState.dotNetRef.invokeMethodAsync('OnDragEnd', safeValues, finalThumbIndex >= 0 ? finalThumbIndex : 0);
    }
}

function updateThumbPositions(elementState, config) {
    if (!elementState.thumbElements || !elementState.latestValues) return;

    const vertical = config.orientation === 'vertical';

    for (let i = 0; i < elementState.thumbElements.length && i < elementState.latestValues.length; i++) {
        const thumb = elementState.thumbElements[i];
        if (!thumb) continue;

        const value = elementState.latestValues[i];
        const percent = valueToPercent(value, config.min, config.max);

        if (vertical) {
            thumb.style.bottom = `${percent}%`;
            thumb.style.left = '50%';
            thumb.style.transform = 'translate(-50%, 50%)';
            thumb.style.position = 'absolute';
        } else {
            thumb.style.top = '50%';
            thumb.style.insetInlineStart = `${percent}%`;
            thumb.style.transform = 'translate(-50%, -50%)';
            thumb.style.position = 'absolute';
        }
    }
}

function updateIndicatorPosition(elementState, config) {
    const indicator = elementState.indicatorElement;
    if (!indicator || !elementState.latestValues) return;

    const values = elementState.latestValues;
    const vertical = config.orientation === 'vertical';
    const isRange = values.length > 1;

    const startPercent = valueToPercent(values[0], config.min, config.max);
    const endPercent = isRange ? valueToPercent(values[values.length - 1], config.min, config.max) : startPercent;

    if (vertical) {
        if (!isRange) {
            indicator.style.position = 'absolute';
            indicator.style.width = 'inherit';
            indicator.style.bottom = '0';
            indicator.style.height = `${startPercent}%`;
        } else {
            const size = endPercent - startPercent;
            indicator.style.position = 'absolute';
            indicator.style.width = 'inherit';
            indicator.style.bottom = `${startPercent}%`;
            indicator.style.height = `${size}%`;
        }
    } else {
        if (!isRange) {
            indicator.style.position = 'relative';
            indicator.style.height = 'inherit';
            indicator.style.insetInlineStart = '0';
            indicator.style.width = `${startPercent}%`;
        } else {
            const size = endPercent - startPercent;
            indicator.style.position = 'relative';
            indicator.style.height = 'inherit';
            indicator.style.insetInlineStart = `${startPercent}%`;
            indicator.style.width = `${size}%`;
        }
    }
}

function valueToPercent(value, min, max) {
    return ((value - min) / (max - min)) * 100;
}

function calculateFingerValueInternal(controlElement, clientX, clientY, config, thumbCenterOffset) {
    if (!controlElement) return null;

    const rect = controlElement.getBoundingClientRect();
    const styles = getComputedStyle(controlElement);
    const vertical = config.orientation === 'vertical';
    const direction = config.direction || 'ltr';
    const insetOffset = config.thumbAlignment === 'edge' ? config.insetOffset || 0 : 0;

    const paddingStart = vertical
        ? (parseFloat(styles.paddingTop) || 0) + (parseFloat(styles.borderTopWidth) || 0)
        : (parseFloat(styles.paddingInlineStart) || parseFloat(styles.paddingLeft) || 0) +
          (parseFloat(styles.borderInlineStartWidth) || parseFloat(styles.borderLeftWidth) || 0);

    const paddingEnd = vertical
        ? (parseFloat(styles.paddingBottom) || 0) + (parseFloat(styles.borderBottomWidth) || 0)
        : (parseFloat(styles.paddingInlineEnd) || parseFloat(styles.paddingRight) || 0) +
          (parseFloat(styles.borderInlineEndWidth) || parseFloat(styles.borderRightWidth) || 0);

    const controlSize = (vertical ? rect.height : rect.width) - paddingStart - paddingEnd - (insetOffset * 2);

    const adjustedX = clientX - (thumbCenterOffset || 0);
    const adjustedY = clientY - (thumbCenterOffset || 0);

    let valueSize;
    if (vertical) {
        valueSize = rect.bottom - adjustedY - paddingEnd;
    } else if (direction === 'rtl') {
        valueSize = rect.right - adjustedX - paddingEnd;
    } else {
        valueSize = adjustedX - rect.left - paddingStart;
    }

    const valueRescaled = Math.max(0, Math.min(1, (valueSize - insetOffset) / controlSize));
    let newValue = (config.max - config.min) * valueRescaled + config.min;

    if (config.step > 0) {
        newValue = Math.round((newValue - config.min) / config.step) * config.step + config.min;
    }

    newValue = Math.max(config.min, Math.min(config.max, newValue));

    return Math.round(newValue * 1e12) / 1e12;
}

function getThumbCenterOffsetInternal(thumbElement, clientX, clientY, orientation) {
    if (!thumbElement) return 0;

    const rect = thumbElement.getBoundingClientRect();
    const vertical = orientation === 'vertical';

    if (vertical) {
        const midY = (rect.top + rect.bottom) / 2;
        return clientY - midY;
    } else {
        const midX = (rect.left + rect.right) / 2;
        return clientX - midX;
    }
}

function resolveCollision(config, currentValues, pressedValues, pressedIndex, nextValue) {
    if (!currentValues || currentValues.length === 0) {
        return { values: [nextValue], thumbIndex: 0, didSwap: false };
    }

    if (pressedIndex < 0 || pressedIndex >= currentValues.length) {
        return { values: [...currentValues], thumbIndex: Math.max(0, Math.min(pressedIndex, currentValues.length - 1)), didSwap: false };
    }

    const isRange = currentValues.length > 1;
    if (!isRange) {
        return { values: [clamp(nextValue, config.min, config.max)], thumbIndex: 0, didSwap: false };
    }

    const minValueDifference = config.step * config.minStepsBetweenValues;
    const behavior = config.collisionBehavior || 'push';

    switch (behavior) {
        case 'swap':
            return resolveSwapCollision(config, currentValues, pressedValues, pressedIndex, nextValue, minValueDifference);
        case 'push':
            return resolvePushCollision(config, currentValues, pressedIndex, nextValue, minValueDifference);
        case 'none':
        default:
            return resolveNoneCollision(config, currentValues, pressedIndex, nextValue, minValueDifference);
    }
}

function resolveSwapCollision(config, currentValues, pressedValues, pressedIndex, nextValue, minValueDifference) {
    const epsilon = 1e-7;
    const candidateValues = [...currentValues];
    const pressedInitialValue = currentValues[pressedIndex];

    const previousNeighbor = pressedIndex > 0 ? candidateValues[pressedIndex - 1] : null;
    const nextNeighbor = pressedIndex < candidateValues.length - 1 ? candidateValues[pressedIndex + 1] : null;

    const lowerBound = previousNeighbor !== null ? previousNeighbor + minValueDifference : config.min;
    const upperBound = nextNeighbor !== null ? nextNeighbor - minValueDifference : config.max;

    const constrainedValue = clamp(nextValue, lowerBound, upperBound);
    candidateValues[pressedIndex] = round12(constrainedValue);

    const movingForward = nextValue > pressedInitialValue;
    const movingBackward = nextValue < pressedInitialValue;

    const shouldSwapForward = movingForward && nextNeighbor !== null && nextValue >= nextNeighbor - epsilon;
    const shouldSwapBackward = movingBackward && previousNeighbor !== null && nextValue <= previousNeighbor + epsilon;

    if (!shouldSwapForward && !shouldSwapBackward) {
        return { values: candidateValues, thumbIndex: pressedIndex, didSwap: false };
    }

    const targetIndex = shouldSwapForward ? pressedIndex + 1 : pressedIndex - 1;

    if (targetIndex < 0 || targetIndex >= candidateValues.length) {
        return { values: candidateValues, thumbIndex: pressedIndex, didSwap: false };
    }

    const nextValueForTarget = shouldSwapForward
        ? Math.max(nextValue, candidateValues[targetIndex])
        : Math.min(nextValue, candidateValues[targetIndex]);

    const adjustedValues = getPushedThumbValues(candidateValues, targetIndex, nextValueForTarget, config, minValueDifference);

    const neighborIndex = shouldSwapForward ? targetIndex - 1 : targetIndex + 1;
    if (neighborIndex >= 0 && neighborIndex < adjustedValues.length) {
        const prevVal = neighborIndex > 0 ? adjustedValues[neighborIndex - 1] : null;
        const nextVal = neighborIndex < adjustedValues.length - 1 ? adjustedValues[neighborIndex + 1] : null;

        let neighborLowerBound = prevVal !== null ? prevVal + minValueDifference : config.min;
        neighborLowerBound = Math.max(neighborLowerBound, config.min + neighborIndex * minValueDifference);

        let neighborUpperBound = nextVal !== null ? nextVal - minValueDifference : config.max;
        neighborUpperBound = Math.min(neighborUpperBound, config.max - (adjustedValues.length - 1 - neighborIndex) * minValueDifference);

        adjustedValues[neighborIndex] = round12(clamp(candidateValues[pressedIndex], neighborLowerBound, neighborUpperBound));
    }

    return { values: adjustedValues, thumbIndex: targetIndex, didSwap: true };
}

function resolvePushCollision(config, currentValues, pressedIndex, nextValue, minValueDifference) {
    const pushedValues = getPushedThumbValues([...currentValues], pressedIndex, nextValue, config, minValueDifference);
    return { values: pushedValues, thumbIndex: pressedIndex, didSwap: false };
}

function resolveNoneCollision(config, currentValues, pressedIndex, nextValue, minValueDifference) {
    const candidateValues = [...currentValues];
    const previousNeighbor = pressedIndex > 0 ? candidateValues[pressedIndex - 1] : null;
    const nextNeighbor = pressedIndex < candidateValues.length - 1 ? candidateValues[pressedIndex + 1] : null;

    const lowerBound = previousNeighbor !== null ? previousNeighbor + minValueDifference : config.min;
    const upperBound = nextNeighbor !== null ? nextNeighbor - minValueDifference : config.max;

    candidateValues[pressedIndex] = round12(clamp(nextValue, lowerBound, upperBound));

    return { values: candidateValues, thumbIndex: pressedIndex, didSwap: false };
}

function getPushedThumbValues(values, index, nextValue, config, minDistance) {
    if (index < 0 || index >= values.length) {
        return values;
    }

    const result = [...values];
    result[index] = clamp(nextValue, config.min, config.max);

    for (let i = index + 1; i < result.length; i++) {
        const minAllowed = result[i - 1] + minDistance;
        if (result[i] < minAllowed) {
            result[i] = Math.min(minAllowed, config.max - (result.length - 1 - i) * minDistance);
        }
    }

    for (let i = index - 1; i >= 0; i--) {
        const maxAllowed = result[i + 1] - minDistance;
        if (result[i] > maxAllowed) {
            result[i] = Math.max(maxAllowed, config.min + i * minDistance);
        }
    }

    for (let i = 0; i < result.length; i++) {
        result[i] = round12(result[i]);
    }

    return result;
}

function validateMinimumDistance(values, step, minStepsBetweenValues) {
    if (values.length < 2) return true;

    const minDistance = step * minStepsBetweenValues;

    for (let i = 0; i < values.length - 1; i++) {
        const distance = Math.abs(values[i + 1] - values[i]);
        if (distance < minDistance - 1e-10) return false;
    }

    return true;
}

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}

function round12(value) {
    return Math.round(value * 1e12) / 1e12;
}

export function getControlRect(controlElement) {
    if (!controlElement) return null;

    const rect = controlElement.getBoundingClientRect();
    const styles = getComputedStyle(controlElement);
    const elementState = state.get(controlElement);
    const vertical = elementState?.orientation === 'vertical';

    const startProp = !vertical ? 'paddingInlineStart' : 'paddingTop';
    const endProp = !vertical ? 'paddingInlineEnd' : 'paddingBottom';
    const borderStartProp = !vertical ? 'borderInlineStartWidth' : 'borderTopWidth';
    const borderEndProp = !vertical ? 'borderInlineEndWidth' : 'borderBottomWidth';

    const paddingStart = parseFloat(styles[startProp]) || 0;
    const paddingEnd = parseFloat(styles[endProp]) || 0;
    const borderStart = parseFloat(styles[borderStartProp]) || 0;
    const borderEnd = parseFloat(styles[borderEndProp]) || 0;

    return {
        left: rect.left,
        right: rect.right,
        top: rect.top,
        bottom: rect.bottom,
        width: rect.width,
        height: rect.height,
        paddingStart,
        paddingEnd,
        borderStart,
        borderEnd
    };
}

export function getThumbRect(thumbElement) {
    if (!thumbElement) return null;

    const rect = thumbElement.getBoundingClientRect();
    return {
        left: rect.left,
        right: rect.right,
        top: rect.top,
        bottom: rect.bottom,
        width: rect.width,
        height: rect.height,
        midX: (rect.left + rect.right) / 2,
        midY: (rect.top + rect.bottom) / 2
    };
}

export function setPointerCapture(controlElement, pointerId) {
    if (!controlElement || !pointerId) return;

    try {
        controlElement.setPointerCapture(pointerId);
    } catch (e) {
        // Pointer may have been released
    }
}

export function releasePointerCapture(controlElement, pointerId) {
    if (!controlElement || !pointerId) return;

    try {
        controlElement.releasePointerCapture(pointerId);
    } catch (e) {
        // Pointer may have already been released
    }
}

export function focusThumbInput(thumbElement) {
    if (!thumbElement) return;

    const input = thumbElement.querySelector('input[type="range"]');
    if (input) {
        input.focus({ preventScroll: true });
    }
}

export function focusElement(element) {
    if (!element) return;
    element.focus({ preventScroll: true });
}

export function getDirection(element) {
    if (!element) return 'ltr';
    return getComputedStyle(element).direction || 'ltr';
}

export function calculateFingerValue(controlElement, clientX, clientY, min, max, step, thumbCenterOffset, insetOffset, direction) {
    if (!controlElement) return null;

    const rect = controlElement.getBoundingClientRect();
    const styles = getComputedStyle(controlElement);
    const elementState = state.get(controlElement);
    const vertical = elementState?.orientation === 'vertical';

    const paddingStart = vertical
        ? (parseFloat(styles.paddingTop) || 0) + (parseFloat(styles.borderTopWidth) || 0)
        : (parseFloat(styles.paddingInlineStart) || parseFloat(styles.paddingLeft) || 0) +
          (parseFloat(styles.borderInlineStartWidth) || parseFloat(styles.borderLeftWidth) || 0);

    const paddingEnd = vertical
        ? (parseFloat(styles.paddingBottom) || 0) + (parseFloat(styles.borderBottomWidth) || 0)
        : (parseFloat(styles.paddingInlineEnd) || parseFloat(styles.paddingRight) || 0) +
          (parseFloat(styles.borderInlineEndWidth) || parseFloat(styles.borderRightWidth) || 0);

    const controlSize = (vertical ? rect.height : rect.width) - paddingStart - paddingEnd - (insetOffset * 2);

    const adjustedX = clientX - (thumbCenterOffset || 0);
    const adjustedY = clientY - (thumbCenterOffset || 0);

    let valueSize;
    if (vertical) {
        valueSize = rect.bottom - adjustedY - paddingEnd;
    } else if (direction === 'rtl') {
        valueSize = rect.right - adjustedX - paddingEnd;
    } else {
        valueSize = adjustedX - rect.left - paddingStart;
    }

    const valueRescaled = Math.max(0, Math.min(1, (valueSize - insetOffset) / controlSize));
    let newValue = (max - min) * valueRescaled + min;

    if (step > 0) {
        newValue = Math.round((newValue - min) / step) * step + min;
    }

    newValue = Math.max(min, Math.min(max, newValue));

    return Math.round(newValue * 1e12) / 1e12;
}

export function findClosestThumbIndex(controlElement, thumbElements, clientX, clientY) {
    if (!controlElement || !thumbElements || thumbElements.length === 0) return 0;

    const elementState = state.get(controlElement);
    const vertical = elementState?.orientation === 'vertical';

    return findClosestThumbByPosition(Array.from(thumbElements), clientX, clientY, vertical ? 'vertical' : 'horizontal');
}

export function getThumbCenterOffset(thumbElement, clientX, clientY, orientation) {
    return getThumbCenterOffsetInternal(thumbElement, clientX, clientY, orientation);
}

export function setInputValue(inputElement, value) {
    if (!inputElement) return;
    inputElement.value = value;
}

export function getActiveElement() {
    return document.activeElement;
}

export function containsElement(parent, child) {
    if (!parent || !child) return false;
    return parent.contains(child);
}

export function stopDrag(controlElement) {
    const elementState = state.get(controlElement);
    if (!elementState) return;

    if (elementState.boundHandlers) {
        document.removeEventListener('pointermove', elementState.boundHandlers.pointerMove);
        document.removeEventListener('pointerup', elementState.boundHandlers.pointerUp);
        document.removeEventListener('pointercancel', elementState.boundHandlers.pointerUp);
    }

    elementState.dragging = false;
    elementState.pressedThumbIndex = -1;
    elementState.thumbCenterOffset = 0;
    elementState.pressedValues = null;
    elementState.latestValues = null;
}
