window.__jsGuards = window.__jsGuards || {};

/**
 * Registers a named guard that polls a check function until it returns truthy
 * or exhausts its internal check budget (~10 seconds).
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
 * Waits for a named guard to be both registered and ready. If the consumer's
 * registerJsGuard() runs after this wait starts (e.g. a JS module that's still
 * loading), this function picks up the registration on its next poll instead of
 * returning false immediately.
 *
 * @param {string} name - Guard identifier
 * @param {number} [timeoutMs=15000] - Overall timeout covering both registration and readiness
 * @returns {Promise<boolean>} - Resolves true when ready, false on overall timeout
 */
window.waitForJsGuard = function (name, timeoutMs) {
    timeoutMs = timeoutMs || 15000;
    return new Promise(function (resolve) {
        var start = Date.now();
        var settled = false;
        var done = function (value) {
            if (settled) return;
            settled = true;
            resolve(value);
        };

        (function poll() {
            if (settled) return;

            var guard = window.__jsGuards[name];
            if (guard) {
                guard.then(done);
                var remaining = timeoutMs - (Date.now() - start);
                if (remaining > 0) {
                    setTimeout(function () { done(false); }, remaining);
                } else {
                    done(false);
                }
                return;
            }

            if (Date.now() - start >= timeoutMs) {
                done(false);
                return;
            }

            setTimeout(poll, 50);
        })();
    });
};

/**
 * Clears any stored guard promise for the given name so the next waitForJsGuard
 * call (or a fresh registerJsGuard) starts from a clean slate. Used by the
 * retry path so a previously-failed promise can't poison subsequent attempts.
 *
 * @param {string} name - Guard identifier to clear
 */
window.clearJsGuard = function (name) {
    delete window.__jsGuards[name];
};
