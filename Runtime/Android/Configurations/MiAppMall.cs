using PrimeGames.SDK.Common;

namespace PrimeGames.SDK.Android {

    [Configuration]
    public class MiAppMall : AndroidConfiguration {

        public override string Name { get; } = "MiAppMall";
        public override string Description { get; } = "https://app.mi.com/";
        public override string IconName { get; } = "MiAppMall";

    }

}