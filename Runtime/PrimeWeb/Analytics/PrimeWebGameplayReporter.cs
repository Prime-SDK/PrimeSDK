using PrimeGames.SDK.Common;
using System.Runtime.InteropServices;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IGameplayReporter))]
    public class PrimeWebGameplayReporter : CommonGameplayReporter {

        [DllImport(Naming.InternalDll)] private static extern void primeSDK_analytics_gameIsReady();
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_analytics_gameplayStart(int level);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_analytics_gameplayRestart(int level);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_analytics_gameplayStop(int level);

        public PrimeWebGameplayReporter() {
            SetInitialized();
        }

        protected override void GameIsReadyImpl() {
            primeSDK_analytics_gameIsReady();
        }

        protected override void GameplayStartImpl(int level = 0) {
            primeSDK_analytics_gameplayStart(level);
        }

        protected override void GameplayRestartImpl(int level = 0) {
            primeSDK_analytics_gameplayRestart(level);
        }

        protected override void GameplayStopImpl(int level = 0) {
            primeSDK_analytics_gameplayStop(level);
        }

    }

}