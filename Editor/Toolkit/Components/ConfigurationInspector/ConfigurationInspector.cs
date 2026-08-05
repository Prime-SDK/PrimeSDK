using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.Editor {

    internal partial class ConfigurationInspector : VisualElement {

        private const string LegacyVersionSectionName = "Legacy Version";
        private const string LegacyVersionToggleName = "Enabled";
        private const string CountdownLocalizationsVisibleKey = "CountdownLocalizationsVisible";
        private static readonly Dictionary<string, string> PlatformIconAliases = new() {
            { "Xiaomi", "XiaomiGames" }
        };

        private readonly Dictionary<SettingsFoldout, Type> foldoutTypeMapping = new();
        private readonly Dictionary<SettingsFoldout, PropertyGroup> configurationMapping = new();

        private PreferencesEditor preferencesEditor;

        public ConfigurationInspector() {
            VisualTreeAsset asset = VisualTreeReference.LoadVisualTree(nameof(ConfigurationInspector));
            asset.CloneTree(this);
        }

        internal VisualElement ConfigurationView => this.Q<VisualElement>(nameof(ConfigurationView));
        internal VisualElement ConfigurationContainer => this.Q<VisualElement>(nameof(ConfigurationContainer));
        internal VisualElement ProvidersContainer => this.Q<VisualElement>(nameof(ProvidersContainer));
        internal VisualElement ProvidersView => this.Q<VisualElement>(nameof(ProvidersView));

        private ConfigurationType SelectedConfiguration {
            get {
                string configurationName = PackageTools.GetPrefsString(nameof(SelectedConfiguration));
                return configurationName.ToEnumOrDefault<ConfigurationType>();
            }
            set {
                PackageTools.SetPrefsString(nameof(SelectedConfiguration), value.ToString());
            }
        }

        private string SelectedConfigurationName => SelectedConfiguration.ToString();

        public void SelectConfiguration(Type configurationType) {
            string configurationName = configurationType.Name;
            ConfigurationType selectedConfiguration = configurationName.ToEnumOrDefault<ConfigurationType>();
            Configuration configurationInstance = Mapping.CreateConfigurationInstance(configurationName);
            SelectedConfiguration = selectedConfiguration;
            preferencesEditor = PreferencesEditor.CreateEditor();
            Reset();
            InitializeConfigurationFoldouts(configurationInstance);
            InitializeProviderFoldouts(configurationInstance);
        }

        private void UpdateConfigurationFoldoutContent(SettingsFoldout settingsFoldout) {
            settingsFoldout.Clear();
            if (configurationMapping.TryGetValue(settingsFoldout, out PropertyGroup propertyGroup)) {
                Type propertyGroupType = propertyGroup.GetType();
                string preferencesKey = propertyGroupType.Name;

                string propertyGroupJson = preferencesEditor.GetPreferenceGroup(SelectedConfigurationName).GetString(preferencesKey, Naming.EmptyJson);
                bool isOverrideEnabled = preferencesEditor.IsOverrideModuleEnabled(SelectedConfigurationName, preferencesKey);

                PropertyGroup preferencesPropertyGroup = JsonUtility.FromJson(propertyGroupJson, propertyGroupType) as PropertyGroup;
                PropertyGroup defaultPropertyGroup = Activator.CreateInstance(propertyGroupType) as PropertyGroup; // TODO: create factory for this and avoid Activator
                PropertyGroup targetPropertyGroup = isOverrideEnabled ? preferencesPropertyGroup : defaultPropertyGroup;

                CreatePropertyFields(settingsFoldout, targetPropertyGroup, preferencesKey, true);
            }
        }

        private void InitializeConfigurationFoldouts(Configuration configurationInstance) {
            Type[] propertyGroupTypes = configurationInstance.PropertyGroups;
            if (propertyGroupTypes.Length > 0) {
                ConfigurationView.style.display = DisplayStyle.Flex;
            }
            else {
                ConfigurationView.style.display = DisplayStyle.None;
            }
            foreach (Type propertyGroupType in propertyGroupTypes) {
                ConfigurationContainer.Add(CreateConfigurationFoldout(propertyGroupType, configurationInstance.ReadOnly));
            }
        }

        private SettingsFoldout CreateConfigurationFoldout(Type propertyGroupType, bool readOnly, bool muted = false) {
            string preferencesKey = propertyGroupType.Name;

            PropertyGroup propertyGroup = Activator.CreateInstance(propertyGroupType) as PropertyGroup; // TODO: create factory for this and avoid Activator
            bool isOverrideEnabled = preferencesEditor.IsOverrideModuleEnabled(SelectedConfigurationName, preferencesKey);

            SettingsFoldout groupFoldout = new(
                overrideValue: isOverrideEnabled,
                contentVisible: preferencesEditor.GetPreferencesBool(SelectedConfigurationName, preferencesKey, Naming.Visible),
                overrideSetter: (value) => { }
            );
            groupFoldout.name = propertyGroup.Name;
            groupFoldout.Text = propertyGroup.Name;
            SetConfigurationFoldoutIcon(groupFoldout, propertyGroup, muted);
            groupFoldout.OnContentVisibleChange += () => {
                preferencesEditor.SetModuleBool(SelectedConfigurationName, preferencesKey, Naming.Visible, groupFoldout.ContentVisible);
            };
            groupFoldout.OnOverrideValueChange += () => {
                preferencesEditor.SetModuleBool(SelectedConfigurationName, preferencesKey, Naming.Override, groupFoldout.OverrideValue);
                UpdateConfigurationFoldoutContent(groupFoldout);
            };
            if (readOnly) {
                groupFoldout.HideOverrideToggle();
            }
            if (muted) {
                groupFoldout.SetMuted();
            }
            configurationMapping[groupFoldout] = propertyGroup;
            UpdateConfigurationFoldoutContent(groupFoldout);
            return groupFoldout;
        }

        private void SetConfigurationFoldoutIcon(SettingsFoldout foldout, PropertyGroup propertyGroup, bool muted) {
            string iconName = propertyGroup.Name;
            if (PlatformIconAliases.TryGetValue(iconName, out string alias)) {
                iconName = alias;
            }
            Texture2D platformIcon = PackageFiles.FindPlatformTextureAsset(iconName);
            foldout.SetIcon(platformIcon, muted);
        }

        private void WritePropertyGroup(string preferencesKey, PropertyGroup propertyGroup) {
            string json = JsonUtility.ToJson(propertyGroup);
            preferencesEditor.SetString(SelectedConfigurationName, preferencesKey, json);
        }

        private void CreatePropertyFields(VisualElement contentContainer, PropertyGroup propertyGroup, string preferencesKey, bool clearContent) {
            if (clearContent) {
                contentContainer.Clear();
            }
            foreach (EnumProperty property in propertyGroup.GetEnumProperties()) {
                EnumField enumField = new(property.Name, property.Getter()) {
                    value = property.Getter()
                };
                enumField.RegisterValueChangedCallback(callback => {
                    property.Setter.Invoke(callback.newValue);
                    WritePropertyGroup(preferencesKey, propertyGroup);
                });
                contentContainer.Add(enumField);
            }
            foreach (BoolProperty property in propertyGroup.GetBoolProperties()) {
                BoolField boolField = new() {
                    Name = property.Name,
                    Value = property.Getter()
                };
                boolField.OnToggleClick += () => {
                    bool value = property.Getter();
                    property.Setter(!value);
                    boolField.Value = !value;
                    WritePropertyGroup(preferencesKey, propertyGroup);
                };
                contentContainer.Add(boolField);
            }
            foreach (StringProperty property in propertyGroup.GetStringProperties()) {
                TextField textField = new(property.Name) {
                    value = property.Getter()
                };
                textField.RegisterValueChangedCallback(callback => {
                    property.Setter(callback.newValue);
                    WritePropertyGroup(preferencesKey, propertyGroup);
                });
                contentContainer.Add(textField);
            }
            foreach (IntProperty property in propertyGroup.GetIntProperties()) {
                IntegerField integerField = new(property.Name) {
                    value = property.Getter()
                };
                integerField.RegisterValueChangedCallback(callback => {
                    property.Setter(callback.newValue);
                    WritePropertyGroup(preferencesKey, propertyGroup);
                });
                contentContainer.Add(integerField);
            }
            foreach (FloatProperty property in propertyGroup.GetFloatProperties()) {
                FloatField floatField = new(property.Name) {
                    value = property.Getter()
                };
                floatField.RegisterValueChangedCallback(callback => {
                    property.Setter(callback.newValue);
                    WritePropertyGroup(preferencesKey, propertyGroup);
                });
                contentContainer.Add(floatField);
            }
            CreateCountdownLocalizationSection(contentContainer, propertyGroup, preferencesKey);
            CreateLegacyVersionSection(contentContainer, propertyGroup, preferencesKey);
        }

        private void CreateCountdownLocalizationSection(VisualElement contentContainer, PropertyGroup propertyGroup, string preferencesKey) {
            if (propertyGroup.GetType().Name != "PrimeWebAds_Configuration" && propertyGroup.GetType().Name != "PrototypeAds_Configuration") {
                return;
            }

            PropertyInfo localizationsProperty = propertyGroup.GetType().GetProperty("CountdownLocalizations", BindingFlags.Instance | BindingFlags.Public);
            if (localizationsProperty?.GetValue(propertyGroup) is not IList localizations) {
                return;
            }

            bool isVisible = PackageTools.GetPrefsBool(CountdownLocalizationsVisibleKey, false);
            VisualElement section = new();
            section.AddToClassList("countdown-localizations-section");

            VisualElement header = new();
            header.AddToClassList("countdown-localizations-header");
            section.Add(header);

            Label title = new($"Countdown Localizations ({localizations.Count})") {
                pickingMode = PickingMode.Ignore
            };
            title.AddToClassList("countdown-localizations-title");
            header.Add(title);

            Button toggleButton = new(() => {
                PackageTools.SetPrefsBool(CountdownLocalizationsVisibleKey, !isVisible);
                CreatePropertyFields(contentContainer, propertyGroup, preferencesKey, true);
            }) {
                text = isVisible ? "Hide" : "Show"
            };
            toggleButton.AddToClassList("countdown-localizations-toggle-button");
            header.Add(toggleButton);

            Button addButton = new(() => {
                AddCountdownLocalization(localizations);
                PackageTools.SetPrefsBool(CountdownLocalizationsVisibleKey, true);
                WritePropertyGroup(preferencesKey, propertyGroup);
                CreatePropertyFields(contentContainer, propertyGroup, preferencesKey, true);
            }) {
                text = "+"
            };
            addButton.AddToClassList("countdown-localizations-add-button");
            header.Add(addButton);

            Button resetButton = new(() => {
                ResetCountdownLocalizations(localizations);
                PackageTools.SetPrefsBool(CountdownLocalizationsVisibleKey, true);
                WritePropertyGroup(preferencesKey, propertyGroup);
                CreatePropertyFields(contentContainer, propertyGroup, preferencesKey, true);
            }) {
                text = "Reset"
            };
            resetButton.AddToClassList("countdown-localizations-reset-button");
            header.Add(resetButton);

            VisualElement listContainer = new();
            listContainer.AddToClassList("countdown-localizations-list");
            listContainer.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            section.Add(listContainer);

            for (int index = 0; index < localizations.Count; index++) {
                object localization = localizations[index];
                if (localization == null) {
                    continue;
                }
                listContainer.Add(CreateCountdownLocalizationRow(localizations, index, localization, contentContainer, propertyGroup, preferencesKey));
            }

            contentContainer.Add(section);
        }

        private VisualElement CreateCountdownLocalizationRow(IList localizations, int index, object localization, VisualElement contentContainer, PropertyGroup propertyGroup, string preferencesKey) {
            Type localizationType = localization.GetType();
            FieldInfo languageField = localizationType.GetField("Language", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo messageField = localizationType.GetField("Message", BindingFlags.Instance | BindingFlags.Public);

            VisualElement row = new();
            row.AddToClassList("countdown-localizations-row");

            Enum language = languageField?.GetValue(localization) as Enum ?? LanguageType.English;
            EnumField languageFieldElement = new(language) {
                value = language
            };
            languageFieldElement.AddToClassList("countdown-localizations-language");
            languageFieldElement.RegisterValueChangedCallback(evt => {
                languageField?.SetValue(localization, evt.newValue);
                WritePropertyGroup(preferencesKey, propertyGroup);
            });
            row.Add(languageFieldElement);

            TextField messageFieldElement = new() {
                value = messageField?.GetValue(localization) as string ?? string.Empty
            };
            messageFieldElement.AddToClassList("countdown-localizations-message");
            messageFieldElement.RegisterValueChangedCallback(evt => {
                messageField?.SetValue(localization, evt.newValue);
                WritePropertyGroup(preferencesKey, propertyGroup);
            });
            row.Add(messageFieldElement);

            Button removeButton = new(() => {
                localizations.RemoveAt(index);
                WritePropertyGroup(preferencesKey, propertyGroup);
                CreatePropertyFields(contentContainer, propertyGroup, preferencesKey, true);
            }) {
                text = "-"
            };
            removeButton.AddToClassList("countdown-localizations-remove-button");
            row.Add(removeButton);

            return row;
        }

        private void AddCountdownLocalization(IList localizations) {
            Type localizationType = localizations.GetType().IsGenericType ? localizations.GetType().GetGenericArguments()[0] : null;
            if (localizationType == null) {
                return;
            }

            LanguageType language = GetFirstMissingCountdownLanguage(localizations);
            localizations.Add(CreateCountdownLocalization(localizationType, language));
        }

        private void ResetCountdownLocalizations(IList localizations) {
            Type localizationType = localizations.GetType().IsGenericType ? localizations.GetType().GetGenericArguments()[0] : null;
            if (localizationType == null) {
                return;
            }

            localizations.Clear();
            foreach (LanguageType language in Enum.GetValues(typeof(LanguageType))) {
                localizations.Add(CreateCountdownLocalization(localizationType, language));
            }
        }

        private static object CreateCountdownLocalization(Type localizationType, LanguageType language) {
            object localization = Activator.CreateInstance(localizationType);
            FieldInfo languageField = localizationType.GetField("Language", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo messageField = localizationType.GetField("Message", BindingFlags.Instance | BindingFlags.Public);
            languageField?.SetValue(localization, language);
            messageField?.SetValue(localization, GetDefaultCountdownMessageSafe(language));
            return localization;
        }

        private static LanguageType GetFirstMissingCountdownLanguage(IList localizations) {
            HashSet<LanguageType> usedLanguages = new();
            foreach (object localization in localizations) {
                FieldInfo languageField = localization?.GetType().GetField("Language", BindingFlags.Instance | BindingFlags.Public);
                if (languageField?.GetValue(localization) is LanguageType language) {
                    usedLanguages.Add(language);
                }
            }

            foreach (LanguageType language in Enum.GetValues(typeof(LanguageType))) {
                if (!usedLanguages.Contains(language)) {
                    return language;
                }
            }
            return LanguageType.English;
        }

        private static string GetDefaultCountdownMessageSafe(LanguageType language) {
            return language switch {
                LanguageType.Russian => "\u0420\u0435\u043a\u043b\u0430\u043c\u0430 \u043d\u0430\u0447\u043d\u0451\u0442\u0441\u044f \u0447\u0435\u0440\u0435\u0437",
                LanguageType.Japanese => "\u5e83\u544a\u958b\u59cb\u307e\u3067",
                LanguageType.Chinese => "\u5e7f\u544a\u5c06\u5728",
                LanguageType.Turkish => "Reklam \u015fu s\u00fcre i\u00e7inde ba\u015flayacak",
                LanguageType.Hindi => "\u0935\u093f\u091c\u094d\u091e\u093e\u092a\u0928 \u0936\u0941\u0930\u0942 \u0939\u094b\u0917\u093e",
                LanguageType.Korean => "\uad11\uace0 \uc2dc\uc791\uae4c\uc9c0",
                LanguageType.Portuguese => "O an\u00fancio come\u00e7a em",
                LanguageType.Indonesian => "Iklan dimulai dalam",
                LanguageType.German => "Werbung startet in",
                LanguageType.Spanish => "El anuncio empieza en",
                LanguageType.Italian => "L'annuncio inizia tra",
                LanguageType.Ukrainian => "\u0420\u0435\u043a\u043b\u0430\u043c\u0430 \u043f\u043e\u0447\u043d\u0435\u0442\u044c\u0441\u044f \u0447\u0435\u0440\u0435\u0437",
                LanguageType.Polish => "Reklama zacznie si\u0119 za",
                LanguageType.French => "La publicit\u00e9 commence dans",
                LanguageType.Danish => "Annoncen starter om",
                LanguageType.Czech => "Reklama za\u010dne za",
                LanguageType.Afrikaans => "Advertensie begin oor",
                LanguageType.Icelandic => "Augl\u00fdsing byrjar eftir",
                LanguageType.Norwegian => "Annonse starter om",
                LanguageType.Swedish => "Annonsen startar om",
                LanguageType.Dutch => "Advertentie start over",
                LanguageType.Slovak => "Reklama za\u010dne o",
                LanguageType.Thai => "\u0e42\u0e06\u0e29\u0e13\u0e32\u0e08\u0e30\u0e40\u0e23\u0e34\u0e48\u0e21\u0e43\u0e19",
                LanguageType.Vietnamese => "Qu\u1ea3ng c\u00e1o b\u1eaft \u0111\u1ea7u sau",
                _ => "Ad starts in"
            };
        }

        private void CreateLegacyVersionSection(VisualElement contentContainer, PropertyGroup propertyGroup, string preferencesKey) {
            BoolProperty legacyVersionProperty = GetLegacyVersionProperty(propertyGroup);
            if (legacyVersionProperty == null) {
                return;
            }

            VisualElement section = new();
            section.AddToClassList("legacy-version-section");

            Label title = new(LegacyVersionSectionName) {
                pickingMode = PickingMode.Ignore
            };
            title.AddToClassList("legacy-version-title");
            section.Add(title);

            BoolField legacyVersionField = new() {
                Name = legacyVersionProperty.Name,
                Value = legacyVersionProperty.Getter()
            };
            legacyVersionField.OnToggleClick += () => {
                bool value = legacyVersionProperty.Getter();
                legacyVersionProperty.Setter(!value);
                legacyVersionField.Value = !value;
                WritePropertyGroup(preferencesKey, propertyGroup);
                CreatePropertyFields(contentContainer, propertyGroup, preferencesKey, true);
            };
            section.Add(legacyVersionField);
            contentContainer.Add(section);
        }

        private BoolProperty GetLegacyVersionProperty(PropertyGroup propertyGroup) {
            Type propertyGroupType = propertyGroup.GetType();
            if (propertyGroupType.Name == "Y8New_PropertyGroup") {
                FieldInfo useNewSdkField = propertyGroupType.GetField("useNewSdk");
                if (useNewSdkField == null) {
                    return null;
                }
                return new BoolProperty(
                    LegacyVersionToggleName,
                    () => !(bool)useNewSdkField.GetValue(propertyGroup),
                    value => { useNewSdkField.SetValue(propertyGroup, !value); }
                );
            }
            if (propertyGroupType.Name == "LaggedNew_PropertyGroup") {
                FieldInfo useLegacySdkField = propertyGroupType.GetField("useLegacySdk");
                if (useLegacySdkField == null) {
                    return null;
                }
                return new BoolProperty(
                    LegacyVersionToggleName,
                    () => (bool)useLegacySdkField.GetValue(propertyGroup),
                    value => { useLegacySdkField.SetValue(propertyGroup, value); }
                );
            }
            return null;
        }

        private void InitializeProviderFoldouts(Configuration configurationInstance) {
            Dictionary<Type, Type[]> rootCollection = Mapping.RootModules;
            Type[] sortedRootInterfaces = rootCollection.Keys.ToArray();
            Array.Sort(sortedRootInterfaces, (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            foreach (Type rootInterface in sortedRootInterfaces) {
                string rootInterfaceName = rootInterface.Name;
                SettingsFoldout rootFoldout = new(
                    overrideValue: preferencesEditor.IsOverrideModuleEnabled(SelectedConfigurationName, rootInterfaceName),
                    contentVisible: preferencesEditor.GetPreferencesBool(SelectedConfigurationName, rootInterface.Name, Naming.Visible),
                    overrideSetter: (value) => { }
                );
                rootFoldout.name = rootInterfaceName.TrimInterfacePrefix();
                rootFoldout.Text = rootInterfaceName.TrimInterfacePrefix();
                rootFoldout.OnContentVisibleChange += () => {
                    preferencesEditor.SetModuleBool(SelectedConfigurationName, rootInterfaceName, Naming.Visible, rootFoldout.ContentVisible);
                };
                rootFoldout.OnOverrideValueChange += () => {
                    preferencesEditor.SetModuleBool(SelectedConfigurationName, rootInterfaceName, Naming.Override, rootFoldout.OverrideValue);
                    UpdateProviderFoldoutContent(rootFoldout);
                };
                if (configurationInstance.ReadOnly) {
                    rootFoldout.HideOverrideToggle();
                }
                foldoutTypeMapping[rootFoldout] = rootInterface;
                UpdateProviderFoldoutContent(rootFoldout);
                ProvidersContainer.Add(rootFoldout);
            }
        }

        private void Reset() {
            foldoutTypeMapping.Clear();
            ConfigurationContainer.Clear();
            ProvidersContainer.Clear();
        }

        private void UpdateProviderFoldoutContent(SettingsFoldout settingsFoldout) {
            settingsFoldout.Clear();
            Type rootInterface = foldoutTypeMapping[settingsFoldout];
            Type[] rootModules = Mapping.RootModules[rootInterface];
            if (rootModules.Length == 0) {
                Type providerEnumType = Mapping.ProviderEnums[rootInterface];
                CreateProviderEnumField(settingsFoldout, rootInterface, providerEnumType);
            }
            else {
                foreach (Type interfaceModule in rootModules) {
                    Type providerEnumType = Mapping.ProviderEnums[interfaceModule];
                    CreateProviderEnumField(settingsFoldout, rootInterface, providerEnumType);
                }
            }
        }

        private void CreateProviderEnumField(SettingsFoldout settingsFoldout, Type rootInterface, Type providerEnumType) {
            string preferencesProviderName = preferencesEditor.GetModuleString(SelectedConfigurationName, rootInterface.Name, providerEnumType.Name);
            string defaultProviderName = preferencesEditor.GetDefaultString(SelectedConfigurationName, rootInterface.Name, providerEnumType.Name);
            Enum defaultProviderEnum = defaultProviderName.ToEnumOrDefault(providerEnumType);
            Enum providerTypeEnum = preferencesProviderName.ToEnumOrDefault(providerEnumType, defaultProviderEnum);
            CreateEnumField(
                name: providerEnumType.Name,
                initialValue: providerTypeEnum,
                parent: settingsFoldout,
                onValueChanged: (newValue) => {
                    preferencesEditor.SetModuleString(SelectedConfigurationName, rootInterface.Name, providerEnumType.Name, newValue.ToString());
                    UpdateProviderFoldoutContent(settingsFoldout);
                }
            );
            if (providerTypeEnum == null) {
                return;
            }
            bool returnDefault = !preferencesEditor.IsOverrideModuleEnabled(SelectedConfigurationName, rootInterface.Name);
            string providerTypeName = providerTypeEnum.ToString();
            if (Mapping.ProviderTypes.ContainsKey(providerTypeName) == false) {
                return;
            }
            Type providerType = Mapping.ProviderTypes[providerTypeName];
            if (MainFactory.GetProviderConfiguration(preferencesEditor, SelectedConfigurationName, providerType, returnDefault) is PropertyGroup providerConfiguration) {
                SettingsGroup settingsGroup = new();
                string preferencesKey = providerConfiguration.GetType().Name;
                CreatePropertyFields(settingsGroup, providerConfiguration, preferencesKey, false);
                if (settingsGroup.contentContainer.childCount > 0) {
                    settingsFoldout.Add(settingsGroup);
                }
            }
        }

        private void CreateEnumField(string name, Enum initialValue, VisualElement parent, Action<Enum> onValueChanged) {
            EnumField enumField = new(name.InsertSpacing(), initialValue);
            enumField.AddToClassList("configuration-inspector-enum-field");
            enumField.RegisterValueChangedCallback(callback => {
                onValueChanged((Enum)callback.newValue);
            });
            parent.Add(enumField);
        }

    }

}
