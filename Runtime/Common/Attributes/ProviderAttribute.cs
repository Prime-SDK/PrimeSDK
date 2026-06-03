using System;

namespace PrimeGames.SDK.Common {

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ProviderAttribute : Attribute {

        public ProviderAttribute(Type interfaceType) { }

    }

}