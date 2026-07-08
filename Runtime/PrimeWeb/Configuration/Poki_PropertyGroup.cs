using PrimeGames.SDK.Common;
using System;
using UnityEngine;

namespace PrimeGames.SDK.PrimeWeb {

    [Serializable]
    public class Poki_PropertyGroup : PropertyGroup {

        public override string Name => "Poki";

        [SerializeField] public float interstitialInterval = 0;

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
