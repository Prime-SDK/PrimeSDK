using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class TimeScale_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<TimeScale_Features> { }

        public TimeScale_Features() {
            SetInfo("Time Scale", nameof(ITime), nameof(TimeScaleProvider));

            CreateString(nameof(ITimeScale.Scale), () => {
                return PrimeSDK.Time.Scale.ToString();
            });
        }

    }

}