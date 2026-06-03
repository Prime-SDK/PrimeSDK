using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class BannerFeatures : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<BannerFeatures> { }

        public BannerFeatures() {
            SetInfo("Banner", nameof(IAds), nameof(AdsProvider));

            CreateBoolean(nameof(IBanner.IsBannerReady), () => {
                return PrimeSDK.Ads.IsBannerReady;
            });
            CreateBoolean(nameof(IBanner.IsBannerVisible), () => {
                return PrimeSDK.Ads.IsBannerVisible;
            });
            CreateBoolean(nameof(IBanner.IsBannerAvailable), () => {
                return PrimeSDK.Ads.IsBannerAvailable;
            });

            CreateButton(nameof(IBanner.InvokeBanner), () => {
                PrimeSDK.Ads.InvokeBanner();
            });
            CreateButton(nameof(IBanner.RefreshBanner), () => {
                PrimeSDK.Ads.RefreshBanner();
            });
            CreateButton(nameof(IBanner.DisableBanner), () => {
                PrimeSDK.Ads.DisableBanner();
            });
        }

    }

}