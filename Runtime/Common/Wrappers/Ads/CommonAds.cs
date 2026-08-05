using System;
using System.Collections.Generic;

namespace PrimeGames.SDK.Common
{

    [Awaitable, Wrapper]
    public abstract partial class CommonAds : IAds
    {

        protected readonly IEventAggregator eventAggregator;
        protected bool countdownBeforeInterstitialEnabled;
        protected int configuredCountdownSeconds = 2;
        protected bool configuredPauseDuringCountdown = true;
        protected int configuredCountdownAdsIntervalSeconds;
        protected float configuredCountdownStartDelaySeconds;
        protected Func<string> configuredCountdownTitleProvider;
        protected Func<string> configuredCountdownMessageProvider;
        private bool invokingInterstitialAfterCountdown;
        private Action activeCountdownCancel;
        private bool countdownStopped;
        private int countdownRunId;

        public CommonAds(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        // Banner

        public virtual bool IsBannerReady { get; protected set; } = false;
        public virtual bool IsBannerVisible { get; protected set; } = false;
        public virtual bool IsBannerAvailable { get; } = false;

        protected abstract void InvokeBannerImpl();
        protected abstract void RefreshBannerImpl();
        protected abstract void DisableBannerImpl();

        public void InvokeBanner()
        {
            Logger.CreateText(this, nameof(InvokeBanner));
            try
            {
                InvokeBannerImpl();
            }
            catch (Exception exception)
            {
                Logger.CreateError(this, nameof(InvokeBanner), exception);
            }
        }

        public void RefreshBanner()
        {
            Logger.CreateText(this, nameof(RefreshBanner));
            try
            {
                RefreshBannerImpl();
            }
            catch (Exception exception)
            {
                Logger.CreateError(this, nameof(RefreshBanner), exception);
            }
        }

        public void DisableBanner()
        {
            Logger.CreateText(this, nameof(DisableBanner));
            try
            {
                DisableBannerImpl();
            }
            catch (Exception exception)
            {
                Logger.CreateError(this, nameof(DisableBanner), exception);
            }
        }

        // Interstitial

        private DateTime? lastInterstitialSuccess = null;

        public virtual bool IsInterstitialReady { get; protected set; }
        public virtual bool IsInterstitialVisible { get; protected set; }
        public virtual bool IsInterstitialAvailable { get; }

        protected abstract void InvokeInterstitialImpl(InterstitialParameters parameters, Action onOpen, Action<bool> onClose);

        protected virtual void InvokeInterstitialImpl(InterstitialParameters parameters, Action onOpen, Action<bool> onClose, Action onAdBlockDetected)
        {
            InvokeInterstitialImpl(parameters, onOpen, onClose);
        }

        public DateTime? GetLastInterstitialSuccess()
        {
            return lastInterstitialSuccess;
        }

        public void InvokeInterstitial(InterstitialParameters parameters)
        {
            Logger.CreateText(this, nameof(InvokeInterstitial), parameters.PlacementId);
            if (countdownBeforeInterstitialEnabled && !countdownStopped && !invokingInterstitialAfterCountdown)
            {
                InvokeConfiguredCountdown(parameters);
                return;
            }

            try
            {
                // Check availability
                if (!IsInterstitialAvailable)
                {
                    Logger.CreateError(this, "Interstitial not available");
                    parameters.OnClose?.Invoke(false);
                    return;
                }
                if (IsInterstitialVisible)
                {
                    Logger.CreateError(this, "Interstitial already visible");
                    parameters.OnClose?.Invoke(false);
                    return;
                }
                // Hard limit to one show per specified seconds
                // (checking last success from both interstitial and rewarded)
                if (lastInterstitialSuccess.HasValue)
                {
                    TimeSpan interstitialTimeSpan = DateTime.Now - lastInterstitialSuccess.Value;
                    double interstitialSeconds = interstitialTimeSpan.TotalSeconds;
                    if (interstitialSeconds < parameters.AdsIntervalSeconds)
                    {
                        Logger.CreateError(this, "Interstitial frequency capped (interstitial)", parameters.AdsIntervalSeconds - interstitialSeconds, "seconds left");
                        parameters.OnClose?.Invoke(false);
                        return;
                    }
                }
                if (lastRewardedSuccess.HasValue)
                {
                    TimeSpan rewardedTimeSpan = DateTime.Now - lastRewardedSuccess.Value;
                    double rewardedSeconds = rewardedTimeSpan.TotalSeconds;
                    if (rewardedSeconds < parameters.AdsIntervalSeconds)
                    {
                        Logger.CreateError(this, "Interstitial frequency capped (rewarded)", parameters.AdsIntervalSeconds - rewardedSeconds, "seconds left");
                        parameters.OnClose?.Invoke(false);
                        return;
                    }
                }
                // Invoke interstitial
                void onOpenCallback()
                {
                    Logger.CreateText(this, nameof(onOpenCallback));
                    parameters.OnOpen?.Invoke();
                    PauseSourceEvent pauseSourceEvent = new(nameof(InvokeInterstitial), true);
                    eventAggregator.Publish(this, pauseSourceEvent);
                }
                void onCloseCallback(bool isSuccess)
                {
                    Logger.CreateText(this, nameof(onCloseCallback), isSuccess);
                    parameters.OnClose?.Invoke(isSuccess);
                    if (isSuccess)
                    {
                        lastInterstitialSuccess = DateTime.Now;
                    }
                    PauseSourceEvent pauseSourceEvent = new(nameof(InvokeInterstitial), false);
                    eventAggregator.Publish(this, pauseSourceEvent);
                }
                void onAdBlockDetectedCallback()
                {
                    Logger.CreateText(this, nameof(onAdBlockDetectedCallback));
                    parameters.OnAdBlockDetected?.Invoke();
                }
                InvokeInterstitialImpl(parameters, onOpenCallback, onCloseCallback, onAdBlockDetectedCallback);
            }
            catch (Exception exception)
            {
                Logger.CreateError(this, nameof(InvokeInterstitial), exception);
                parameters.OnClose?.Invoke(false);
                PauseSourceEvent pauseSourceEvent = new(nameof(InvokeInterstitial), false);
                eventAggregator.Publish(this, pauseSourceEvent);
            }
        }

        public void InvokeInterstitial(Action onOpen = null, Action<bool> onClose = null, Action onAdBlockDetected = null)
        {
            Logger.CreateText(this, nameof(InvokeInterstitial));
            InterstitialParameters parameters = new()
            {
                OnOpen = onOpen,
                OnClose = onClose,
                OnAdBlockDetected = onAdBlockDetected
            };
            InvokeInterstitial(parameters);
        }

        public void InvokeCountdown(Action onOpen = null, Action<bool> onClose = null)
        {
            countdownStopped = false;
            int runId = ++countdownRunId;
            AutoAdsCountdownView.StartAutoAds(
                configuredCountdownStartDelaySeconds,
                configuredCountdownAdsIntervalSeconds,
                (_, completeAutoAdsCycle) => {
                    if (countdownStopped || runId != countdownRunId)
                    {
                        return false;
                    }
                    string title = configuredCountdownTitleProvider?.Invoke();
                    string messageFormat = configuredCountdownMessageProvider?.Invoke();
                    return InvokeCountdownInternal(
                        onOpen,
                        isSuccess => {
                            onClose?.Invoke(isSuccess);
                            completeAutoAdsCycle?.Invoke(isSuccess);
                        },
                        configuredCountdownSeconds,
                        title,
                        messageFormat,
                        configuredPauseDuringCountdown,
                        0
                    );
                }
            );
        }

        public void StopCountdown()
        {
            Logger.CreateText(this, nameof(StopCountdown));
            countdownStopped = true;
            countdownRunId++;
            Action cancelCountdown = activeCountdownCancel;
            activeCountdownCancel = null;
            AutoAdsCountdownView.StopAutoAds();
            cancelCountdown?.Invoke();
            eventAggregator.Publish(this, new PauseSourceEvent(nameof(InvokeCountdown), false));
        }

        protected bool InvokeCountdownInternal(Action onOpen, Action<bool> onClose, int countdownSeconds, string messageFormat, bool pauseDuringCountdown, int adsIntervalSeconds)
        {
            return InvokeCountdownInternal(onOpen, onClose, countdownSeconds, AutoAdsCountdownView.GetAdvertisementTitle(LanguageType.English), messageFormat, pauseDuringCountdown, adsIntervalSeconds);
        }

        protected bool InvokeCountdownInternal(Action onOpen, Action<bool> onClose, int countdownSeconds, string title, string messageFormat, bool pauseDuringCountdown, int adsIntervalSeconds)
        {
            Logger.CreateText(this, nameof(InvokeCountdownInternal), countdownSeconds);
            if (!CanShowCountdownInterstitial(adsIntervalSeconds, onClose, true)) {
                return false;
            }

            try
            {
                if (pauseDuringCountdown)
                {
                    eventAggregator.Publish(this, new PauseSourceEvent(nameof(InvokeCountdown), true));
                }

                onOpen?.Invoke();
                activeCountdownCancel = () =>
                {
                    if (pauseDuringCountdown)
                    {
                        eventAggregator.Publish(this, new PauseSourceEvent(nameof(InvokeCountdown), false));
                    }
                    onClose?.Invoke(false);
                };
                AutoAdsCountdownView.Show(countdownSeconds, title, messageFormat, () =>
                {
                    activeCountdownCancel = null;
                    InterstitialParameters parameters = new()
                    {
                        AdsIntervalSeconds = adsIntervalSeconds,
                        OnOpen = null,
                        OnClose = isSuccess =>
                        {
                            activeCountdownCancel = null;
                            onClose?.Invoke(isSuccess);
                            if (pauseDuringCountdown)
                            {
                                eventAggregator.Publish(this, new PauseSourceEvent(nameof(InvokeCountdown), false));
                            }
                        }
                    };
                    invokingInterstitialAfterCountdown = true;
                    try {
                        InvokeInterstitial(parameters);
                    }
                    finally {
                        invokingInterstitialAfterCountdown = false;
                    }
                });
                return true;
            }
            catch (Exception exception)
            {
                Logger.CreateError(this, nameof(InvokeCountdownInternal), exception);
                AutoAdsCountdownView.Hide();
                if (pauseDuringCountdown)
                {
                    eventAggregator.Publish(this, new PauseSourceEvent(nameof(InvokeCountdown), false));
                }
                activeCountdownCancel = null;
                onClose?.Invoke(false);
                return false;
            }
        }

        protected void StartAutoCountDownAds(float startDelaySeconds, float intervalSeconds, int countdownSeconds, string messageFormat, bool pauseDuringCountdown)
        {
            countdownStopped = false;
            int runId = ++countdownRunId;
            AutoAdsCountdownView.StartAutoAds(startDelaySeconds, intervalSeconds, (onOpen, onClose) =>
            {
                if (countdownStopped || runId != countdownRunId)
                {
                    return false;
                }
                return InvokeCountdownInternal(onOpen, onClose, countdownSeconds, messageFormat, pauseDuringCountdown, 0);
            });
        }

        protected void StartAutoCountDownAds(float startDelaySeconds, float intervalSeconds, int countdownSeconds, Func<string> messageFormatProvider, bool pauseDuringCountdown)
        {
            countdownStopped = false;
            int runId = ++countdownRunId;
            AutoAdsCountdownView.StartAutoAds(startDelaySeconds, intervalSeconds, (onOpen, onClose) =>
            {
                if (countdownStopped || runId != countdownRunId)
                {
                    return false;
                }
                string messageFormat = messageFormatProvider?.Invoke();
                return InvokeCountdownInternal(onOpen, onClose, countdownSeconds, messageFormat, pauseDuringCountdown, 0);
            });
        }

        protected void StartAutoCountDownAds(float startDelaySeconds, float intervalSeconds, int countdownSeconds, Func<string> titleProvider, Func<string> messageFormatProvider, bool pauseDuringCountdown)
        {
            countdownStopped = false;
            int runId = ++countdownRunId;
            AutoAdsCountdownView.StartAutoAds(startDelaySeconds, intervalSeconds, (onOpen, onClose) =>
            {
                if (countdownStopped || runId != countdownRunId)
                {
                    return false;
                }
                string title = titleProvider?.Invoke();
                string messageFormat = messageFormatProvider?.Invoke();
                return InvokeCountdownInternal(onOpen, onClose, countdownSeconds, title, messageFormat, pauseDuringCountdown, 0);
            });
        }

        protected void ConfigureCountdownBeforeInterstitial(int countdownSeconds, Func<string> titleProvider, Func<string> messageFormatProvider, bool pauseDuringCountdown, int adsIntervalSeconds)
        {
            countdownBeforeInterstitialEnabled = true;
            ConfigureCountdownSettings(countdownSeconds, titleProvider, messageFormatProvider, pauseDuringCountdown, adsIntervalSeconds, configuredCountdownStartDelaySeconds);
        }

        protected void ConfigureCountdownStartDelay(float startDelaySeconds)
        {
            configuredCountdownStartDelaySeconds = Math.Max(0.0f, startDelaySeconds);
        }

        protected void ConfigureCountdownSettings(int countdownSeconds, Func<string> titleProvider, Func<string> messageFormatProvider, bool pauseDuringCountdown, int adsIntervalSeconds)
        {
            ConfigureCountdownSettings(countdownSeconds, titleProvider, messageFormatProvider, pauseDuringCountdown, adsIntervalSeconds, configuredCountdownStartDelaySeconds);
        }

        protected void ConfigureCountdownSettings(int countdownSeconds, Func<string> titleProvider, Func<string> messageFormatProvider, bool pauseDuringCountdown, int adsIntervalSeconds, float startDelaySeconds)
        {
            configuredCountdownSeconds = countdownSeconds;
            configuredCountdownTitleProvider = titleProvider;
            configuredCountdownMessageProvider = messageFormatProvider;
            configuredPauseDuringCountdown = pauseDuringCountdown;
            configuredCountdownAdsIntervalSeconds = adsIntervalSeconds;
            configuredCountdownStartDelaySeconds = Math.Max(0.0f, startDelaySeconds);
        }

        protected static int ToAdsIntervalSeconds(float seconds)
        {
            return Math.Max(0, (int)Math.Ceiling(seconds));
        }

        private void InvokeConfiguredCountdown(InterstitialParameters parameters)
        {
            int runId = countdownRunId;
            void invokeConfiguredCountdown()
            {
                if (countdownStopped || runId != countdownRunId)
                {
                    parameters.OnClose?.Invoke(false);
                    return;
                }

                string title = configuredCountdownTitleProvider?.Invoke();
                string messageFormat = configuredCountdownMessageProvider?.Invoke();
                bool invoked = InvokeCountdownInternal(
                    parameters.OnOpen,
                    isSuccess => {
                        parameters.OnClose?.Invoke(isSuccess);
                    },
                    configuredCountdownSeconds,
                    title,
                    messageFormat,
                    configuredPauseDuringCountdown,
                    parameters.AdsIntervalSeconds > 0 ? parameters.AdsIntervalSeconds : configuredCountdownAdsIntervalSeconds,
                    parameters
                );

                if (!invoked) {
                    parameters.OnClose?.Invoke(false);
                }
            }

            if (configuredCountdownStartDelaySeconds > 0.0f)
            {
                AutoAdsCountdownView.Delay(configuredCountdownStartDelaySeconds, invokeConfiguredCountdown);
                return;
            }

            invokeConfiguredCountdown();
        }

        private bool InvokeCountdownInternal(Action onOpen, Action<bool> onClose, int countdownSeconds, string title, string messageFormat, bool pauseDuringCountdown, int adsIntervalSeconds, InterstitialParameters interstitialParameters)
        {
            Logger.CreateText(this, nameof(InvokeCountdownInternal), countdownSeconds);
            if (!CanShowCountdownInterstitial(adsIntervalSeconds, onClose, false)) {
                return false;
            }

            try
            {
                if (pauseDuringCountdown)
                {
                    eventAggregator.Publish(this, new PauseSourceEvent(nameof(InvokeCountdown), true));
                }

                onOpen?.Invoke();
                activeCountdownCancel = () =>
                {
                    if (pauseDuringCountdown)
                    {
                        eventAggregator.Publish(this, new PauseSourceEvent(nameof(InvokeCountdown), false));
                    }
                    onClose?.Invoke(false);
                };
                AutoAdsCountdownView.Show(countdownSeconds, title, messageFormat, () =>
                {
                    activeCountdownCancel = null;
                    interstitialParameters.AdsIntervalSeconds = adsIntervalSeconds;
                    interstitialParameters.OnOpen = null;
                    interstitialParameters.OnClose = isSuccess =>
                    {
                        activeCountdownCancel = null;
                        onClose?.Invoke(isSuccess);
                        if (pauseDuringCountdown)
                        {
                            eventAggregator.Publish(this, new PauseSourceEvent(nameof(InvokeCountdown), false));
                        }
                    };

                    invokingInterstitialAfterCountdown = true;
                    try {
                        InvokeInterstitial(interstitialParameters);
                    }
                    finally {
                        invokingInterstitialAfterCountdown = false;
                    }
                });
                return true;
            }
            catch (Exception exception)
            {
                Logger.CreateError(this, nameof(InvokeCountdownInternal), exception);
                AutoAdsCountdownView.Hide();
                if (pauseDuringCountdown)
                {
                    eventAggregator.Publish(this, new PauseSourceEvent(nameof(InvokeCountdown), false));
                }
                activeCountdownCancel = null;
                return false;
            }
        }

        private bool CanShowCountdownInterstitial(int adsIntervalSeconds, Action<bool> onClose, bool invokeOnClose)
        {
            if (!IsInterstitialAvailable)
            {
                Logger.CreateError(this, "Countdown interstitial not available");
                if (invokeOnClose) {
                    onClose?.Invoke(false);
                }
                return false;
            }
            if (IsInterstitialVisible || IsRewardedVisible)
            {
                Logger.CreateError(this, "Countdown skipped because ad is already visible");
                if (invokeOnClose) {
                    onClose?.Invoke(false);
                }
                return false;
            }
            if (IsInterstitialFrequencyCapped(adsIntervalSeconds, out string source, out double secondsLeft))
            {
                Logger.CreateError(this, $"Countdown interstitial frequency capped ({source})", secondsLeft, "seconds left");
                if (invokeOnClose) {
                    onClose?.Invoke(false);
                }
                return false;
            }
            return true;
        }

        private bool IsInterstitialFrequencyCapped(int adsIntervalSeconds, out string source, out double secondsLeft)
        {
            source = string.Empty;
            secondsLeft = 0.0;
            if (adsIntervalSeconds <= 0) {
                return false;
            }

            if (lastInterstitialSuccess.HasValue)
            {
                double interstitialSeconds = (DateTime.Now - lastInterstitialSuccess.Value).TotalSeconds;
                if (interstitialSeconds < adsIntervalSeconds)
                {
                    source = "interstitial";
                    secondsLeft = adsIntervalSeconds - interstitialSeconds;
                    return true;
                }
            }
            if (lastRewardedSuccess.HasValue)
            {
                double rewardedSeconds = (DateTime.Now - lastRewardedSuccess.Value).TotalSeconds;
                if (rewardedSeconds < adsIntervalSeconds)
                {
                    source = "rewarded";
                    secondsLeft = adsIntervalSeconds - rewardedSeconds;
                    return true;
                }
            }
            return false;
        }

        // Rewarded

        private DateTime? lastRewardedSuccess = null;
        private Dictionary<string, DateTime?> lastRewardedSuccessByTag = new();

        public virtual bool IsRewardedReady { get; protected set; }
        public virtual bool IsRewardedVisible { get; protected set; }
        public virtual bool IsRewardedAvailable { get; }

        protected abstract void InvokeRewardedImpl(RewardedParameters parameters, Action onOpen, Action<bool> onClose);

        protected virtual void InvokeRewardedImpl(RewardedParameters parameters, Action onOpen, Action<bool> onClose, Action onAdBlockDetected)
        {
            InvokeRewardedImpl(parameters, onOpen, onClose);
        }

        public DateTime? GetLastRewardedSuccess(string rewardTag = null)
        {
            if (string.IsNullOrEmpty(rewardTag))
            {
                return lastRewardedSuccess;
            }
            if (lastRewardedSuccessByTag.TryGetValue(rewardTag, out var dateTime))
            {
                return dateTime;
            }
            return null;
        }

        public void InvokeRewarded(RewardedParameters parameters)
        {
            Logger.CreateText(this, nameof(InvokeRewarded), parameters.PlacementId);
            try
            {
                // Check availability
                if (!IsRewardedAvailable)
                {
                    Logger.CreateError(this, "Rewarded not available");
                    parameters.OnClose?.Invoke(false);
                    return;
                }
                if (IsRewardedVisible)
                {
                    Logger.CreateError(this, "Rewarded already visible");
                    parameters.OnClose?.Invoke(false);
                    return;
                }
                // Invoke rewarded
                void onOpenCallback()
                {
                    Logger.CreateText(this, nameof(onOpenCallback));
                    parameters.OnOpen?.Invoke();
                    PauseSourceEvent pauseSourceEvent = new(nameof(InvokeRewarded), true);
                    eventAggregator.Publish(this, pauseSourceEvent);
                }
                void onCloseCallback(bool isSuccess)
                {
                    Logger.CreateText(this, nameof(onCloseCallback), isSuccess);
                    parameters.OnClose?.Invoke(isSuccess);
                    if (isSuccess)
                    {
                        lastRewardedSuccess = DateTime.Now;
                        if (!string.IsNullOrEmpty(parameters.PlacementId))
                        {
                            lastRewardedSuccessByTag[parameters.PlacementId] = lastRewardedSuccess;
                        }
                    }
                    PauseSourceEvent pauseSourceEvent = new(nameof(InvokeRewarded), false);
                    eventAggregator.Publish(this, pauseSourceEvent);
                }
                void onAdBlockDetectedCallback()
                {
                    Logger.CreateText(this, nameof(onAdBlockDetectedCallback));
                    parameters.OnAdBlockDetected?.Invoke();
                }
                InvokeRewardedImpl(parameters, onOpenCallback, onCloseCallback, onAdBlockDetectedCallback);
            }
            catch (Exception exception)
            {
                Logger.CreateError(this, nameof(InvokeRewarded), exception);
                parameters.OnClose?.Invoke(false);
                PauseSourceEvent pauseSourceEvent = new(nameof(InvokeRewarded), false);
                eventAggregator.Publish(this, pauseSourceEvent);
            }
        }

        public void InvokeRewarded(Action onOpen = null, Action<bool> onClose = null, string rewardTag = null, Action onAdBlockDetected = null)
        {
            RewardedParameters parameters = new()
            {
                OnOpen = onOpen,
                OnClose = onClose,
                PlacementId = rewardTag,
                OnAdBlockDetected = onAdBlockDetected
            };
            InvokeRewarded(parameters);
        }

    }

}
