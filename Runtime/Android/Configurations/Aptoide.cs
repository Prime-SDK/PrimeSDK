using PrimeGames.SDK.Common;

namespace PrimeGames.SDK.Android {

    [Configuration]
    public class Aptoide : AndroidConfiguration {

        public override string Name { get; } = "Aptoide";
        public override string Description { get; } = "https://en.aptoide.com/";
        public override string IconName { get; } = "Aptoide";

    }

}