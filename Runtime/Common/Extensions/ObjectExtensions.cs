using UnityEngine;

namespace PrimeGames.SDK.Common {

    public static class ObjectExtensions {

        public static bool IsNullOrDestroyed(this Object obj) {
            return obj == null || !obj;
        }

    }

}