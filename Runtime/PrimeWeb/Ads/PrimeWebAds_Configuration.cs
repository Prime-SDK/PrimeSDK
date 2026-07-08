using PrimeGames.SDK.Common;
using UnityEngine;

namespace PrimeGames.SDK.PrimeWeb {

    [ProviderConfiguration(typeof(PrimeWebAds))]
    public class PrimeWebAds_Configuration : PropertyGroup {

        public override string Name => nameof(PrimeWebAds);

        [field: SerializeField] public bool AdBlockDetectionEnabled { get; private set; } = true;

        public override BoolProperty[] GetBoolProperties() {
            return new BoolProperty[] {
                new(
                    "AdBlock Detection Enabled",
                    getter: () => AdBlockDetectionEnabled,
                    setter: (value) => { AdBlockDetectionEnabled = value; }
                )
            };
        }

    }

}
