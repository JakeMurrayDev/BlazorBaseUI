const STATE_KEY = Symbol.for('BlazorBaseUI.Portal.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = new Map();
}

const portalMap = window[STATE_KEY];

export function createPortal(id, target = "body") {
    const content = document.getElementById(id);
    const container = document.querySelector(target);

    if (content && container) {
        if (!portalMap.has(id)) {
            portalMap.set(id, content.parentNode);
        }
        container.appendChild(content);
    }
}

export function restorePortal(id) {
    const content = document.getElementById(id);
    const originalParent = portalMap.get(id);

    if (content && originalParent) {
        originalParent.appendChild(content);
        portalMap.delete(id);
    } else if (content) {
        content.remove();
        portalMap.delete(id);
    }
}
