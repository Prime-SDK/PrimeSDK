using AOT;
using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Application = UnityEngine.Application;
using RuntimeInitializeLoadType = UnityEngine.RuntimeInitializeLoadType;
using RuntimeInitializeOnLoadMethod = UnityEngine.RuntimeInitializeOnLoadMethodAttribute;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IPause))]
    public class ShowPause : PrimeWebPause {

        [DllImport(Naming.InternalDll)] private static extern void primeSDK_pause_showContinuePrompt(int senderId, DelegateVoid onContinue);

        private static readonly Dictionary<int, Action> continuePromptCallbacks = new();
        private static int nextContinuePromptId;

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnContinuePromptClosed(int senderId) {
            try {
                if (continuePromptCallbacks.TryGetValue(senderId, out Action onContinue)) {
                    continuePromptCallbacks.Remove(senderId);
                    onContinue?.Invoke();
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(ShowPause), nameof(OnContinuePromptClosed), exception);
            }
        }

        public ShowPause(ShowPause_Configuration configuration, IEventAggregator aggregator, IEventDispatcher eventDispatcher)
            : base(aggregator, eventDispatcher) {
            if (Application.isEditor) {
                PauseOverlayView.Clear();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearEditorOverlayOnPlayModeStart() {
            if (Application.isEditor) {
                PauseOverlayView.Clear();
            }
        }

        public override void OnPauseChange(bool isPaused) { }

        public override void ShowContinuePrompt(Action onContinue = null) {
            if (Application.isEditor) {
                PauseOverlayView.ShowContinuePrompt(onContinue);
                return;
            }

            int senderId = nextContinuePromptId++;
            continuePromptCallbacks[senderId] = onContinue;

            try {
                primeSDK_pause_showContinuePrompt(senderId, OnContinuePromptClosed);
            }
            catch (Exception exception) {
                continuePromptCallbacks.Remove(senderId);
                Logger.CreateError(this, exception);
                onContinue?.Invoke();
            }
        }

    }

}
