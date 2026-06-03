using PrimeGames.SDK.Common;

namespace PrimeGames.SDK.Android {

    [Configuration]
    public class XiaomiGames : AndroidConfiguration {

        public override string Name { get; } = "XiaomiGames";
        public override string Description { get; } = "https://global.app.mi.com/games";
        public override string IconName { get; } = "XiaomiGames";

    }

}