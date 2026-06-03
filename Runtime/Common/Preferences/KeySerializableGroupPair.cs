using System;

namespace PrimeGames.SDK.Common {

    [Serializable]
    public record KeySerializableGroupPair {
        public string key;
        public SerializableGroup group;
    }

}