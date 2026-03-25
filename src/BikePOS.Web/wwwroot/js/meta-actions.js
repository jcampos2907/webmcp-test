// Meta field action executor + input mask
// Called from Blazor via JS interop to run user-defined scripts on field values
window.MetaFieldActions = {
    // Blocked patterns — prevent network, DOM, cookie, eval, and global access
    _blocklist: /\b(fetch|XMLHttpRequest|import|require|eval|Function|setTimeout|setInterval|document\.|window\.|globalThis|localStorage|sessionStorage|cookie|indexedDB|WebSocket|Worker|ServiceWorker|navigator\.|location\.|history\.)\b/,

    /**
     * Execute a JS action script on a value in a sandboxed scope.
     */
    execute: function (script, value) {
        try {
            if (this._blocklist.test(script)) {
                console.warn('[MetaFieldAction] Blocked: script references a restricted API.');
                return value;
            }

            var fn = new Function(
                'value',
                '_window', '_document', '_globalThis', '_self',
                '_fetch', '_XMLHttpRequest', '_eval', '_Function',
                '_setTimeout', '_setInterval',
                '_localStorage', '_sessionStorage',
                '_navigator', '_location', '_history',
                'var window=_window,document=_document,globalThis=_globalThis,self=_self,' +
                'fetch=_fetch,XMLHttpRequest=_XMLHttpRequest,eval=_eval,Function=_Function,' +
                'setTimeout=_setTimeout,setInterval=_setInterval,' +
                'localStorage=_localStorage,sessionStorage=_sessionStorage,' +
                'navigator=_navigator,location=_location,history=_history;\n' +
                '"use strict";\n' + script
            );

            var result = fn(value);
            return (result !== undefined && result !== null) ? String(result) : value;
        } catch (e) {
            console.warn('[MetaFieldAction] Script error:', e.message);
            return value;
        }
    },

    /**
     * Apply a format mask to a value as the user types.
     * Mask chars: X = any alphanumeric, 9 = digit, A = letter.
     * Literal chars (like - or .) are inserted automatically.
     * @param {string} mask - e.g. "9-9999-9999"
     * @param {string} rawValue - current input value
     * @returns {string} masked value
     */
    applyMask: function (mask, rawValue) {
        if (!mask || !rawValue) return rawValue || '';

        // Strip out all literal (non-placeholder) chars from the raw value
        var literals = new Set();
        for (var i = 0; i < mask.length; i++) {
            var mc = mask[i];
            if (mc !== 'X' && mc !== '9' && mc !== 'A') {
                literals.add(mc);
            }
        }
        var stripped = '';
        for (var j = 0; j < rawValue.length; j++) {
            if (!literals.has(rawValue[j])) stripped += rawValue[j];
        }

        var result = '';
        var si = 0; // index into stripped
        for (var mi = 0; mi < mask.length && si < stripped.length; mi++) {
            var mc = mask[mi];
            if (mc === '9') {
                // digit only
                if (/\d/.test(stripped[si])) {
                    result += stripped[si];
                    si++;
                } else {
                    si++; mi--; // skip non-digit
                }
            } else if (mc === 'A') {
                // letter only
                if (/[a-zA-Z]/.test(stripped[si])) {
                    result += stripped[si];
                    si++;
                } else {
                    si++; mi--;
                }
            } else if (mc === 'X') {
                // any alphanumeric
                if (/[a-zA-Z0-9]/.test(stripped[si])) {
                    result += stripped[si];
                    si++;
                } else {
                    si++; mi--;
                }
            } else {
                // literal char in mask — insert it
                result += mc;
            }
        }
        return result;
    }
};
