using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class PlatformInfo_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<PlatformInfo_Features> { }

        public PlatformInfo_Features() {
            SetInfo("Platform Info", nameof(IPlatform), nameof(PlatformInfoProvider));

            CreateString(nameof(IPlatform.Current), () => {
                return PrimeSDK.Platform.Current.ToString();
            });

            CreateString(nameof(IPlatform.Deployment), () => {
                return PrimeSDK.Platform.Deployment.ToString();
            });

            CreateString(nameof(IPlatform.AppId), () => {
                return PrimeSDK.Platform.AppId;
            });

            CreateButton(nameof(IPlatform.ShareGame), () => {
                PrimeSDK.Platform.ShareGame("this is example of message text");
            });

            CreateButton(nameof(IPlatform.RateGame), () => {
                PrimeSDK.Platform.RateGame();
            });
        }

    }

}