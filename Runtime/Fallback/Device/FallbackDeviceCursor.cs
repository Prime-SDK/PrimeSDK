using PrimeGames.SDK.Common;
using UnityEngine;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.Fallback {

    [Provider(typeof(IDeviceCursor))]
    public class FallbackDeviceCursor : CommonDeviceCursor {

        public FallbackDeviceCursor(IEventAggregator eventAggregator) : base(eventAggregator) { }

        protected override bool HandlePauseEvents => false;

        protected override bool GetVisibleImpl() {
            Logger.NotImplementedWarning(this, nameof(GetVisibleImpl));
            return default;
        }

        protected override void SetVisibleImpl(bool visible) {
            Logger.NotImplementedWarning(this, nameof(SetVisibleImpl));
        }

        protected override CursorLockMode GetLockImpl() {
            Logger.NotImplementedWarning(this, nameof(GetLockImpl));
            return default;
        }

        protected override void SetLockImpl(CursorLockMode cursorLock) {
            Logger.NotImplementedWarning(this, nameof(SetLockImpl));
        }

    }

}