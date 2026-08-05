using System;

namespace PrimeGames.SDK.Common
{

    public interface IInterstitial
    {

        bool IsInterstitialReady { get; }
        bool IsInterstitialVisible { get; }
        bool IsInterstitialAvailable { get; }

        DateTime? GetLastInterstitialSuccess();
        void InvokeCountdown(Action onOpen = null, Action<bool> onClose = null);
        void StopCountdown();
        void InvokeInterstitial(Action onOpen = null, Action<bool> onClose = null, Action onAdBlockDetected = null);
        void InvokeInterstitial(InterstitialParameters parameters);

    }

}
