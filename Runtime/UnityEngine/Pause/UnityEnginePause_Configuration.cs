using PrimeGames.SDK.Common;

namespace PrimeGames.SDK.UnityEngine {

    [ProviderConfiguration(typeof(UnityEnginePause))]
    public class UnityEnginePause_Configuration : PropertyGroup {

        public override string Name => nameof(UnityEnginePause);

    }

}
