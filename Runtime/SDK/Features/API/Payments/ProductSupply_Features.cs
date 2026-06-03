using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class ProductSupply_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<ProductSupply_Features> { }

        public ProductSupply_Features() {
            SetInfo("Product Supply", nameof(IPayments), nameof(PaymentsProvider));

            CreateButton(nameof(IProductSupply.GetSupplyId), () => {
                Debug.Log(PrimeSDK.Payments.GetSupplyId("customProduct"));
            });
            CreateButton(nameof(IProductSupply.GetSupplyCount), () => {
                Debug.Log(PrimeSDK.Payments.GetSupplyCount("customProduct"));
            });
            CreateButton(nameof(IProductSupply.SetSupplyCount), () => {
                PrimeSDK.Payments.SetSupplyCount("customProduct", 10);
            });
            CreateButton($"{nameof(IProductSupply.SupplyProduct)} (callback)", () => {
                PrimeSDK.Payments.SupplyProduct("customProduct", () => {
                    Debug.Log("Product supplied.");
                }, true);
            });
        }

    }

}