using PrimeGames.SDK.Common;
using UnityEngine;

namespace PrimeGames.SDK.Prototype {

    [ProviderConfiguration(typeof(PrototypeLanguageInfo))]
    public class PrototypeLanguageInfo_Configuration : PropertyGroup {

        public override string Name => nameof(PrototypeLanguageInfo);

        public LanguageType Current {
            get => CurrentName.ToEnumOrDefault(LanguageType.English);
        }

        [field: SerializeField] public string CurrentName { get; private set; }

        public override EnumProperty[] GetEnumProperties() {
            return new EnumProperty[] {
                new(
                    nameof(Current),
                    () => Current,
                    (value) => CurrentName = value.ToString()
                )
            };
        }

    }

}