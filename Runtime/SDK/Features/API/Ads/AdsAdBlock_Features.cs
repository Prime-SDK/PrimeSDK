using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class AdsAdBlock_Features : FeaturesContainer {

        private const string RewardedPlacementId = "adblock-debug";

        private string lastEvent = "None";
        private string lastCloseResult = "None";
        private int interstitialAdBlockCallbacks;
        private int rewardedAdBlockCallbacks;

        public new class UxmlFactory : UxmlFactory<AdsAdBlock_Features> { }

        public AdsAdBlock_Features() {
            SetInfo("Ads AdBlock", nameof(IAds), nameof(AdsProvider));

            CreateString("Last Event", () => lastEvent);
            CreateString("Last Close", () => lastCloseResult);
            CreateString("Interstitial AdBlock", () => interstitialAdBlockCallbacks.ToString());
            CreateString("Rewarded AdBlock", () => rewardedAdBlockCallbacks.ToString());

            CreateButton("Invoke Interstitial", () => {
                lastEvent = "Interstitial invoked";
                lastCloseResult = "Pending";
                PrimeSDK.Ads.InvokeInterstitial(
                    onOpen: () => {
                        lastEvent = "Interstitial opened";
                    },
                    onClose: (isSuccess) => {
                        lastEvent = "Interstitial closed";
                        lastCloseResult = isSuccess.ToString();
                    },
                    onAdBlockDetected: () => {
                        interstitialAdBlockCallbacks++;
                        lastEvent = "Interstitial AdBlock detected";
                        Debug.Log(lastEvent);
                    }
                );
            });

            CreateButton("Invoke Rewarded", () => {
                lastEvent = "Rewarded invoked";
                lastCloseResult = "Pending";
                PrimeSDK.Ads.InvokeRewarded(
                    onOpen: () => {
                        lastEvent = "Rewarded opened";
                    },
                    onClose: (isSuccess) => {
                        lastEvent = "Rewarded closed";
                        lastCloseResult = isSuccess.ToString();
                    },
                    rewardTag: RewardedPlacementId,
                    onAdBlockDetected: () => {
                        rewardedAdBlockCallbacks++;
                        lastEvent = "Rewarded AdBlock detected";
                        Debug.Log(lastEvent);
                    }
                );
            });
        }

    }

}
