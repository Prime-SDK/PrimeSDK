const primeSDK_payments_library = {

    primeSDK_payments_getProductPrice: function (productTagUTF8) {
        const productTag = UTF8ToString(productTagUTF8);
        return Module.primeSDK.payments.getProductPrice(productTag);
    },

    primeSDK_payments_getProductCurrency: function (productTagUTF8) {
        const productTag = UTF8ToString(productTagUTF8);
        const currency = Module.primeSDK.payments.getProductCurrency(productTag);
        return Module.allocateString(currency);
    },

    primeSDK_payments_isProductPurchased: function (productTagUTF8) {
        const productTag = UTF8ToString(productTagUTF8);
        return Module.primeSDK.payments.isProductPurchased(productTag);
    },

    primeSDK_payments_purchase: function (senderId, productTagUTF8, onSuccessPtr, onErrorPtr) {
        const productTag = UTF8ToString(productTagUTF8);
        const onSuccess = () => {
            Module.invokeMonoPCallback(senderId, onSuccessPtr);
        };
        const onError = () => {
            Module.invokeMonoPCallback(senderId, onErrorPtr);
        };
        Module.primeSDK.payments.purchase(productTag, onSuccess, onError);
    },

    primeSDK_payments_updatePurchases: function (senderId, onSuccessPtr) {
        (async () => {
            await Module.primeSDK.payments.updatePurchases();
            const purchasesJson = JSON.stringify({
                Purchases: Module.primeSDK.payments.purchases
            });
            const purchasesJsonPtr = Module.allocateString(purchasesJson);
            Module.invokeMonoPCallback(senderId, onSuccessPtr, purchasesJsonPtr);
        })();
    },

};
mergeInto(LibraryManager.library, primeSDK_payments_library);