using PrimeGames.SDK.Common;
using System;
using UnityEngine;

namespace PrimeGames.SDK.PrimeWeb {

    [Serializable]
    public class Y8New_PropertyGroup : PropertyGroup {

        public override string Name => "Y8 New";

        [SerializeField] public string appId = "";
        [SerializeField] public string gameId = "";
        [SerializeField] public bool useNewSdk = false;
        [SerializeField] public float interstitialInterval = 0;

        public override StringProperty[] GetStringProperties() {
            return new StringProperty[] {
                new("App Id", () => appId, (value) => { appId = value; }),
                new("Game Id", () => gameId, (value) => { gameId = value; })
            };
        }

        public override BoolProperty[] GetBoolProperties() {
            return new BoolProperty[] {
                new("Use New SDK", () => useNewSdk, (value) => { useNewSdk = value; })
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
