using PrimeGames.SDK.Common;
using Application = UnityEngine.Application;
using RuntimeInitializeLoadType = UnityEngine.RuntimeInitializeLoadType;
using RuntimeInitializeOnLoadMethod = UnityEngine.RuntimeInitializeOnLoadMethodAttribute;

namespace PrimeGames.SDK.UnityEngine {

    [Provider(typeof(IPause))]
    public class UnityEnginePause : CommonPause {

        public UnityEnginePause(UnityEnginePause_Configuration configuration, IEventAggregator eventAggregator, IEventDispatcher eventDispatcher) : base(eventAggregator) {
            PauseOverlayView.Clear();
            eventDispatcher.OnApplicationFocus += OnApplicationFocus;
            eventDispatcher.OnApplicationPause += OnApplicationPause;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearOverlayOnPlayModeStart() {
            if (Application.isEditor) {
                PauseOverlayView.Clear();
            }
        }

        public void OnApplicationFocus(bool focusStatus) {
            if (!Application.isPlaying) {
                ClearPauseState();
                return;
            }

            Register(nameof(OnApplicationFocus), !focusStatus);
        }

        public void OnApplicationPause(bool pauseStatus) {
            if (!Application.isPlaying) {
                ClearPauseState();
                return;
            }

            Register(nameof(OnApplicationPause), pauseStatus);
        }

        public override void OnPauseChange(bool isPaused) { }

        private void ClearPauseState() {
            PauseOverlayView.Clear();
            Register(nameof(OnApplicationFocus), false);
            Register(nameof(OnApplicationPause), false);
        }

    }

}
