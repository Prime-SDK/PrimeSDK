using PrimeGames.SDK.Common;
using System;
using UnityEngine;

namespace PrimeGames.SDK.PrimeWeb {

    [Serializable]
    public class LaggedNew_PropertyGroup : PropertyGroup {

        public override string Name => "Lagged";

        [SerializeField] public bool useLegacySdk = false;
        [SerializeField] public string devId = "";
        [SerializeField] public string publisherId = "";
        [SerializeField] public string gameKey = "";
        [SerializeField] public float interstitialInterval = 0;

        public override StringProperty[] GetStringProperties() {
            if (useLegacySdk) {
                return new StringProperty[] {
                    new("Dev Id", () => devId, (value) => { devId = value; }),
                    new("Publisher Id", () => publisherId, (value) => { publisherId = value; })
                };
            }
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
