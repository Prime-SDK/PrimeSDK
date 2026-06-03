using AOT;
using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IAds))]
    public class PrimeWebAds : CommonAds {

        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_ads_isBannerReady();
        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_ads_isBannerVisible();
        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_ads_isBannerAvailable();
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_ads_invokeBanner();
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_ads_refreshBanner();
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_ads_disableBanner();

        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_ads_isInterstitialReady();
        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_ads_isInterstitialVisible();
        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_ads_isInterstitialAvailable();
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_ads_invokeInterstitial(int senderId, DelegateVoid onOpen, DelegateInt onClose);

        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_ads_isRewardedReady();
        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_ads_isRewardedVisible();
        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_ads_isRewardedAvailable();
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_ads_invokeRewarded(int senderId, DelegateVoid onOpen, DelegateInt onClose);

        private record InvokeInterstitialInfo {
            public Action onOpen;
            public Action<bool> onClose;
        }

        public record InvokeRewardedInfo {
            public string rewardTag;
            public Action onOpen;
            public Action<bool> onClose;
        }

        private static readonly Dictionary<int, InvokeInterstitialInfo> invokeInterstitialInfo = new();
        private static readonly Dictionary<int, InvokeRewardedInfo> invokeRewardedInfo = new();

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnInterstitialOpen(int senderId) {
            try {
                if (invokeInterstitialInfo.TryGetValue(senderId, out InvokeInterstitialInfo info)) {
                    info.onOpen?.Invoke();
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebAds), nameof(OnInterstitialOpen), exception);
            }
        }

        [MonoPInvokeCallback(typeof(DelegateInt))]
        private static void OnInterstitialClose(int senderId, int isSuccess) {
            try {
                if (invokeInterstitialInfo.TryGetValue(senderId, out InvokeInterstitialInfo info)) {
                    info.onClose?.Invoke(isSuccess == 1);
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebAds), nameof(OnInterstitialClose), exception);
            }
        }

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnRewardedOpen(int senderId) {
            try {
                if (invokeRewardedInfo.TryGetValue(senderId, out InvokeRewardedInfo info)) {
                    info.onOpen?.Invoke();
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebAds), nameof(OnRewardedOpen), exception);
            }
        }

        [MonoPInvokeCallback(typeof(DelegateInt))]
        private static void OnRewardedClose(int senderId, int isSuccess) {
            try {
                if (invokeRewardedInfo.TryGetValue(senderId, out InvokeRewardedInfo info)) {
                    info.onClose?.Invoke(isSuccess == 1);
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebAds), nameof(OnRewardedClose), exception);
            }
        }

        public PrimeWebAds(IEventAggregator eventAggregator) : base(eventAggregator) {
            SetInitialized();
        }

        public override bool IsBannerReady {
            get => primeSDK_ads_isBannerReady();
            protected set { }
        }

        public override bool IsBannerVisible {
            get => primeSDK_ads_isBannerVisible();
            protected set { }
        }

        public override bool IsBannerAvailable {
            get => primeSDK_ads_isBannerAvailable();
        }

        protected override void InvokeBannerImpl() {
            primeSDK_ads_invokeBanner();
        }

        protected override void RefreshBannerImpl() {
            primeSDK_ads_refreshBanner();
        }

        protected override void DisableBannerImpl() {
            primeSDK_ads_disableBanner();
        }

        public override bool IsInterstitialReady {
            get => primeSDK_ads_isInterstitialReady();
            protected set { }
        }

        public override bool IsInterstitialVisible {
            get => primeSDK_ads_isInterstitialVisible();
            protected set { }
        }

        public override bool IsInterstitialAvailable {
            get => primeSDK_ads_isInterstitialAvailable();
        }

        protected override void InvokeInterstitialImpl(InterstitialParameters parameters, Action onOpen, Action<bool> onClose) {
            int senderId = invokeInterstitialInfo.Count;
            invokeInterstitialInfo[senderId] = new InvokeInterstitialInfo() {
                onOpen = onOpen,
                onClose = onClose
            };
            primeSDK_ads_invokeInterstitial(senderId, OnInterstitialOpen, OnInterstitialClose);
        }

        public override bool IsRewardedReady {
            get => primeSDK_ads_isRewardedReady();
            protected set { }
        }

        public override bool IsRewardedVisible {
            get => primeSDK_ads_isRewardedVisible();
            protected set { }
        }

        public override bool IsRewardedAvailable {
            get => primeSDK_ads_isRewardedAvailable();
        }

        protected override void InvokeRewardedImpl(RewardedParameters parameters, Action onOpen, Action<bool> onClose) {
            int senderId = invokeRewardedInfo.Count;
            invokeRewardedInfo[senderId] = new InvokeRewardedInfo() {
                rewardTag = parameters.PlacementId,
                onOpen = onOpen,
                onClose = onClose
            };
            primeSDK_ads_invokeRewarded(senderId, OnRewardedOpen, OnRewardedClose);
        }

    }

}