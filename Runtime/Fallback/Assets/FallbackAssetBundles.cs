using PrimeGames.SDK.Common;
using System;
using UnityEngine;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.Fallback {

    [Provider(typeof(IAssetBundles))]
    public class FallbackAssetBundles : CommonAssetBundles {

        protected override void LoadBundleImpl(string bundleTag, string bundleURL, Action<AssetBundle> onSuccess, Action onError = null) {
            Logger.NotImplementedWarning(this, nameof(LoadBundleImpl));
            onError?.Invoke();
        }

        protected override void ReleaseBundleImpl(string bundleTag, bool unloadAllObjects) {
            Logger.NotImplementedWarning(this, nameof(ReleaseBundleImpl));
        }

    }

}