using PrimeGames.SDK.Common;

namespace PrimeGames.SDK.PrimeWeb {

    [ProviderConfiguration(typeof(ShowPause))]
    public class ShowPause_Configuration : PropertyGroup {

        public override string Name => nameof(ShowPause);

    }

}
