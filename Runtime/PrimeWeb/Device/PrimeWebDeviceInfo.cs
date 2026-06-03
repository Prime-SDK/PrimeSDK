using PrimeGames.SDK.Common;
using System.Runtime.InteropServices;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IDeviceInfo))]
    public class PrimeWebDeviceInfo : CommonDeviceInfo {

        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_device_isMobile();
        [DllImport(Naming.InternalDll)] private static extern string primeSDK_device_systemType();

        protected override bool GetIsMobileImpl() {
            return primeSDK_device_isMobile();
        }

        protected override SystemType GetSystemTypeImpl() {
            string systemType = primeSDK_device_systemType();
            return systemType.ToEnumOrDefault<SystemType>(SystemType.Unknown);
        }

    }

}