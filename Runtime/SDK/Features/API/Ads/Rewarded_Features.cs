using System;
using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class Rewarded_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<Rewarded_Features> { }

        public Rewarded_Features() {
            SetInfo("Rewarded", nameof(IAds), nameof(AdsProvider));

            CreateBoolean(nameof(IRewarded.IsRewardedReady), () => {
                return PrimeSDK.Ads.IsRewardedReady;
            });
            CreateBoolean(nameof(IRewarded.IsRewardedVisible), () => {
                return PrimeSDK.Ads.IsRewardedVisible;
            });
            CreateBoolean(nameof(IRewarded.IsRewardedAvailable), () => {
                return PrimeSDK.Ads.IsRewardedAvailable;
            });

            CreateString(nameof(IRewarded.GetLastRewardedSuccess), () => {
                DateTime? dateTime = PrimeSDK.Ads.GetLastRewardedSuccess();
                if (dateTime == null) {
                    return "null";
                }
                return dateTime.HasValue ? dateTime.Value.ToString("HH:mm:ss") : "null";
            });

            CreateButton(nameof(IRewarded.InvokeRewarded), () => {
                // TODO: log callbacks.
                PrimeSDK.Ads.InvokeRewarded(
                    onOpen: () => { },
                    onClose: (success) => { }
                );
            });
        }

    }

}