using System;

namespace PrimeGames.SDK.Common {

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ProviderConfigurationAttribute : Attribute {

        public ProviderConfigurationAttribute(Type providerType) { }

    }

}