using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class Audio_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<Audio_Features> { }

        public Audio_Features() {
            SetInfo("Audio", nameof(IAudio), nameof(AudioProvider));

            CreateBoolean(nameof(IAudio.Pause), () => {
                return PrimeSDK.Audio.Pause;
            });
            CreateString(nameof(IAudio.Volume), () => {
                return PrimeSDK.Audio.Volume.ToString();
            });
        }

    }

}