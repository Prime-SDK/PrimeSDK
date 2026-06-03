using System;
using System.Collections.Generic;

namespace PrimeGames.SDK.Common {

    [Serializable]
    public class SerializableGroup {
        public List<KeyBoolPair> boolCollection = new();
        public List<KeyIntPair> intCollection = new();
        public List<KeyFloatPair> floatCollection = new();
        public List<KeyStringPair> stringCollection = new();
    }

}