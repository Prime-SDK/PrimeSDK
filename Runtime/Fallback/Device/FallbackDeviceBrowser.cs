using PrimeGames.SDK.Common;

namespace PrimeGames.SDK.Fallback {

    [Provider(typeof(IDeviceBrowser))]
    public class FallbackDeviceBrowser : CommonDeviceBrowser {

        protected override void OpenUrlImpl(string url) {
            Logger.NotImplementedWarning(this, nameof(OpenUrlImpl));
        }

    }

}