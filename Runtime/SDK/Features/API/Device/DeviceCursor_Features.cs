using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class DeviceCursor_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<DeviceCursor_Features> { }

        public DeviceCursor_Features() {
            SetInfo("Device Cursor", nameof(IDevice), nameof(DeviceCursorProvider));

            CreateBoolean(nameof(IDeviceCursor.CursorVisible), () => {
                return PrimeSDK.Device.CursorVisible;
            });
            CreateString(nameof(IDeviceCursor.CursorLock), () => {
                return PrimeSDK.Device.CursorLock.ToString();
            });
            CreateButton("Show Cursor", () => {
                PrimeSDK.Device.CursorVisible = true;
            });
            CreateButton("Hide Cursor", () => {
                PrimeSDK.Device.CursorVisible = false;
            });
        }

    }

}