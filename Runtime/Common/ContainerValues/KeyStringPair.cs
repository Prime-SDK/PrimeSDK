using System;

namespace PrimeGames.SDK.Common {

    [Serializable]
    public record KeyStringPair {
        public string key;
        public string value = string.Empty;
    }

}