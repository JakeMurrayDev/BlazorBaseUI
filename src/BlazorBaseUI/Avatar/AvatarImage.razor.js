const STATE_KEY = Symbol.for('BlazorBaseUI.AvatarImage.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = { initialized: true };
}
const state = window[STATE_KEY];

export function loadImage(src, referrerPolicy, crossOrigin) {
    return new Promise((resolve) => {
        if (!src) {
            resolve('error');
            return;
        }

        const image = new Image();

        image.onload = () => resolve('loaded');
        image.onerror = () => resolve('error');

        if (referrerPolicy) {
            image.referrerPolicy = referrerPolicy;
        }
        image.crossOrigin = crossOrigin ?? null;
        image.src = src;
    });
}