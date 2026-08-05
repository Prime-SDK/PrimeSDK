using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Editor {

    internal partial class BuildAutomationView : VisualElement {

        private static readonly Color CheckmarkVisible = Color.white;
        private static readonly Color CheckmarkInvisible = new(1f, 1f, 1f, 0f);

        private readonly bool showBuildActions;

        public BuildAutomationView(bool showBuildActions = true) {
            this.showBuildActions = showBuildActions;

            VisualTreeAsset asset = VisualTreeReference.LoadVisualTree(nameof(BuildAutomationView));
            asset.CloneTree(this);
            style.flexGrow = 1;

            InitializeView();
        }

        private VisualElement UnityGroup => this.Q<VisualElement>("UnityGroup");
        private DropdownField TargetPipeline => this.Q<DropdownField>("TargetPipeline");
        private Button OpenBuildSettings => this.Q<Button>("OpenBuildSettings");
        private Button OpenPlayerSettings => this.Q<Button>("OpenPlayerSettings");

        private VisualElement WebGLSettingsGroup => this.Q<VisualElement>("WebGLSettingsGroup");
        private DropdownField EnableExceptions => this.Q<DropdownField>("EnableExceptions");
        private DropdownField CompressionFormat => this.Q<DropdownField>("CompressionFormat");
        private Toggle NameFilesAsHashes => this.Q<Toggle>("NameFilesAsHashes");
        private Toggle DataCaching => this.Q<Toggle>("DataCaching");
        private DropdownField DebugSymbols => this.Q<DropdownField>("DebugSymbols");
        private Toggle DecompressionFallback => this.Q<Toggle>("DecompressionFallback");

        private VisualElement WebGLOutputGroup => this.Q<VisualElement>("WebGLOutputGroup");
        private DropdownField BuildExportFormat => this.Q<DropdownField>("BuildExportFormat");
        private TextField BuildsFolderPath => this.Q<TextField>("BuildsFolderPath");
        private Button ResetBuildsFolder => this.Q<Button>("ResetBuildsFolder");
        private Button SelectBuildsFolder => this.Q<Button>("SelectBuildsFolder");
        private TextField BuildFileName => this.Q<TextField>("BuildFileName");
        private Button ResetFileName => this.Q<Button>("ResetFileName");

        private VisualElement AndroidOutputGroup => this.Q<VisualElement>("AndroidOutputGroup");
        private DropdownField AndroidBuildFormatField => this.Q<DropdownField>("AndroidBuildFormat");
        private Toggle AndroidDevelopmentBuild => this.Q<Toggle>("AndroidDevelopmentBuild");
        private Toggle AndroidScriptDebugging => this.Q<Toggle>("AndroidScriptDebugging");
        private Toggle AndroidCleanOutputBeforeBuild => this.Q<Toggle>("AndroidCleanOutputBeforeBuild");
        private Button AndroidOpenBuildsFolder => this.Q<Button>("AndroidOpenBuildsFolder");
        private TextField AndroidBuildsFolderPath => this.Q<TextField>("AndroidBuildsFolderPath");
        private Button AndroidResetBuildsFolder => this.Q<Button>("AndroidResetBuildsFolder");
        private Button AndroidSelectBuildsFolder => this.Q<Button>("AndroidSelectBuildsFolder");
        private TextField AndroidBuildFileName => this.Q<TextField>("AndroidBuildFileName");
        private Button AndroidResetFileName => this.Q<Button>("AndroidResetFileName");

        private BuildExportFormat CurrentBuildExportFormat {
            get {
                string valueName = PackageTools.GetPrefsString(nameof(CurrentBuildExportFormat));
                return valueName.ToEnumOrDefault<BuildExportFormat>();
            }
        }

        private string DefaultBuildsFolderPath {
            get {
                return Path.Combine(PackageTools.ProjectPath, Naming.Builds).NormalizePath();
            }
        }

        private BuildOptimizerPipeline CurrentTargetPipeline {
            get {
                string valueName = PackageTools.GetPrefsString(nameof(CurrentTargetPipeline), BuildOptimizerPipeline.CurrentActiveTarget.ToString());
                return valueName.ToEnumOrDefault<BuildOptimizerPipeline>();
            }
            set => PackageTools.SetPrefsString(nameof(CurrentTargetPipeline), value.ToString());
        }

        private string CurrentBuildsFolderPath {
            get {
                return PackageTools.GetPrefsString(nameof(CurrentBuildsFolderPath), DefaultBuildsFolderPath);
            }
            set => PackageTools.SetPrefsString(nameof(CurrentBuildsFolderPath), value);
        }

        private string DefaultProjectName {
            get {
                return PlayerSettings.productName.ToSafeFileName("build");
            }
        }

        private string DefaultBuildFileName {
            get {
                return $"{DefaultProjectName}[#NUMBER]-primeSDK[#VERSION]";
            }
        }

        private string CurrentBuildFileName {
            get {
                return PackageTools.GetPrefsString(nameof(CurrentBuildFileName), DefaultBuildFileName);
            }
            set => PackageTools.SetPrefsString(nameof(CurrentBuildFileName), value);
        }

        private string DefaultAndroidBuildsFolderPath {
            get {
                return Path.Combine(PackageTools.ProjectPath, Naming.Builds).NormalizePath();
            }
        }

        private string CurrentAndroidBuildsFolderPath {
            get {
                return PackageTools.GetPrefsString(nameof(CurrentAndroidBuildsFolderPath), DefaultAndroidBuildsFolderPath);
            }
            set => PackageTools.SetPrefsString(nameof(CurrentAndroidBuildsFolderPath), value);
        }

        private string DefaultAndroidBuildFileName {
            get {
                return $"{DefaultProjectName}[#NUMBER]-primeSDK[#VERSION]";
            }
        }

        private string CurrentAndroidBuildFileName {
            get {
                return PackageTools.GetPrefsString(nameof(CurrentAndroidBuildFileName), DefaultAndroidBuildFileName);
            }
            set => PackageTools.SetPrefsString(nameof(CurrentAndroidBuildFileName), value);
        }

        private AndroidBuildFormat CurrentAndroidBuildFormat {
            get {
                string valueName = PackageTools.GetPrefsString(nameof(CurrentAndroidBuildFormat), AndroidBuildFormat.APK.ToString());
                return valueName.ToEnumOrDefault<AndroidBuildFormat>();
            }
            set => PackageTools.SetPrefsString(nameof(CurrentAndroidBuildFormat), value.ToString());
        }

        private bool CurrentAndroidDevelopmentBuild {
            get => PackageTools.GetPrefsBool(nameof(CurrentAndroidDevelopmentBuild), false);
            set => PackageTools.SetPrefsBool(nameof(CurrentAndroidDevelopmentBuild), value);
        }

        private bool CurrentAndroidScriptDebugging {
            get => PackageTools.GetPrefsBool(nameof(CurrentAndroidScriptDebugging), false);
            set => PackageTools.SetPrefsBool(nameof(CurrentAndroidScriptDebugging), value);
        }

        private bool CurrentAndroidCleanOutputBeforeBuild {
            get => PackageTools.GetPrefsBool(nameof(CurrentAndroidCleanOutputBeforeBuild), true);
            set => PackageTools.SetPrefsBool(nameof(CurrentAndroidCleanOutputBeforeBuild), value);
        }

        private VisualElement ConfigurationGroup => this.Q<VisualElement>("ConfigurationGroup");
        private DropdownField BuildConfiguration => this.Q<DropdownField>("BuildConfiguration");

        private ConfigurationType BuildConfigurationType {
            get {
                PreferencesEditor preferencesEditor = PreferencesEditor.CreateEditor();
                string configurationName = preferencesEditor.GetBuildConfigurationName();
                return configurationName.ToEnumOrDefault<ConfigurationType>(ConfigurationType.FallbackConfiguration);
            }
            set {
                PreferencesEditor preferencesEditor = PreferencesEditor.CreateEditor();
                preferencesEditor.SetBuildConfigurationName(value.ToString());
            }
        }

        private VisualElement WebGLActionsGroup => this.Q<VisualElement>("WebGLActionsGroup");
        private Button Build => this.Q<Button>("Build");
        private Button BuildAndRun => this.Q<Button>("BuildAndRun");
        private Button OpenBuildsFolder => this.Q<Button>("OpenBuildsFolder");

        private void UpdateValues() {
            EnableExceptions.SetValueWithoutNotify(PlayerSettings.WebGL.exceptionSupport.ToString());
            CompressionFormat.SetValueWithoutNotify(PlayerSettings.WebGL.compressionFormat.ToString());
            NameFilesAsHashes.SetValueWithoutNotify(PlayerSettings.WebGL.nameFilesAsHashes);
            DataCaching.SetValueWithoutNotify(PlayerSettings.WebGL.dataCaching);
            DebugSymbols.SetValueWithoutNotify(PlayerSettings.WebGL.debugSymbolMode.ToString());
            DecompressionFallback.SetValueWithoutNotify(PlayerSettings.WebGL.decompressionFallback);
        }

        private void InitializeView() {
            // Unity
            OpenBuildSettings.clicked += () => {
                EditorWindow.GetWindow(typeof(BuildPlayerWindow));
            };
            OpenPlayerSettings.clicked += () => {
                SettingsService.OpenProjectSettings("Project/Player");
            };
            Array targetPipelineChoices = Enum.GetValues(typeof(BuildOptimizerPipeline));
            TargetPipeline.choices = targetPipelineChoices.Cast<BuildOptimizerPipeline>().Select(v => v.ToString()).ToList();
            TargetPipeline.value = CurrentTargetPipeline.ToString();
            TargetPipeline.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                if (Enum.TryParse(evt.newValue, out BuildOptimizerPipeline result)) {
                    CurrentTargetPipeline = result;
                    RefreshPipelineVisibility();
                }
            });

            // WebGL Settings
            Array enableExceptionsChoices = Enum.GetValues(typeof(WebGLExceptionSupport));
            EnableExceptions.choices = enableExceptionsChoices.Cast<WebGLExceptionSupport>().Select(v => v.ToString()).ToList();
            EnableExceptions.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                if (Enum.TryParse<WebGLExceptionSupport>(evt.newValue, out WebGLExceptionSupport result)) {
                    PlayerSettings.WebGL.exceptionSupport = result;
                    SavePlayerSettings();
                }
            });
            Array compressionFormatChoices = Enum.GetValues(typeof(WebGLCompressionFormat));
            CompressionFormat.choices = compressionFormatChoices.Cast<WebGLCompressionFormat>().Select(v => v.ToString()).ToList();
            CompressionFormat.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                if (Enum.TryParse<WebGLCompressionFormat>(evt.newValue, out WebGLCompressionFormat result)) {
                    PlayerSettings.WebGL.compressionFormat = result;
                    SavePlayerSettings();
                }
            });
            NameFilesAsHashes.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                PlayerSettings.WebGL.nameFilesAsHashes = evt.newValue;
                SyncToggleVisual(NameFilesAsHashes);
                SavePlayerSettings();
            });
            DataCaching.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                PlayerSettings.WebGL.dataCaching = evt.newValue;
                SyncToggleVisual(DataCaching);
                SavePlayerSettings();
            });
            Array debugSymbolsChoices = Enum.GetValues(typeof(WebGLDebugSymbolMode));
            DebugSymbols.choices = debugSymbolsChoices.Cast<WebGLDebugSymbolMode>().Select(v => v.ToString()).ToList();
            DebugSymbols.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                if (Enum.TryParse<WebGLDebugSymbolMode>(evt.newValue, out WebGLDebugSymbolMode result)) {
                    PlayerSettings.WebGL.debugSymbolMode = result;
                    SavePlayerSettings();
                }
            });
            DecompressionFallback.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                PlayerSettings.WebGL.decompressionFallback = evt.newValue;
                SyncToggleVisual(DecompressionFallback);
                SavePlayerSettings();
            });

            // WebGL Output
            Array buildExportFormatChoices = Enum.GetValues(typeof(BuildExportFormat));
            BuildExportFormat.choices = buildExportFormatChoices.Cast<BuildExportFormat>().Select(v => v.ToString()).ToList();
            BuildExportFormat.value = CurrentBuildExportFormat.ToString();
            BuildExportFormat.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                if (Enum.TryParse<BuildExportFormat>(evt.newValue, out BuildExportFormat result)) {
                    PackageTools.SetPrefsString(nameof(CurrentBuildExportFormat), result.ToString());
                }
            });
            BuildsFolderPath.value = CurrentBuildsFolderPath;
            BuildsFolderPath.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                CurrentBuildsFolderPath = evt.newValue;
            });
            ResetBuildsFolder.clicked += () => {
                CurrentBuildsFolderPath = DefaultBuildsFolderPath;
                BuildsFolderPath.value = DefaultBuildsFolderPath;
            };
            SelectBuildsFolder.clicked += () => {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Builds Folder", CurrentBuildsFolderPath, "");
                if (!string.IsNullOrEmpty(selectedPath)) {
                    CurrentBuildsFolderPath = selectedPath.NormalizePath();
                    BuildsFolderPath.value = CurrentBuildsFolderPath;
                }
            };
            BuildFileName.value = CurrentBuildFileName;
            BuildFileName.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                CurrentBuildFileName = evt.newValue;
            });
            ResetFileName.clicked += () => {
                CurrentBuildFileName = DefaultBuildFileName;
                BuildFileName.value = DefaultBuildFileName;
            };

            // Android Output
            Array androidBuildFormatChoices = Enum.GetValues(typeof(AndroidBuildFormat));
            AndroidBuildFormatField.choices = androidBuildFormatChoices.Cast<AndroidBuildFormat>().Select(v => v.ToString()).ToList();
            AndroidBuildFormatField.value = CurrentAndroidBuildFormat.ToString();
            AndroidBuildFormatField.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                if (Enum.TryParse(evt.newValue, out AndroidBuildFormat result)) {
                    CurrentAndroidBuildFormat = result;
                }
            });
            AndroidDevelopmentBuild.value = CurrentAndroidDevelopmentBuild;
            AndroidDevelopmentBuild.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                CurrentAndroidDevelopmentBuild = evt.newValue;
                SyncToggleVisual(AndroidDevelopmentBuild);
            });
            AndroidScriptDebugging.value = CurrentAndroidScriptDebugging;
            AndroidScriptDebugging.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                CurrentAndroidScriptDebugging = evt.newValue;
                SyncToggleVisual(AndroidScriptDebugging);
            });
            AndroidCleanOutputBeforeBuild.value = CurrentAndroidCleanOutputBeforeBuild;
            AndroidCleanOutputBeforeBuild.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                CurrentAndroidCleanOutputBeforeBuild = evt.newValue;
                SyncToggleVisual(AndroidCleanOutputBeforeBuild);
            });
            AndroidBuildsFolderPath.value = CurrentAndroidBuildsFolderPath;
            AndroidBuildsFolderPath.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                CurrentAndroidBuildsFolderPath = evt.newValue;
            });
            AndroidResetBuildsFolder.clicked += () => {
                CurrentAndroidBuildsFolderPath = DefaultAndroidBuildsFolderPath;
                AndroidBuildsFolderPath.value = DefaultAndroidBuildsFolderPath;
            };
            AndroidSelectBuildsFolder.clicked += () => {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Android Output Folder", CurrentAndroidBuildsFolderPath, "");
                if (!string.IsNullOrEmpty(selectedPath)) {
                    CurrentAndroidBuildsFolderPath = selectedPath.NormalizePath();
                    AndroidBuildsFolderPath.value = CurrentAndroidBuildsFolderPath;
                }
            };
            AndroidBuildFileName.value = CurrentAndroidBuildFileName;
            AndroidBuildFileName.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                CurrentAndroidBuildFileName = evt.newValue;
            });
            AndroidResetFileName.clicked += () => {
                CurrentAndroidBuildFileName = DefaultAndroidBuildFileName;
                AndroidBuildFileName.value = DefaultAndroidBuildFileName;
            };
            AndroidOpenBuildsFolder.clicked += () => {
                string buildsFolderPath = CurrentAndroidBuildsFolderPath;
                if (!Directory.Exists(buildsFolderPath)) {
                    Directory.CreateDirectory(buildsFolderPath);
                }
                EditorUtility.RevealInFinder(buildsFolderPath + Path.AltDirectorySeparatorChar);
            };

            // Configuration
            Array configurationChoices = Enum.GetValues(typeof(ConfigurationType));
            BuildConfiguration.choices = configurationChoices.Cast<ConfigurationType>().Select(v => v.ToString()).ToList();
            ToolkitWindow.OnConfigurationChanged += () => {
                BuildConfiguration.value = BuildConfigurationType.ToString();
            };
            BuildConfiguration.value = BuildConfigurationType.ToString();
            BuildConfiguration.RegisterValueChangedCallback(evt => {
                if (evt.newValue == evt.previousValue) return;
                if (Enum.TryParse<ConfigurationType>(evt.newValue, out ConfigurationType result)) {
                    BuildConfigurationType = result;
                    ToolkitWindow.OnConfigurationChanged?.Invoke();
                    RefreshPipelineVisibility();
                }
            });

            // WebGL Actions
            Build.clicked += () => {
                switch (CurrentBuildExportFormat) {
                    case Editor.BuildExportFormat.Folder: {
                        BuildFolder();
                        break;
                    }
                    case Editor.BuildExportFormat.UncompressedZip: {
                        BuildUncompressedZip();
                        break;
                    }
                }
            };
            BuildAndRun.clicked += () => {
                switch (CurrentBuildExportFormat) {
                    case Editor.BuildExportFormat.Folder: {
                        BuildAndRunFolder();
                        break;
                    }
                    case Editor.BuildExportFormat.UncompressedZip: {
                        BuildAndRunUncompressedZip();
                        break;
                    }
                }
            };
            OpenBuildsFolder.clicked += () => {
                string buildsFolderPath = CurrentBuildsFolderPath;
                if (!Directory.Exists(buildsFolderPath)) {
                    Directory.CreateDirectory(buildsFolderPath);
                }
                EditorUtility.RevealInFinder(buildsFolderPath + Path.AltDirectorySeparatorChar);
            };

            UpdateValues();
            SyncWebGLToggleVisuals();
            SyncAndroidToggleVisuals();
            RefreshPipelineVisibility();
        }

        private void RefreshPipelineVisibility() {
            BuildTarget target = ResolvePipelineTarget(CurrentTargetPipeline);
            bool isWebGL = target == BuildTarget.WebGL;
            bool isAndroid = target == BuildTarget.Android;

            UnityGroup.style.display = DisplayStyle.Flex;
            WebGLSettingsGroup.style.display = isWebGL ? DisplayStyle.Flex : DisplayStyle.None;
            WebGLOutputGroup.style.display = isWebGL ? DisplayStyle.Flex : DisplayStyle.None;
            AndroidOutputGroup.style.display = isAndroid ? DisplayStyle.Flex : DisplayStyle.None;
            ConfigurationGroup.style.display = DisplayStyle.Flex;
            WebGLActionsGroup.style.display = showBuildActions && isWebGL ? DisplayStyle.Flex : DisplayStyle.None;
        }

        internal static BuildTarget ResolvePipelineTarget(BuildOptimizerPipeline pipeline) {
            return pipeline switch {
                BuildOptimizerPipeline.WebGL => BuildTarget.WebGL,
                BuildOptimizerPipeline.Android => BuildTarget.Android,
                _ => EditorUserBuildSettings.activeBuildTarget
            };
        }

        private string GetBuildFilePath() {
            DirectoryInfo buildsDirectory = new(CurrentBuildsFolderPath);
            if (!buildsDirectory.Exists) {
                buildsDirectory.Create();
            }
            string fileName = CurrentBuildFileName;
            int versionHandle = buildsDirectory.GetFileSystemInfos().Count() + 1;
            fileName = fileName.Replace("#NUMBER", versionHandle.ToString());
            fileName = fileName.Replace("#VERSION", PrimeSDK.Version);
            string buildDirectoryPath = Path.Combine(buildsDirectory.FullName, fileName).NormalizePath();
            DirectoryInfo buildDirectory = new(buildDirectoryPath);
            if (!buildDirectory.Exists) {
                buildDirectory.Create();
            }
            return buildDirectory.FullName;
        }

        internal static string GetAndroidBuildFilePath() {
            string defaultBuildsFolder = Path.Combine(PackageTools.ProjectPath, Naming.Builds).NormalizePath();
            string buildsFolder = PackageTools.GetPrefsString(nameof(CurrentAndroidBuildsFolderPath), defaultBuildsFolder);
            Directory.CreateDirectory(buildsFolder);

            string defaultProjectName = PlayerSettings.productName.ToSafeFileName("build");
            string defaultFileName = $"{defaultProjectName}[#NUMBER]-primeSDK[#VERSION]";
            string fileName = PackageTools.GetPrefsString(nameof(CurrentAndroidBuildFileName), defaultFileName);
            int versionHandle = new DirectoryInfo(buildsFolder).GetFileSystemInfos().Count() + 1;
            fileName = fileName.Replace("#NUMBER", versionHandle.ToString());
            fileName = fileName.Replace("#VERSION", PrimeSDK.Version);

            string extension = GetAndroidBuildFormat() == AndroidBuildFormat.AAB ? ".aab" : ".apk";
            if (!fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) {
                fileName += extension;
            }
            return Path.Combine(buildsFolder, fileName).NormalizePath();
        }

        internal static BuildOptimizerPipeline GetCurrentPipeline() {
            string valueName = PackageTools.GetPrefsString(nameof(CurrentTargetPipeline), BuildOptimizerPipeline.CurrentActiveTarget.ToString());
            return valueName.ToEnumOrDefault<BuildOptimizerPipeline>();
        }

        internal static AndroidBuildFormat GetAndroidBuildFormat() {
            string valueName = PackageTools.GetPrefsString(nameof(CurrentAndroidBuildFormat), AndroidBuildFormat.APK.ToString());
            return valueName.ToEnumOrDefault<AndroidBuildFormat>();
        }

        internal static bool GetAndroidDevelopmentBuild() {
            return PackageTools.GetPrefsBool(nameof(CurrentAndroidDevelopmentBuild), false);
        }

        internal static bool GetAndroidScriptDebugging() {
            return PackageTools.GetPrefsBool(nameof(CurrentAndroidScriptDebugging), false);
        }

        internal static bool GetAndroidCleanOutputBeforeBuild() {
            return PackageTools.GetPrefsBool(nameof(CurrentAndroidCleanOutputBeforeBuild), true);
        }

        public BuildReport ExecuteBuildPipeline(BuildPlayerOptions buildPlayerOptions) {
            try {
                return BuildPipeline.BuildPlayer(buildPlayerOptions);
            }
            catch (Exception exception) {
                if (exception.Message.Contains("buildprogram run 6 times")) {
                    throw new InvalidOperationException($"Common Unity bug detected - try to start build again. Message: {exception.Message}");
                }
                throw exception;
            }
        }

        public void BuildFolder() {
            string buildFilePath = GetBuildFilePath();
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.WebGL, buildFilePath);
            BuildPlayerOptions buildPlayerOptions = GetBuildPlayerOptions();
            BuildReport buildReport = ExecuteBuildPipeline(buildPlayerOptions);
            if (buildReport.summary.result == BuildResult.Succeeded) {
                CleanBuildOutputArtifacts(buildFilePath);
            }
            EditorUtility.RevealInFinder(buildFilePath);
        }

        public void BuildUncompressedZip() {
            string buildFilePath = GetBuildFilePath();
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.WebGL, buildFilePath);
            BuildPlayerOptions buildPlayerOptions = GetBuildPlayerOptions();
            BuildReport buildReport = ExecuteBuildPipeline(buildPlayerOptions);
            if (buildReport.summary.result == BuildResult.Succeeded) {
                CleanBuildOutputArtifacts(buildFilePath);
                string zipPath = CompressFolder(buildFilePath, true);
                EditorUtility.RevealInFinder(zipPath);
            }
        }

        public void BuildAndRunFolder() {
            string buildFilePath = GetBuildFilePath();
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.WebGL, buildFilePath);
            BuildPlayerOptions buildPlayerOptions = GetBuildPlayerOptions();
            buildPlayerOptions.options |= UnityEditor.BuildOptions.AutoRunPlayer;
            BuildReport buildReport = ExecuteBuildPipeline(buildPlayerOptions);
            if (buildReport.summary.result == BuildResult.Succeeded) {
                CleanBuildOutputArtifacts(buildFilePath);
            }
        }

        public void BuildAndRunUncompressedZip() {
            string buildFilePath = GetBuildFilePath();
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.WebGL, buildFilePath);
            BuildPlayerOptions buildPlayerOptions = GetBuildPlayerOptions();
            buildPlayerOptions.options |= UnityEditor.BuildOptions.AutoRunPlayer;
            BuildReport buildReport = ExecuteBuildPipeline(buildPlayerOptions);
            if (buildReport.summary.result == BuildResult.Succeeded) {
                CleanBuildOutputArtifacts(buildFilePath);
                CompressFolder(buildFilePath, false);
            }
        }

        private static void CleanBuildOutputArtifacts(string folderPath) {
            foreach (string filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)) {
                if (!IsBuildOutputArtifactIgnored(filePath)) {
                    continue;
                }
                File.Delete(filePath);
            }
        }

        private static bool IsBuildOutputArtifactIgnored(string filePath) {
            return Path.GetFileName(filePath).EndsWith("~", StringComparison.Ordinal);
        }

        private static void SavePlayerSettings() {
            AssetDatabase.SaveAssets();
        }

        private void SyncWebGLToggleVisuals() {
            SyncToggleVisual(NameFilesAsHashes);
            SyncToggleVisual(DataCaching);
            SyncToggleVisual(DecompressionFallback);
        }

        private void SyncAndroidToggleVisuals() {
            SyncToggleVisual(AndroidDevelopmentBuild);
            SyncToggleVisual(AndroidScriptDebugging);
            SyncToggleVisual(AndroidCleanOutputBeforeBuild);
        }

        private static void SyncToggleVisual(Toggle toggle) {
            VisualElement checkmark = toggle.Q<VisualElement>("unity-checkmark");
            if (checkmark == null) {
                return;
            }

            checkmark.style.unityBackgroundImageTintColor = toggle.value ? CheckmarkVisible : CheckmarkInvisible;
        }

        private static string CompressFolder(string folderPath, bool deleteFolder) {
            string zipPath = folderPath + ".zip";
            if (File.Exists(zipPath)) {
                File.Delete(zipPath);
            }
            using ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            foreach (string filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)) {
                if (IsBuildOutputArtifactIgnored(filePath)) {
                    continue;
                }
                string relativePath = Path.GetRelativePath(folderPath, filePath);
                relativePath = relativePath.NormalizePath();
                archive.CreateEntryFromFile(filePath, relativePath, System.IO.Compression.CompressionLevel.NoCompression);
            }
            if (deleteFolder == true) {
                Directory.Delete(folderPath, true);
            }
            return zipPath;
        }

        private static BuildPlayerOptions GetBuildPlayerOptions() {
            Type defaultBuildMethodsType = typeof(BuildPlayerWindow.DefaultBuildMethods);
            MethodInfo getBuildPlayerOptionsInternalMethod = defaultBuildMethodsType.GetMethod(
                name: "GetBuildPlayerOptionsInternal",
                bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new Type[] { typeof(bool), typeof(BuildPlayerOptions) },
                modifiers: null
            );
            if (getBuildPlayerOptionsInternalMethod != null) {
                // Invoke the method with 'askForBuildLocation' set to false.
                BuildPlayerOptions defaultOptions = new();
                return (BuildPlayerOptions)getBuildPlayerOptionsInternalMethod.Invoke(
                    null, new object[] { false, defaultOptions }
                );
            }
            throw new InvalidOperationException("Failed to get BuildPlayerOptions");
        }

    }

}
