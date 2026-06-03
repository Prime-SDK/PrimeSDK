using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class Flags_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<Flags_Features> { }

        public Flags_Features() {
            SetInfo("Flags", nameof(IFlags), nameof(FlagsProvider));

            CreateButton(nameof(IFlags.GetBool), () => {
                bool customBool = PrimeSDK.Flags.GetBool("customBool");
                Debug.Log($"customBool: {customBool}");
            });
            CreateButton(nameof(IFlags.GetInt), () => {
                int customInt = PrimeSDK.Flags.GetInt("customInt");
                Debug.Log($"customInt: {customInt}");
            });
            CreateButton(nameof(IFlags.GetFloat), () => {
                float customFloat = PrimeSDK.Flags.GetFloat("customFloat");
                Debug.Log($"customFloat: {customFloat}");
            });
            CreateButton(nameof(IFlags.GetString), () => {
                string customString = PrimeSDK.Flags.GetString("customString");
                Debug.Log($"customString: {customString}");
            });
            CreateButton(nameof(IFlags.HasKey), () => {
                bool hasKey = PrimeSDK.Flags.HasKey("customBool");
                Debug.Log($"hasKey (customBool): {hasKey}");
            });
        }

    }

}