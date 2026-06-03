using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class DeviceInfo_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<DeviceInfo_Features> { }

        public DeviceInfo_Features() {
            SetInfo("Device Info", nameof(IDevice), nameof(DeviceInfoProvider));

            CreateBoolean(nameof(IDeviceInfo.IsMobile), () => {
                return PrimeSDK.Device.IsMobile;
            });
            CreateString(nameof(IDeviceInfo.SystemType), () => {
                return PrimeSDK.Device.SystemType.ToString();
            });
        }

    }

}