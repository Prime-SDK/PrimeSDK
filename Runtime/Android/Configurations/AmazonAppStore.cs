using PrimeGames.SDK.Common;

namespace PrimeGames.SDK.Android {

    [Configuration]
    public class AmazonAppStore : AndroidConfiguration {

        public override string Name { get; } = "AmazonAppStore";
        public override string Description { get; } = "https://www.amazon.com/mobile-apps/";
        public override string IconName { get; } = "AmazonAppStore";

    }

}