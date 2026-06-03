using System;

namespace PrimeGames.SDK.Common {

    public interface IProductRestore {

        void RestorePurchases(Action<IRestoreData> onRestoreData);

    }

}