using AOT;
using PrimeGames.SDK.Common;
using System;
using System.Runtime.InteropServices;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IPause))]
    public class PrimeWebPause : CommonPause {

        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_pause_isPaused();
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_pause_onPauseChange(DelegateInt onPauseChange);

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnPauseChangeCallback(int senderId, int isPaused) {
            try {
                OnExternalPauseChange?.Invoke(isPaused != 0);
            }
            catch (Exception exception) {
                Logger.CreateError(typeof(PrimeWebPause), exception);
            }
        }

        private static event Action<bool> OnExternalPauseChange;

        public PrimeWebPause(IEventAggregator aggregator, IEventDispatcher eventDispatcher) : base(aggregator) {
            eventDispatcher.OnApplicationFocus += OnApplicationFocus;
            eventDispatcher.OnApplicationPause += OnApplicationPause;
            try {
                primeSDK_pause_onPauseChange(OnPauseChangeCallback);
                OnExternalPauseChange += (isPaused) => {
                    Register(nameof(OnExternalPauseChange), isPaused);
                };
                bool isPaused = primeSDK_pause_isPaused();
                Register(nameof(OnExternalPauseChange), isPaused);
            }
            catch (Exception exception) {
                Logger.CreateError(this, exception);
            }
        }

        public void OnApplicationFocus(bool focusStatus) {
            Register(nameof(OnApplicationFocus), !focusStatus);
        }

        public void OnApplicationPause(bool pauseStatus) {
            Register(nameof(OnApplicationPause), pauseStatus);
        }

    }

}