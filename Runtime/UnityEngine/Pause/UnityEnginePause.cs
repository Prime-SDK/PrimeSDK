using PrimeGames.SDK.Common;

namespace PrimeGames.SDK.UnityEngine {

    [Provider(typeof(IPause))]
    public class UnityEnginePause : CommonPause {

        public UnityEnginePause(IEventAggregator eventAggregator, IEventDispatcher eventDispatcher) : base(eventAggregator) {
            eventDispatcher.OnApplicationFocus += OnApplicationFocus;
            eventDispatcher.OnApplicationPause += OnApplicationPause;
        }

        public void OnApplicationFocus(bool focusStatus) {
            Register(nameof(OnApplicationFocus), !focusStatus);
        }

        public void OnApplicationPause(bool pauseStatus) {
            Register(nameof(OnApplicationPause), pauseStatus);
        }

    }

}