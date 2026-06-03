using PrimeGames.SDK.SourceGenerator;
using PrimeGames.SDK.Common;
using System;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    internal class Interstitial_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<Interstitial_Features> { }

        public Interstitial_Features() {
            SetInfo("Interstitial", nameof(IAds), nameof(AdsProvider));

            CreateBoolean(nameof(IInterstitial.IsInterstitialReady), () => {
                return PrimeSDK.Ads.IsInterstitialReady;
            });
            CreateBoolean(nameof(IInterstitial.IsInterstitialVisible), () => {
                return PrimeSDK.Ads.IsInterstitialVisible;
            });
            CreateBoolean(nameof(IInterstitial.IsInterstitialAvailable), () => {
                return PrimeSDK.Ads.IsInterstitialAvailable;
            });
            CreateString(nameof(IInterstitial.GetLastInterstitialSuccess), () => {
                DateTime? dateTime = PrimeSDK.Ads.GetLastInterstitialSuccess();
                if (dateTime == null) {
                    return "null";
                }
                return dateTime.HasValue ? dateTime.Value.ToString("HH:mm:ss") : "null";
            });
            CreateButton(nameof(IInterstitial.InvokeInterstitial), () => {
                PrimeSDK.Ads.InvokeInterstitial(
                    onOpen: () => { },
                    onClose: (isSuccess) => { }
                );
            });
        }

    }

}