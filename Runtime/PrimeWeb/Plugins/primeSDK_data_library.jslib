const primeSDK_data_library = {

    primeSDK_data_getBool: function (keyUTF8, defaultValue) {
        const key = UTF8ToString(keyUTF8);
        return Module.primeSDK.data.get(key, defaultValue);
    },

    primeSDK_data_setBool: function (keyUTF8, value, important) {
        const key = UTF8ToString(keyUTF8);
        Module.primeSDK.data.set(key, value, important);
    },

    primeSDK_data_getInt: function (keyUTF8, defaultValue) {
        const key = UTF8ToString(keyUTF8);
        return Module.primeSDK.data.get(key, defaultValue);
    },

    primeSDK_data_setInt: function (keyUTF8, value, important) {
        const key = UTF8ToString(keyUTF8);
        Module.primeSDK.data.set(key, value, important);
    },

    primeSDK_data_getFloat: function (keyUTF8, defaultValue) {
        const key = UTF8ToString(keyUTF8);
        return Module.primeSDK.data.get(key, defaultValue);
    },

    primeSDK_data_setFloat: function (keyUTF8, value, important) {
        const key = UTF8ToString(keyUTF8);
        Module.primeSDK.data.set(key, value, important);
    },

    primeSDK_data_getString: function (keyUTF8, defaultValueUTF8) {
        const key = UTF8ToString(keyUTF8);
        const defaultValue = UTF8ToString(defaultValueUTF8);
        const value = Module.primeSDK.data.get(key, defaultValue);
        return Module.allocateString(value);
    },


    primeSDK_data_setString: function (keyUTF8, valueUTF8, important) {
        const key = UTF8ToString(keyUTF8);
        const value = UTF8ToString(valueUTF8);
        Module.primeSDK.data.set(key, value, important);
    },


    primeSDK_data_save: function () {
        Module.primeSDK.data.save();
    },

    primeSDK_data_hasKey: function (keyUTF8) {
        const key = UTF8ToString(keyUTF8);
        return Module.primeSDK.data.hasKey(key);
    },

    primeSDK_data_deleteKey: function (keyUTF8) {
        const key = UTF8ToString(keyUTF8);
        Module.primeSDK.data.deleteKey(key);
    },

    primeSDK_data_deleteAll: function () {
        Module.primeSDK.data.deleteAll();
    },

};
mergeInto(LibraryManager.library, primeSDK_data_library);