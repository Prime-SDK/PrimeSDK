using PrimeGames.SDK.Common;
using UnityEngine.Scripting;

namespace PrimeGames.SDK {

    [Root, Preserve]
    public partial class PrimeSDK {

        public const string Version = "5.1.53";

        private PrimeSDK(MainFactory factory) {
            Logger.CreateText(this, $"Starting up. Version {Version}");

            eventDispatcher = factory.CreateEventDispatcher();
            eventAggregator = factory.CreateEventAggregator();

            preferences = factory.CreatePreferencesReader();
            data = factory.CreateData();

            achievements = factory.CreateAchievements();
            ads = factory.CreateAds();
            analytics = factory.CreateAnalytics();
            assets = factory.CreateAssets();
            audio = factory.CreateAudio();
            bootstrap = factory.CreateBootstrap();
            device = factory.CreateDevice();
            flags = factory.CreateFlags();
            language = factory.CreateLanguage();
            pause = factory.CreatePause();
            payments = factory.CreatePayments();
            platform = factory.CreatePlatform();
            player = factory.CreatePlayer();
            time = factory.CreateTime();

            Logger.CreateText(this, "Hello! Don't forget to WaitForProviders before using my API :)");
        }

    }

}
