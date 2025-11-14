window.theme = (function () {
    'use strict';

    const STORAGE_KEY = 'theme-preference';

    function getStoredTheme() {
        try {
            const stored = localStorage.getItem(STORAGE_KEY);
            if (stored === 'true') {
                return true;
            }
            if (stored === 'false') {
                return false;
            }
            return null;
        } catch (e) {
            return null;
        }
    }

    function setStoredTheme(isDark) {
        try {
            localStorage.setItem(STORAGE_KEY, isDark ? 'true' : 'false');
        } catch (e) {
            // Ignore storage errors
        }
    }

    function getSystemPreference() {
        try {
            if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                return true;
            }
            return false;
        } catch (e) {
            return false;
        }
    }

    function getInitialTheme() {
        const stored = getStoredTheme();
        if (stored !== null) {
            return stored;
        }
        return getSystemPreference();
    }

    return {
        getStoredTheme: getStoredTheme,
        setStoredTheme: setStoredTheme,
        getSystemPreference: getSystemPreference,
        getInitialTheme: getInitialTheme
    };
})();

