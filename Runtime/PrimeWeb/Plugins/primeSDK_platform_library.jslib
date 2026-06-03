const primeSDK_platform_library = {

    primeSDK_platform_current: function () {
        const platform = Module.primeSDK.platform.current.toString();
        return Module.allocateString(platform);
    },

    primeSDK_platform_appId: function () {
        const appId = Module.primeSDK.platform.appId.toString();
        return Module.allocateString(appId);
    },

    primeSDK_platform_shareGame: function (messageText_utf8) {
        const messageText = UTF8ToString(messageText_utf8);
        Module.primeSDK.platform.shareGame(messageText);
    },

    primeSDK_platform_rateGame: function () {
        Module.primeSDK.platform.rateGame();
    }

};
mergeInto(LibraryManager.library, primeSDK_platform_library);