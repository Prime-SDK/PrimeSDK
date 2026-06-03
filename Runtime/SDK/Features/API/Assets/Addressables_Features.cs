using PrimeGames.SDK.SourceGenerator;
using PrimeGames.SDK.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class Addressables_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<Addressables_Features> { }

        public Addressables_Features() {
            SetInfo("Addressables", nameof(IAssets), nameof(AddressablesProvider));

            CreateButton(nameof(IAddressables.LoadAddressable), () => {
                PrimeSDK.Assets.LoadAddressable<GameObject>("customAddressable", onSuccess: (asset) => {
                    Debug.Log($"Resolved asset: {asset.name}");
                },
                onError: () => {
                    Debug.LogError("Failed to resolve asset.");
                });
            });

            CreateButton(nameof(IAddressables.ReleaseAddressable), () => {
                PrimeSDK.Assets.ReleaseAddressable("customAddressable");
            });
        }

    }

}