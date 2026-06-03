namespace PrimeGames.SDK.Common {

    [Awaitable, Facade]
    public partial interface IAnalytics : IGameplayReporter, IEventsReporter { }

}