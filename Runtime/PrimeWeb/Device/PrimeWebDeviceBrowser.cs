using PrimeGames.SDK.Common;
using System.Runtime.InteropServices;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IDeviceBrowser))]
    public class PrimeWebDeviceBrowser : CommonDeviceBrowser {

        [DllImport(Naming.InternalDll)] private static extern void primeSDK_device_openUrl(string url);

        protected override void OpenUrlImpl(string url) {
            primeSDK_device_openUrl(url);
        }

    }

}