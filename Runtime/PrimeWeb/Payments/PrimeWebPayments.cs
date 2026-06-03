using AOT;
using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IPayments))]
    public class PrimeWebPayments : CommonPayments {

        [DllImport(Naming.InternalDll)] private static extern float primeSDK_payments_getProductPrice(string productTag);
        [DllImport(Naming.InternalDll)] private static extern string primeSDK_payments_getProductCurrency(string productTag);
        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_payments_isProductPurchased(string productTag);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_payments_purchase(int senderId, string productTag, DelegateVoid onSuccess, DelegateVoid onError);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_payments_updatePurchases(int senderId, DelegateString onSuccess);

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnPurchaseSuccess(int senderId) {
            try {
                if (purchaseCallbacks.TryGetValue(senderId, out PurchaseInfo info)) {
                    info.onSuccess?.Invoke();
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebPayments), nameof(OnPurchaseSuccess), exception);
            }
        }

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnPurchaseError(int senderId) {
            try {
                if (purchaseCallbacks.TryGetValue(senderId, out PurchaseInfo info)) {
                    info.onError?.Invoke();
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebPayments), nameof(OnPurchaseError), exception);
            }
        }

        [MonoPInvokeCallback(typeof(DelegateString))]
        private static void OnUpdatePurchases(int senderId, string json) {
            Logger.CreateText(nameof(PrimeWebPayments), nameof(OnUpdatePurchases), Naming.Quote(json));
            try {
                if (getPurchasesCallbacks.TryGetValue(senderId, out var info)) {
                    PurchasesArray purchasesArray = JsonUtility.FromJson<PurchasesArray>(json);
                    Logger.CreateText(nameof(PrimeWebPayments), nameof(OnUpdatePurchases), Naming.Quote(string.Join(", ", purchasesArray.Purchases)));
                    info.onSuccess?.Invoke(purchasesArray.Purchases);
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebPayments), nameof(OnUpdatePurchases), exception);
            }
        }

        private class PurchaseInfo {
            public string productTag;
            public Action onSuccess;
            public Action onError;
        }

        private class GetPurchasesInfo {
            public Action<string[]> onSuccess;
        }

        private static readonly Dictionary<int, PurchaseInfo> purchaseCallbacks = new();
        private static readonly Dictionary<int, GetPurchasesInfo> getPurchasesCallbacks = new();

        public PrimeWebPayments(IData data) : base(data) {
            SetInitialized();
        }

        protected override ProductData GetProductDataImpl(string productTag) {
            float price = primeSDK_payments_getProductPrice(productTag);
            string currency = primeSDK_payments_getProductCurrency(productTag);
            return new ProductData(productTag, price, currency);
        }

        protected override bool IsAlreadyPurchasedImpl(string productTag) {
            return primeSDK_payments_isProductPurchased(productTag);
        }

        protected override void PurchaseImpl(string productTag, Action onSuccess, Action onError = null) {
            int senderId = purchaseCallbacks.Count;
            purchaseCallbacks[senderId] = new PurchaseInfo() {
                productTag = productTag,
                onSuccess = () => {
                    SupplyProduct(productTag, onSuccess, true);
                },
                onError = onError
            };
            primeSDK_payments_purchase(senderId, productTag, OnPurchaseSuccess, OnPurchaseError);
        }

        protected override void RestorePurchasesImpl(Action<IRestoreData> onRestoreData) {
            int senderId = getPurchasesCallbacks.Count;
            getPurchasesCallbacks[senderId] = new() {
                onSuccess = (purchases) => {
                    Logger.CreateText(nameof(PrimeWebPayments), nameof(RestorePurchasesImpl), "onSuccess", purchases);
                    Purchases = purchases;
                    IRestoreData restoreData = new RestoreData(this, Purchases);
                    onRestoreData?.Invoke(restoreData);
                }
            };
            primeSDK_payments_updatePurchases(senderId, OnUpdatePurchases);
        }

    }

}