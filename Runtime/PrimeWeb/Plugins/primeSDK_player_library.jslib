const primeSDK_player_library = {

    primeSDK_player_displayName: function () {
        const displayName = Module.primeSDK.player.displayName;
        return Module.allocateString(displayName);
    },

    primeSDK_player_firstName: function () {
        const firstName = Module.primeSDK.player.firstName;
        return Module.allocateString(firstName);
    },

    primeSDK_player_lastName: function () {
        const lastName = Module.primeSDK.player.lastName;
        return Module.allocateString(lastName);
    },

    primeSDK_player_username: function () {
        const username = Module.primeSDK.player.username;
        return Module.allocateString(username);
    },

    primeSDK_player_uniqueId: function () {
        const uniqueId = Module.primeSDK.player.uniqueId;
        return Module.allocateString(uniqueId);
    },

    primeSDK_player_isLoggedIn: function () {
        return Module.primeSDK.player.isLoggedIn;
    },

    primeSDK_player_invokeLogin: function (senderId, onSuccessPtr, onErrorPtr) {
        const onSuccess = () => {
            Module.invokeMonoPCallback(senderId, onSuccessPtr);
        };
        const onError = () => {
            Module.invokeMonoPCallback(senderId, onErrorPtr);
        };
        Module.primeSDK.player.invokeLogin(onSuccess, onError);
    },

};
mergeInto(LibraryManager.library, primeSDK_player_library);