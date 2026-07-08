using PrimeGames.SDK.Common;
using System;
using UnityEngine;

namespace PrimeGames.SDK.PrimeWeb {

    [Serializable]
    public class Y8New_PropertyGroup : PropertyGroup {

        public override string Name => "Y8";

        [SerializeField] public string appId = "";
        [SerializeField] public string gameId = "";
        [SerializeField] public bool useNewSdk = true;
        [SerializeField] public float interstitialInterval = 0;
        [field: SerializeField] public AdSenseSettings adSense = new();

        public override StringProperty[] GetStringProperties() {
            if (!useNewSdk) {
                return new StringProperty[] {
                    new("Data Ad Client", () => adSense.dataAdClient, (value) => { adSense.dataAdClient = value; }),
                    new("Data Ad Channel", () => adSense.dataAdChannel, (value) => { adSense.dataAdChannel = value; })
                };
            }
            return new StringProperty[] {
                new("App Id", () => appId, (value) => { appId = value; }),
                new("Game Id", () => gameId, (value) => { gameId = value; })
            };
        }

        public override BoolProperty[] GetBoolProperties() {
            if (!useNewSdk) {
                return new BoolProperty[] {
                    new("Data Ad Break Test", () => adSense.dataAdBreakTest, (value) => { adSense.dataAdBreakTest = value; })
                };
            }
            return Array.Empty<BoolProperty>();
        }

        public override FloatProperty[] GetFloatProperties() {
            if (!useNewSdk) {
                return new FloatProperty[] {
                    new(
                        "Interstitial Interval (s)",
                        () => adSense.interstitialInterval,
                        (value) => { adSense.interstitialInterval = value; }
                    )
                };
            }
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
