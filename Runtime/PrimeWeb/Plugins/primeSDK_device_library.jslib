const primeSDK_device_library = {

    primeSDK_device_isMobile: function () {
        return Module.primeSDK.device.isMobile;
    },

    primeSDK_device_systemType: function () {
        const systemType = Module.primeSDK.device.systemType;
        return Module.allocateString(systemType);
    },

    primeSDK_device_openUrl: function (url_ptr) {
        const url = UTF8ToString(url_ptr);
        Module.primeSDK.device.openUrl(url);
    }

};
mergeInto(LibraryManager.library, primeSDK_device_library);