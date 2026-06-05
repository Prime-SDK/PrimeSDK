using System;

namespace PrimeGames.SDK.Common {

    public abstract partial class Configuration {

        public abstract string Name { get; }
        public abstract string Description { get; }
        public virtual string IconName { get; }
        public virtual bool ReadOnly { get; }

        public virtual Type[] PropertyGroups { get; } = Array.Empty<Type>();
        public virtual string[] DefaultOverrideModules { get; } = Array.Empty<string>();
        public virtual string[] DefaultVisibleModules { get; } = Array.Empty<string>();

        public DictionaryGroup CreateDefaultValuesGroup() {
            DictionaryGroup dictionaryGroup = GetDefaultValuesGroup();
            foreach (string moduleName in DefaultOverrideModules) {
                dictionaryGroup.boolCollection[Naming.Key(moduleName, Naming.Override)] = true;
            }
            foreach (string moduleName in DefaultVisibleModules) {
                dictionaryGroup.boolCollection[Naming.Key(moduleName, Naming.Visible)] = true;
            }
            return dictionaryGroup;
        }

    }

}
