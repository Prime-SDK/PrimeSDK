using PrimeGames.SDK.SourceGenerator;
using PrimeGames.SDK.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class AssetBundles_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<AssetBundles_Features> { }

        public AssetBundles_Features() {
            SetInfo("Asset Bundles", nameof(IAssets), nameof(AssetBundlesProvider));

            CreateButton(nameof(IAssetBundles.LoadBundle), () => {
                PrimeSDK.Assets.LoadBundle("customBundle", "https://example.com/customBundle", onSuccess: (assetBundle) => {
                    Debug.Log($"Resolved asset: {assetBundle.name}");
                },
                onError: () => {
                    Debug.LogError("Failed to resolve asset.");
                });
            });

            CreateButton(nameof(IAssetBundles.ReleaseBundle), () => {
                PrimeSDK.Assets.ReleaseBundle("customBundle", unloadAllObjects: true);
            });
        }

    }

}