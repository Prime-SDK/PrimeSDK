const primeSDK_analytics_library = {

    primeSDK_analytics_gameIsReady: function () {
        Module.primeSDK.analytics.gameIsReady();
    },

    primeSDK_analytics_gameplayStart: function (level) {
        Module.primeSDK.analytics.gameplayStart(level);
    },

    primeSDK_analytics_gameplayRestart: function (level) {
        Module.primeSDK.analytics.gameplayRestart(level);
    },

    primeSDK_analytics_gameplayStop: function (level) {
        Module.primeSDK.analytics.gameplayStop(level);
    },

};
mergeInto(LibraryManager.library, primeSDK_analytics_library);