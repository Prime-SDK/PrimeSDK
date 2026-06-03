using System;

namespace PrimeGames.SDK.Common {

    [Serializable]
    public record KeyIntPair {
        public string key;
        public int value = 0;
    }

}