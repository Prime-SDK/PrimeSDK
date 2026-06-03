const primeSDK_flags_library = {

    primeSDK_flags_getBool: function (keyUTF8, defaultValue) {
        const key = UTF8ToString(keyUTF8);
        const value = Module.primeSDK.flags.get(key, defaultValue);
        return value === "true" || value === 1 || value === true;
    },

    primeSDK_flags_getInt: function (keyUTF8, defaultValue) {
        const key = UTF8ToString(keyUTF8);
        return Module.primeSDK.flags.get(key, defaultValue);
    },

    primeSDK_flags_getFloat: function (keyUTF8, defaultValue) {
        const key = UTF8ToString(keyUTF8);
        return Module.primeSDK.flags.get(key, defaultValue);
    },

    primeSDK_flags_getString: function (keyUTF8, defaultValueUTF8) {
        const key = UTF8ToString(keyUTF8);
        const defaultValue = UTF8ToString(defaultValueUTF8);
        const value = Module.primeSDK.flags.get(key, defaultValue);
        return Module.allocateString(value);
    },

    primeSDK_flags_hasKey: function (keyUTF8) {
        const key = UTF8ToString(keyUTF8);
        return Module.primeSDK.flags.hasKey(key);
    },

};
mergeInto(LibraryManager.library, primeSDK_flags_library);