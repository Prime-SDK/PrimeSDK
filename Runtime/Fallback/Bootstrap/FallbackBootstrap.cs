using PrimeGames.SDK.Common;

namespace PrimeGames.SDK.Fallback {

    [Provider(typeof(IBootstrap))]
    public class FallbackBootstrap : CommonBootstrap {

        public FallbackBootstrap() {
            SetInitialized();
        }

    }

}