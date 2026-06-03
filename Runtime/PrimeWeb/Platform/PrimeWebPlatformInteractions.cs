using PrimeGames.SDK.Common;
using System.Runtime.InteropServices;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IPlatformInteractions))]
    public class PrimeWebPlatformInteractions : CommonPlatformInteractions {

        [DllImport(Naming.InternalDll)] private static extern void primeSDK_platform_shareGame(string messageText);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_platform_rateGame();

        protected override void ShareGameImpl(string messageText) {
            primeSDK_platform_shareGame(messageText);
        }

        protected override void RateGameImpl() {
            primeSDK_platform_rateGame();
        }

    }

}