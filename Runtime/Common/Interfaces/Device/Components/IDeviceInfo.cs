namespace PrimeGames.SDK.Common {

    [Module]
    public partial interface IDeviceInfo {

        bool IsMobile { get; }
        SystemType SystemType { get; }

    }

}