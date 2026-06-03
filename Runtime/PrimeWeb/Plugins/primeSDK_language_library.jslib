const primeSDK_language_library = {

    primeSDK_language_current: function () {
        return Module.primeSDK.language.current;
    },

};
mergeInto(LibraryManager.library, primeSDK_language_library);