const primeSDK_pause_library = {

    primeSDK_pause_isPaused: function () {
        return Module.PrimeSDK.pause.isPaused;
    },

    primeSDK_pause_onPauseChange: function (onPauseChange_ptr) {
        const onPauseChange = (isPaused) => {
            Module.invokeMonoPCallback(-1, onPauseChange_ptr, isPaused);
        };
        Module.PrimeSDK.pause.onPauseChange.add(onPauseChange);
    }

};
mergeInto(LibraryManager.library, primeSDK_pause_library);