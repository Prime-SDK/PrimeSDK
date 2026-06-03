using PrimeGames.SDK.SourceGenerator;
using PrimeGames.SDK.Common;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class GameplayReporter_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<GameplayReporter_Features> { }

        public GameplayReporter_Features() {
            SetInfo("Gameplay Reporter", nameof(IAnalytics), nameof(GameplayReporterProvider));

            CreateButton(nameof(IGameplayReporter.GameIsReady), () => {
                PrimeSDK.Analytics.GameIsReady();
            });

            CreateButton(nameof(IGameplayReporter.GameplayStart), () => {
                PrimeSDK.Analytics.GameplayStart();
            });

            CreateButton(nameof(IGameplayReporter.GameplayStop), () => {
                PrimeSDK.Analytics.GameplayStop();
            });
        }

    }

}