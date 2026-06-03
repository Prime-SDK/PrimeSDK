using PrimeGames.SDK.Common;

namespace PrimeGames.SDK.Fallback {

    [Provider(typeof(IPause))]
    public class FallbackPause : CommonPause {

        public FallbackPause(IEventAggregator eventAggregator) : base(eventAggregator) { }

    }

}