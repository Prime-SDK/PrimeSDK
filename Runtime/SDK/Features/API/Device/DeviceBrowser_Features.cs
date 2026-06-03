using PrimeGames.SDK.SourceGenerator;
using PrimeGames.SDK.Common;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class DeviceBrowser_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<DeviceBrowser_Features> { }

        public DeviceBrowser_Features() {
            SetInfo("Device Browser", nameof(IDevice), nameof(DeviceBrowserProvider));

            CreateButton(nameof(IDeviceBrowser.OpenUrl), () => {
                PrimeSDK.Device.OpenUrl("https://google.com/");
            });
        }

    }

}