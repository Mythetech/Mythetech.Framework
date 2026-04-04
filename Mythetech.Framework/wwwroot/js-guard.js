window.__jsGuards = window.__jsGuards || {};

/**
 * Registers a named guard that polls a check function until it returns truthy
 * or times out after ~10 seconds.
 *
 * @param {string} name - Unique guard identifier
 * @param {function(): boolean} checkFn - Returns truthy when the dependency is ready
 */
window.registerJsGuard = function (name, checkFn) {
    window.__jsGuards[name] = new Promise(function (resolve) {
        if (checkFn()) {
            resolve(true);
            return;
        }
        var checks = 0;
        var interval = setInterval(function () {
            if (checkFn()) {
                clearInterval(interval);
                resolve(true);
            } else if (++checks > 200) {
                clearInterval(interval);
                resolve(false);
            }
        }, 50);
    });
};

/**
 * Returns the guard's Promise, resolving to true (ready) or false (timeout/unregistered).
 *
 * @param {string} name - Guard identifier previously passed to registerJsGuard
 * @returns {Promise<boolean>}
 */
window.waitForJsGuard = function (name) {
    return window.__jsGuards[name] || Promise.resolve(false);
};
