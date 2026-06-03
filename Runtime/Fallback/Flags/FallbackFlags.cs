using PrimeGames.SDK.Common;
using System;

namespace PrimeGames.SDK.Fallback {

    [Provider(typeof(IFlags))]
    public class FallbackFlags : CommonFlags {

        public FallbackFlags() {
            SetInitialized();
        }

        protected override void ReadJson(Action<string> jsonRequest) {
            jsonRequest?.Invoke(Naming.EmptyJson);
        }

    }

}