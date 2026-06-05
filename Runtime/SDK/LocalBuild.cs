using PrimeGames.SDK.Common;
using PrimeGames.SDK.Fallback;
using PrimeGames.SDK.System;
using PrimeGames.SDK.UnityEngine;

namespace PrimeGames.SDK {

    [Configuration]
    public class LocalBuild : Configuration {

        public override string Name { get; } = nameof(LocalBuild);
        public override string Description { get; } = "Use it for Aeroflot and any other tasks where a local build is needed.";
        public override string IconName { get; } = nameof(LocalBuild);
        public override bool ReadOnly { get; } = false;
        public override string[] DefaultOverrideModules { get; } = new[] {
            "IAchievements",
            "IAnalytics",
            "IData",
            "IDevice",
            "ILanguage",
            "IPause",
            "IPlayer"
        };
        public override string[] DefaultVisibleModules { get; } = new[] {
            "IAchievements",
            "IAnalytics",
            "IData",
            "IDevice",
            "ILanguage",
            "IPause",
            "IPlayer"
        };

        public override string AchievementsProviderName { get; } = nameof(FallbackAchievements);
        public override string AdsProviderName { get; } = nameof(FallbackAds);
        public override string EventsReporterProviderName { get; } = nameof(FallbackEventsReporter);
        public override string GameplayReporterProviderName { get; } = nameof(FallbackGameplayReporter);
        public override string AddressablesProviderName { get; } = nameof(UnityEngineAddressables);
        public override string AssetBundlesProviderName { get; } = nameof(UnityEngineAssetBundles);
        public override string StreamingAssetsProviderName { get; } = nameof(UnityEngineStreamingAssets);
        public override string AudioProviderName { get; } = nameof(UnityEngineAudio);
        public override string BootstrapProviderName { get; } = nameof(FallbackBootstrap);
        public override string DataProviderName { get; } = nameof(UnityEngineData);
        public override string DeviceBrowserProviderName { get; } = nameof(UnityEngineDeviceBrowser);
        public override string DeviceCursorProviderName { get; } = nameof(UnityEngineDeviceCursor);
        public override string DeviceInfoProviderName { get; } = nameof(UnityEngineDeviceInfo);
        public override string FlagsProviderName { get; } = nameof(FallbackFlags);
        public override string LanguageInfoProviderName { get; } = nameof(UnityEngineLanguageInfo);
        public override string PauseProviderName { get; } = nameof(UnityEnginePause);
        public override string PaymentsProviderName { get; } = nameof(FallbackPayments);
        public override string PlatformInfoProviderName { get; } = nameof(FallbackPlatformInfo);
        public override string PlatformInteractionsProviderName { get; } = nameof(FallbackPlatformInteractions);
        public override string PlayerAccountProviderName { get; } = nameof(FallbackPlayerAccount);
        public override string DateTimeProviderName { get; } = nameof(SystemDateTime);
        public override string TimeScaleProviderName { get; } = nameof(UnityEngineTimeScale);

    }

}
