/**
 * Floating UI Loader
 *
 * Loads the vendored Floating UI libraries (core + dom) and exposes them globally.
 * Version: @floating-ui/dom 1.7.4
 */

const FLOATING_UI_KEY = Symbol.for('BlazorBaseUI.FloatingUI');

async function loadScript(src) {
    return new Promise((resolve, reject) => {
        // Check if already loaded
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

export async function ensureFloatingUI() {
    if (window[FLOATING_UI_KEY]) {
        return window[FLOATING_UI_KEY];
    }

    // Get the base path for the vendor scripts
    const basePath = './_content/BlazorBaseUI/vendor/';

    // Load core first, then dom (dom depends on core)
    await loadScript(basePath + 'floating-ui.core.min.js');
    await loadScript(basePath + 'floating-ui.dom.min.js');

    // FloatingUIDOM should now be available globally
    if (typeof FloatingUIDOM !== 'undefined') {
        window[FLOATING_UI_KEY] = FloatingUIDOM;
        return FloatingUIDOM;
    }

    throw new Error('Failed to load Floating UI libraries');
}

// Also export individual functions for convenience
export async function computePosition(reference, floating, options) {
    const FloatingUI = await ensureFloatingUI();
    return FloatingUI.computePosition(reference, floating, options);
}

export async function autoUpdate(reference, floating, update, options) {
    const FloatingUI = await ensureFloatingUI();
    return FloatingUI.autoUpdate(reference, floating, update, options);
}
