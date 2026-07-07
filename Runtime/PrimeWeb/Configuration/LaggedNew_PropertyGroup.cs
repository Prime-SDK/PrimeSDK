using PrimeGames.SDK.Common;
using System;
using UnityEngine;

namespace PrimeGames.SDK.PrimeWeb {

    [Serializable]
    public class LaggedNew_PropertyGroup : PropertyGroup {

        public override string Name => "Lagged New";

        [SerializeField] public bool useLegacySdk = false;
        [SerializeField] public string gameKey = "";
        [SerializeField] public float interstitialInterval = 0;

        public override BoolProperty[] GetBoolProperties() {
            return new BoolProperty[] {
                new("Use Legacy SDK", () => useLegacySdk, (value) => { useLegacySdk = value; })
            };
        }

        public override StringProperty[] GetStringProperties() {
            return new StringProperty[] {
                new("Game Key", () => gameKey, (value) => { gameKey = value; })
            };
        }

        public override FloatProperty[] GetFloatProperties() {
            return new FloatProperty[] {
                new(
                    "Interstitial Interval (s)",
                    () => interstitialInterval,
                    (value) => { interstitialInterval = value; }
                )
            };
        }

    }

}
