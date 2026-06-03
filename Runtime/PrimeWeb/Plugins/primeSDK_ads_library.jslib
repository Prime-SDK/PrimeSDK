const primeSDK_ads_library = {

    primeSDK_ads_isBannerReady: function () {
        return Module.primeSDK.ads.isBannerReady;
    },

    primeSDK_ads_isBannerVisible: function () {
        return Module.primeSDK.ads.isBannerVisible;
    },

    primeSDK_ads_isBannerAvailable: function () {
        return Module.primeSDK.ads.isBannerAvailable;
    },

    primeSDK_ads_invokeBanner: function () {
        Module.primeSDK.ads.invokeBanner();
    },

    primeSDK_ads_refreshBanner: function () {
        Module.primeSDK.ads.refreshBanner();
    },

    primeSDK_ads_disableBanner: function () {
        Module.primeSDK.ads.disableBanner();
    },

    primeSDK_ads_isInterstitialReady: function () {
        return Module.primeSDK.ads.isInterstitialReady;
    },

    primeSDK_ads_isInterstitialVisible: function () {
        return Module.primeSDK.ads.isInterstitialVisible;
    },

    primeSDK_ads_isInterstitialAvailable: function () {
        return Module.primeSDK.ads.isInterstitialAvailable;
    },

    primeSDK_ads_invokeInterstitial: function (senderId, onOpenPtr, onClosePtr) {
        const onOpen = () => {
            Module.invokeMonoPCallback(senderId, onOpenPtr);
        };
        const onClose = (isSuccess) => {
            Module.invokeMonoPCallback(senderId, onClosePtr, isSuccess);
        };
        Module.primeSDK.ads.invokeInterstitial(onOpen, onClose);
    },

    primeSDK_ads_isRewardedReady: function () {
        return Module.primeSDK.ads.isRewardedReady;
    },

    primeSDK_ads_isRewardedVisible: function () {
        return Module.primeSDK.ads.isRewardedVisible;
    },

    primeSDK_ads_isRewardedAvailable: function () {
        return Module.primeSDK.ads.isRewardedAvailable;
    },

    primeSDK_ads_invokeRewarded: function (senderId, onOpenPtr, onClosePtr) {
        const onOpen = () => {
            Module.invokeMonoPCallback(senderId, onOpenPtr);
        };
        const onClose = (isSuccess) => {
            Module.invokeMonoPCallback(senderId, onClosePtr, isSuccess);
        };
        Module.primeSDK.ads.invokeRewarded(onOpen, onClose);
    },

};
mergeInto(LibraryManager.library, primeSDK_ads_library);