using PrimeGames.SDK.Common;
using System;

namespace PrimeGames.SDK.PrimeWeb {

    [Configuration]
    public class PrimeWebConfiguration : Configuration {

        public override string Name { get; } = nameof(PrimeWeb);
        public override string Description { get; } = "Stable crossplatform Web support from PrimeGames";
        public override string IconName { get; } = "IconPrime";
        public override bool ReadOnly { get; } = false;

        public override Type[] PropertyGroups { get; } = new Type[] {
            typeof(Framework_PropertyGroup),
            typeof(Logger_PropertyGroup),
            typeof(Y8_PropertyGroup),
            typeof(YandexGames_PropertyGroup),
            typeof(MSN_PropertyGroup),
            typeof(Xiaomi_PropertyGroup),
            typeof(Lagged_PropertyGroup),
            typeof(CrazyGames_PropertyGroup),
            typeof(GameDistribution_PropertyGroup)
        };

        public override string AchievementsProviderName { get; } = nameof(PrimeWebAchievements);
        public override string AdsProviderName { get; } = nameof(PrimeWebAds);
        public override string EventsReporterProviderName { get; } = "FallbackEventsReporter";
        public override string GameplayReporterProviderName { get; } = nameof(PrimeWebGameplayReporter);
        public override string AddressablesProviderName { get; } = "UnityEngineAddressables";
        public override string AssetBundlesProviderName { get; } = "UnityEngineAssetBundles";
        public override string StreamingAssetsProviderName { get; } = "UnityEngineStreamingAssets";
        public override string AudioProviderName { get; } = "UnityEngineAudio";
        public override string BootstrapProviderName { get; } = nameof(PrimeWebBootstrap);
        public override string DataProviderName { get; } = nameof(PrimeWebData);
        public override string DeviceBrowserProviderName { get; } = nameof(PrimeWebDeviceBrowser);
        public override string DeviceCursorProviderName { get; } = "UnityEngineDeviceCursor";
        public override string DeviceInfoProviderName { get; } = nameof(PrimeWebDeviceInfo);
        public override string FlagsProviderName { get; } = nameof(PrimeWebFlags);
        public override string LanguageInfoProviderName { get; } = nameof(PrimeWebLanguageInfo);
        public override string PauseProviderName { get; } = nameof(PrimeWebPause);
        public override string PaymentsProviderName { get; } = nameof(PrimeWebPayments);
        public override string PlatformInfoProviderName { get; } = nameof(PrimeWebPlatformInfo);
        public override string PlatformInteractionsProviderName { get; } = nameof(PrimeWebPlatformInteractions);
        public override string PlayerAccountProviderName { get; } = nameof(PrimeWebPlayerAccount);
        public override string DateTimeProviderName { get; } = "SystemDateTime";
        public override string TimeScaleProviderName { get; } = "UnityEngineTimeScale";

    }

}
