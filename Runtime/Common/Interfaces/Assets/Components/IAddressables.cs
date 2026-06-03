using System;

namespace PrimeGames.SDK.Common {

    [Module]
    public partial interface IAddressables {

        void LoadAddressable<T>(string addressableKey, Action<T> onSuccess, Action onError = null);
        void ReleaseAddressable(string addressableKey);

    }

}