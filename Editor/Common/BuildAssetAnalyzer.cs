using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.Editor {

    internal enum BuildAssetCategory {
        Texture,
        Audio,
        Model,
        Material,
        Shader,
        Font,
        Other
    }

    [Flags]
    internal enum OptimizerTextureExtensions {
        PNG = 1 << 0,
        JPG = 1 << 1,
        TIF = 1 << 2,
        TGA = 1 << 3,
        PSD = 1 << 4,
        EXR = 1 << 5,
        BMP = 1 << 6,
        PSB = 1 << 7,
        SpriteAtlas = 1 << 8,
        JPEG = 1 << 9,
        All = PNG | JPG | TIF | TGA | PSD | EXR | BMP | PSB | SpriteAtlas | JPEG
    }

    [Flags]
    internal enum OptimizerAudioExtensions {
        MP3 = 1 << 0,
        OGG = 1 << 1,
        WAV = 1 << 2,
        All = MP3 | OGG | WAV
    }

    [Flags]
    internal enum OptimizerTextureImporterTypes {
        Default = 1 << 0,
        NormalMap = 1 << 1,
        GUI = 1 << 2,
        Sprite = 1 << 3,
        Cursor = 1 << 4,
        Cookie = 1 << 5,
        Lightmap = 1 << 6,
        DirectionalLightmap = 1 << 7,
        Shadowmask = 1 << 8,
        SingleChannel = 1 << 9,
        All = Default | NormalMap | GUI | Sprite | Cursor | Cookie | Lightmap | DirectionalLightmap | Shadowmask | SingleChannel
    }

    [Flags]
    internal enum OptimizerPlatforms {
        Windows = 1 << 0,
        DedicatedServer = 1 << 1,
        Android = 1 << 2,
        iPhone = 1 << 3,
        WebGL = 1 << 4,
        All = Windows | DedicatedServer | Android | iPhone | WebGL
    }

    internal sealed class BuildAssetInfo {

        public string Path;
        public long SizeBytes;
        public string TypeName;
        public BuildAssetCategory Category;

    }

    internal sealed class BuildAssetAnalysis {

        public List<BuildAssetInfo> Assets = new();
        public long TotalSizeBytes;

        public string StatusMessage {
            get {
                return $"Assets: {Assets.Count} | Estimated source size: {BuildAssetAnalyzer.FormatBytes(TotalSizeBytes)}";
            }
        }

    }

    internal sealed class BuildAssetOptimizationCandidate {

        public BuildAssetInfo Asset;
        public bool Selected = true;
        public string Reason = string.Empty;

    }

    internal sealed class BuildAssetOptimizationPlan {

        public List<BuildAssetOptimizationCandidate> Textures = new();
        public List<BuildAssetOptimizationCandidate> Audio = new();
        public List<BuildAssetOptimizationCandidate> Models = new();
        public List<BuildAssetOptimizationCandidate> Materials = new();

        public IEnumerable<BuildAssetOptimizationCandidate> AllCandidates {
            get {
                return Textures.Concat(Audio).Concat(Models).Concat(Materials);
            }
        }

        public IEnumerable<BuildAssetOptimizationCandidate> SelectedCandidates {
            get {
                return AllCandidates.Where(candidate => candidate.Selected);
            }
        }

    }

    internal sealed class BuildAssetOptimizationProfile {

        public bool OptimizeTextures = true;
        public bool OptimizeAudio = true;
        public bool OptimizeModels = true;
        public bool OptimizeMaterials = true;

        public bool TextureGenerateMipMaps = false;
        public bool TextureOverrideGenerateMipMaps = true;
        public int TextureMaxSize = 1024;
        public bool TextureOverrideMaxSize = true;
        public TextureResizeAlgorithm TextureResizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        public bool TextureOverrideResizeAlgorithm = false;
        public TextureImporterFormat TextureFormat = TextureImporterFormat.Automatic;
        public bool TextureOverrideFormat = false;
        public TextureImporterCompression TextureCompression = TextureImporterCompression.Compressed;
        public bool TextureOverrideCompression = true;
        public bool TextureUseCrunchCompression = true;
        public bool TextureOverrideCrunchCompression = true;
        public int TextureCompressionQuality = 50;
        public bool TextureOverrideCompressionQuality = true;
        public bool TextureDisablePlatformOverrides = false;
        public OptimizerPlatforms TexturePlatformsToDisable = OptimizerPlatforms.WebGL;
        public bool TextureResizeSourceFiles = false;
        public bool TextureSkipResizeIfReadWrite = true;
        public OptimizerTextureExtensions TextureExtensions = OptimizerTextureExtensions.All;
        public OptimizerTextureImporterTypes TextureImporterTypes = OptimizerTextureImporterTypes.All;
        public bool TextureExcludeFolders = true;
        public List<string> TextureExcludedFolders = new() { "WebGLTemplates" };
        public bool TextureIncludeFolders = false;
        public List<string> TextureIncludedFolders = new();

        public bool AudioForceToMono = false;
        public bool AudioOverrideForceToMono = false;
        public bool AudioLoadInBackground = true;
        public bool AudioOverrideLoadInBackground = true;
        public bool AudioAmbisonic = false;
        public bool AudioOverrideAmbisonic = false;
        public bool AudioPreloadAudioData = false;
        public bool AudioOverridePreloadAudioData = false;
        public AudioClipLoadType AudioLoadType = AudioClipLoadType.DecompressOnLoad;
        public bool AudioOverrideLoadType = true;
        public AudioCompressionFormat AudioCompressionFormat = AudioCompressionFormat.Vorbis;
        public bool AudioOverrideCompressionFormat = true;
        public AudioSampleRateSetting AudioSampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        public bool AudioOverrideSampleRate = false;
        public float AudioQuality = 0.6f;
        public bool AudioOverrideQuality = true;
        public OptimizerAudioExtensions AudioExtensions = OptimizerAudioExtensions.All;

        public ModelImporterMeshCompression ModelMeshCompression = ModelImporterMeshCompression.Medium;
        public bool ModelOverrideMeshCompression = true;
        public bool ModelDisableReadWrite = true;
        public bool ModelOptimizeMesh = true;

        public bool MaterialOverrideGPUInstancing = true;
        public bool MaterialEnableGPUInstancing = false;
        public bool MaterialChangeShaders = false;
        public string MaterialOldShader = "Standard";
        public string MaterialNewShader = "Legacy Shaders/Diffuse";

    }

    internal sealed class BuildAssetOptimizationResult {

        public int TextureCount;
        public int AudioCount;
        public int ModelCount;
        public int MaterialCount;
        public int SkippedCount;

        public string StatusMessage {
            get {
                return $"Optimized textures: {TextureCount}, audio: {AudioCount}, models: {ModelCount}, materials: {MaterialCount}, skipped: {SkippedCount}";
            }
        }

    }

    internal static class BuildAssetAnalyzer {

        private const string WebGLPlatformName = "WebGL";
        private const int LegacyTextureMaxSizeWebGL = 2048;
        private const int LegacyTextureCompressionQuality = 75;
        private const float LegacyAudioCompressionQuality = 0.6f;

        private static readonly Dictionary<OptimizerPlatforms, string> PlatformNames = new() {
            { OptimizerPlatforms.WebGL, "WebGL" },
            { OptimizerPlatforms.Android, "Android" },
            { OptimizerPlatforms.iPhone, "iPhone" },
            { OptimizerPlatforms.Windows, "Standalone" },
            { OptimizerPlatforms.DedicatedServer, "Server" }
        };

        public static BuildAssetAnalysis AnalyzeEnabledBuildScenes() {
            string[] scenes = GetEnabledBuildScenes();
            if (scenes.Length == 0) {
                throw new InvalidOperationException("No enabled scenes in Build Settings.");
            }

            string[] dependencies;
            try {
                EditorUtility.DisplayProgressBar("PrimeSDK Build Analysis", "Collecting scene dependencies...", 0.15f);
                dependencies = AssetDatabase.GetDependencies(scenes, true);
            }
            finally {
                EditorUtility.ClearProgressBar();
            }

            BuildAssetAnalysis analysis = new();
            HashSet<string> uniquePaths = new(dependencies ?? Array.Empty<string>());
            int index = 0;
            foreach (string path in uniquePaths) {
                index++;
                if (index % 250 == 0) {
                    EditorUtility.DisplayProgressBar("PrimeSDK Build Analysis", "Analyzing assets...", index / Mathf.Max(1f, uniquePaths.Count));
                }

                if (!IsProjectAssetPath(path)) {
                    continue;
                }

                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                long sizeBytes = GetFileSizeForAssetPath(path);
                BuildAssetInfo assetInfo = new() {
                    Path = path,
                    SizeBytes = sizeBytes,
                    TypeName = assetType != null ? assetType.Name : "Unknown",
                    Category = Categorize(path, assetType)
                };
                analysis.Assets.Add(assetInfo);
                analysis.TotalSizeBytes += sizeBytes;
            }

            EditorUtility.ClearProgressBar();
            analysis.Assets.Sort((left, right) => right.SizeBytes.CompareTo(left.SizeBytes));
            SaveReport(analysis);
            return analysis;
        }

        public static BuildAssetOptimizationPlan CreateOptimizationPlan(BuildAssetAnalysis analysis, BuildAssetOptimizationProfile profile) {
            if (analysis == null) {
                return new BuildAssetOptimizationPlan();
            }

            BuildAssetOptimizationPlan plan = new();
            foreach (BuildAssetInfo asset in analysis.Assets) {
                if (!asset.Path.StartsWith("Assets/", StringComparison.Ordinal)) {
                    continue;
                }

                BuildAssetOptimizationCandidate candidate = new() {
                    Asset = asset
                };

                switch (asset.Category) {
                    case BuildAssetCategory.Texture: {
                        if (profile.OptimizeTextures && IsTextureCandidate(asset.Path, profile, out string reason)) {
                            candidate.Reason = reason;
                            plan.Textures.Add(candidate);
                        }
                        break;
                    }
                    case BuildAssetCategory.Audio: {
                        if (profile.OptimizeAudio && IsAudioCandidate(asset.Path, profile, out string reason)) {
                            candidate.Reason = reason;
                            plan.Audio.Add(candidate);
                        }
                        break;
                    }
                    case BuildAssetCategory.Model: {
                        if (profile.OptimizeModels) {
                            candidate.Reason = "Model importer";
                            plan.Models.Add(candidate);
                        }
                        break;
                    }
                    case BuildAssetCategory.Material: {
                        if (profile.OptimizeMaterials) {
                            candidate.Reason = "Material";
                            plan.Materials.Add(candidate);
                        }
                        break;
                    }
                }
            }

            return plan;
        }

        public static BuildAssetOptimizationResult OptimizeEnabledBuildSceneAssets() {
            BuildAssetOptimizationProfile profile = new() {
                TextureMaxSize = LegacyTextureMaxSizeWebGL,
                TextureCompressionQuality = LegacyTextureCompressionQuality,
                AudioForceToMono = true,
                AudioOverrideForceToMono = true,
                AudioPreloadAudioData = true,
                AudioOverridePreloadAudioData = true,
                AudioLoadType = AudioClipLoadType.CompressedInMemory,
                AudioQuality = LegacyAudioCompressionQuality,
                ModelDisableReadWrite = true,
                ModelOptimizeMesh = true,
                OptimizeMaterials = false
            };
            BuildAssetAnalysis analysis = AnalyzeEnabledBuildScenes();
            BuildAssetOptimizationPlan plan = CreateOptimizationPlan(analysis, profile);
            return ApplyOptimization(plan, profile);
        }

        public static BuildAssetOptimizationResult ApplyOptimization(BuildAssetOptimizationPlan plan, BuildAssetOptimizationProfile profile) {
            BuildAssetOptimizationResult result = new();
            List<BuildAssetOptimizationCandidate> candidates = plan.SelectedCandidates.ToList();

            try {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < candidates.Count; i++) {
                    BuildAssetOptimizationCandidate candidate = candidates[i];
                    string path = candidate.Asset.Path;
                    EditorUtility.DisplayProgressBar("PrimeSDK Asset Optimization", path, i / Mathf.Max(1f, candidates.Count));

                    bool changed = candidate.Asset.Category switch {
                        BuildAssetCategory.Texture => OptimizeTexture(path, profile),
                        BuildAssetCategory.Audio => OptimizeAudio(path, profile),
                        BuildAssetCategory.Model => OptimizeModel(path, profile),
                        BuildAssetCategory.Material => OptimizeMaterial(path, profile),
                        _ => false
                    };

                    if (!changed) {
                        result.SkippedCount++;
                        continue;
                    }

                    switch (candidate.Asset.Category) {
                        case BuildAssetCategory.Texture: result.TextureCount++; break;
                        case BuildAssetCategory.Audio: result.AudioCount++; break;
                        case BuildAssetCategory.Model: result.ModelCount++; break;
                        case BuildAssetCategory.Material: result.MaterialCount++; break;
                    }
                }
            }
            finally {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            return result;
        }

        public static string FormatBytes(long bytes) {
            string[] units = { "B", "KB", "MB", "GB" };
            double value = bytes;
            int unitIndex = 0;
            while (value >= 1024 && unitIndex < units.Length - 1) {
                value /= 1024;
                unitIndex++;
            }
            return $"{value:0.##} {units[unitIndex]}";
        }

        public static long GetCandidatesSize(IEnumerable<BuildAssetOptimizationCandidate> candidates) {
            return candidates?.Sum(candidate => candidate.Asset.SizeBytes) ?? 0;
        }

        private static bool OptimizeTexture(string path, BuildAssetOptimizationProfile profile) {
            bool changed = false;

            if (Path.GetExtension(path).Equals(".spriteatlasv2", StringComparison.OrdinalIgnoreCase)) {
                changed |= OptimizeSpriteAtlas(path, profile);
                return changed;
            }

            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) {
                return false;
            }

            if (profile.TextureOverrideGenerateMipMaps && importer.mipmapEnabled != profile.TextureGenerateMipMaps) {
                importer.mipmapEnabled = profile.TextureGenerateMipMaps;
                changed = true;
            }
            if (profile.TextureOverrideMaxSize && importer.maxTextureSize != profile.TextureMaxSize) {
                importer.maxTextureSize = profile.TextureMaxSize;
                changed = true;
            }
            if (profile.TextureOverrideCompression && importer.textureCompression != profile.TextureCompression) {
                importer.textureCompression = profile.TextureCompression;
                changed = true;
            }
            if (profile.TextureOverrideCrunchCompression && !(profile.TextureSkipResizeIfReadWrite && importer.isReadable) && importer.crunchedCompression != profile.TextureUseCrunchCompression) {
                importer.crunchedCompression = profile.TextureUseCrunchCompression;
                changed = true;
            }
            if (profile.TextureOverrideCompressionQuality && importer.compressionQuality != profile.TextureCompressionQuality) {
                importer.compressionQuality = profile.TextureCompressionQuality;
                changed = true;
            }

            TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
            if (profile.TextureOverrideResizeAlgorithm && defaultSettings.resizeAlgorithm != profile.TextureResizeAlgorithm) {
                defaultSettings.resizeAlgorithm = profile.TextureResizeAlgorithm;
                importer.SetPlatformTextureSettings(defaultSettings);
                changed = true;
            }
            if (profile.TextureOverrideFormat && defaultSettings.format != profile.TextureFormat) {
                defaultSettings.format = profile.TextureFormat;
                importer.SetPlatformTextureSettings(defaultSettings);
                changed = true;
            }

            if (profile.TextureDisablePlatformOverrides) {
                changed |= DisableTexturePlatformOverrides(importer, profile.TexturePlatformsToDisable);
            }

            if (profile.TextureResizeSourceFiles) {
                changed |= ResizeSourceTextureIfNeeded(path, importer, profile);
            }

            if (changed) {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
            return changed;
        }

        private static bool OptimizeSpriteAtlas(string path, BuildAssetOptimizationProfile profile) {
            if (AssetImporter.GetAtPath(path) is not SpriteAtlasImporter importer) {
                return false;
            }

            bool changed = false;
            TextureImporterPlatformSettings platformSettings = importer.GetPlatformSettings("DefaultTexturePlatform");
            SpriteAtlasTextureSettings textureSettings = importer.textureSettings;

            if (profile.TextureOverrideGenerateMipMaps && textureSettings.generateMipMaps != profile.TextureGenerateMipMaps) {
                textureSettings.generateMipMaps = profile.TextureGenerateMipMaps;
                changed = true;
            }
            if (profile.TextureOverrideMaxSize && platformSettings.maxTextureSize != profile.TextureMaxSize) {
                platformSettings.maxTextureSize = profile.TextureMaxSize;
                changed = true;
            }
            if (profile.TextureOverrideCompression && platformSettings.textureCompression != profile.TextureCompression) {
                platformSettings.textureCompression = profile.TextureCompression;
                changed = true;
            }
            if (profile.TextureOverrideCrunchCompression && !(profile.TextureSkipResizeIfReadWrite && textureSettings.readable) && platformSettings.crunchedCompression != profile.TextureUseCrunchCompression) {
                platformSettings.crunchedCompression = profile.TextureUseCrunchCompression;
                changed = true;
            }
            if (profile.TextureOverrideCompressionQuality && platformSettings.compressionQuality != profile.TextureCompressionQuality) {
                platformSettings.compressionQuality = profile.TextureCompressionQuality;
                changed = true;
            }
            if (profile.TextureOverrideResizeAlgorithm && platformSettings.resizeAlgorithm != profile.TextureResizeAlgorithm) {
                platformSettings.resizeAlgorithm = profile.TextureResizeAlgorithm;
                changed = true;
            }
            if (profile.TextureOverrideFormat && platformSettings.format != profile.TextureFormat) {
                platformSettings.format = profile.TextureFormat;
                changed = true;
            }
            if (profile.TextureDisablePlatformOverrides) {
                changed |= DisableTexturePlatformOverrides(importer, profile.TexturePlatformsToDisable);
            }

            if (changed) {
                importer.textureSettings = textureSettings;
                importer.SetPlatformSettings(platformSettings);
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
            return changed;
        }

        private static bool OptimizeAudio(string path, BuildAssetOptimizationProfile profile) {
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer) {
                return false;
            }

            bool changed = false;
            if (profile.AudioOverrideForceToMono && importer.forceToMono != profile.AudioForceToMono) {
                importer.forceToMono = profile.AudioForceToMono;
                changed = true;
            }
            if (profile.AudioOverrideAmbisonic && importer.ambisonic != profile.AudioAmbisonic) {
                importer.ambisonic = profile.AudioAmbisonic;
                changed = true;
            }
            if (profile.AudioOverrideLoadInBackground && importer.loadInBackground != profile.AudioLoadInBackground) {
                importer.loadInBackground = profile.AudioLoadInBackground;
                changed = true;
            }

            AudioImporterSampleSettings defaultSettings = importer.defaultSampleSettings;
            changed |= ApplyAudioSampleSettings(ref defaultSettings, profile);
            importer.defaultSampleSettings = defaultSettings;

            if (changed) {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
            return changed;
        }

        private static bool ApplyAudioSampleSettings(ref AudioImporterSampleSettings settings, BuildAssetOptimizationProfile profile) {
            bool changed = false;
            if (profile.AudioOverrideLoadType && settings.loadType != profile.AudioLoadType) {
                settings.loadType = profile.AudioLoadType;
                changed = true;
            }
            if (profile.AudioOverridePreloadAudioData && settings.preloadAudioData != profile.AudioPreloadAudioData) {
                settings.preloadAudioData = profile.AudioPreloadAudioData;
                changed = true;
            }
            if (profile.AudioOverrideCompressionFormat && settings.compressionFormat != profile.AudioCompressionFormat) {
                settings.compressionFormat = profile.AudioCompressionFormat;
                changed = true;
            }
            if (profile.AudioOverrideQuality && Math.Abs(settings.quality - profile.AudioQuality) > 0.001f) {
                settings.quality = profile.AudioQuality;
                changed = true;
            }
            if (profile.AudioOverrideSampleRate && settings.sampleRateSetting != profile.AudioSampleRateSetting) {
                settings.sampleRateSetting = profile.AudioSampleRateSetting;
                changed = true;
            }
            return changed;
        }

        private static bool OptimizeModel(string path, BuildAssetOptimizationProfile profile) {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer) {
                return false;
            }

            bool changed = false;
            if (profile.ModelOverrideMeshCompression && importer.meshCompression != profile.ModelMeshCompression) {
                importer.meshCompression = profile.ModelMeshCompression;
                changed = true;
            }
            if (profile.ModelDisableReadWrite && importer.isReadable) {
                importer.isReadable = false;
                changed = true;
            }
            if (profile.ModelOptimizeMesh) {
                changed |= SetImporterProperty(importer, "optimizeMesh", true);
                changed |= SetImporterProperty(importer, "optimizeMeshPolygons", true);
                changed |= SetImporterProperty(importer, "optimizeMeshVertices", true);
            }

            if (changed) {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
            return changed;
        }

        private static bool OptimizeMaterial(string path, BuildAssetOptimizationProfile profile) {
            if (AssetDatabase.LoadMainAssetAtPath(path) is not Material material) {
                return false;
            }

            bool changed = false;
            if (profile.MaterialOverrideGPUInstancing && material.enableInstancing != profile.MaterialEnableGPUInstancing) {
                material.enableInstancing = profile.MaterialEnableGPUInstancing;
                changed = true;
            }
            if (profile.MaterialChangeShaders && material.shader != null && material.shader.name == profile.MaterialOldShader) {
                Shader shader = Shader.Find(profile.MaterialNewShader);
                if (shader == null) {
                    Logger.CreateError(nameof(BuildAssetAnalyzer), "Shader not found", profile.MaterialNewShader);
                }
                else {
                    material.shader = shader;
                    changed = true;
                }
            }

            if (changed) {
                EditorUtility.SetDirty(material);
                AssetDatabase.ImportAsset(path);
            }
            return changed;
        }

        private static bool IsTextureCandidate(string path, BuildAssetOptimizationProfile profile, out string reason) {
            reason = string.Empty;
            string normalizedPath = path.Replace('\\', '/');
            string extension = Path.GetExtension(normalizedPath).ToLowerInvariant();
            if (!profile.TextureExtensions.HasFlag(ConvertTextureExtension(extension))) {
                return false;
            }

            if (profile.TextureExcludeFolders && profile.TextureExcludedFolders.Any(folder => IsInFolder(normalizedPath, folder))) {
                return false;
            }
            if (profile.TextureIncludeFolders && !profile.TextureIncludedFolders.Any(folder => IsInFolder(normalizedPath, folder))) {
                return false;
            }

            if (extension == ".spriteatlasv2") {
                reason = "Sprite atlas";
                return true;
            }

            if (AssetImporter.GetAtPath(path) is TextureImporter importer && !profile.TextureImporterTypes.HasFlag(ConvertImporterType(importer.textureType))) {
                return false;
            }

            reason = "Texture importer";
            return true;
        }

        private static bool IsAudioCandidate(string path, BuildAssetOptimizationProfile profile, out string reason) {
            reason = string.Empty;
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (!profile.AudioExtensions.HasFlag(ConvertAudioExtension(extension))) {
                return false;
            }
            reason = "Audio importer";
            return true;
        }

        private static bool IsInFolder(string assetPath, string folder) {
            if (string.IsNullOrWhiteSpace(folder)) {
                return false;
            }
            string normalizedFolder = folder.Replace('\\', '/').Trim('/');
            if (!normalizedFolder.StartsWith("Assets/", StringComparison.Ordinal)) {
                normalizedFolder = $"Assets/{normalizedFolder}";
            }
            return assetPath.StartsWith(normalizedFolder + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static OptimizerTextureExtensions ConvertTextureExtension(string extension) {
            return extension switch {
                ".png" => OptimizerTextureExtensions.PNG,
                ".jpg" => OptimizerTextureExtensions.JPG,
                ".jpeg" => OptimizerTextureExtensions.JPEG,
                ".tif" => OptimizerTextureExtensions.TIF,
                ".tiff" => OptimizerTextureExtensions.TIF,
                ".tga" => OptimizerTextureExtensions.TGA,
                ".psd" => OptimizerTextureExtensions.PSD,
                ".psb" => OptimizerTextureExtensions.PSB,
                ".exr" => OptimizerTextureExtensions.EXR,
                ".bmp" => OptimizerTextureExtensions.BMP,
                ".spriteatlasv2" => OptimizerTextureExtensions.SpriteAtlas,
                _ => 0
            };
        }

        private static OptimizerAudioExtensions ConvertAudioExtension(string extension) {
            return extension switch {
                ".mp3" => OptimizerAudioExtensions.MP3,
                ".ogg" => OptimizerAudioExtensions.OGG,
                ".wav" => OptimizerAudioExtensions.WAV,
                _ => 0
            };
        }

        private static OptimizerTextureImporterTypes ConvertImporterType(TextureImporterType type) {
            return type switch {
                TextureImporterType.Default => OptimizerTextureImporterTypes.Default,
                TextureImporterType.Cookie => OptimizerTextureImporterTypes.Cookie,
                TextureImporterType.Cursor => OptimizerTextureImporterTypes.Cursor,
                TextureImporterType.DirectionalLightmap => OptimizerTextureImporterTypes.DirectionalLightmap,
                TextureImporterType.GUI => OptimizerTextureImporterTypes.GUI,
                TextureImporterType.Lightmap => OptimizerTextureImporterTypes.Lightmap,
                TextureImporterType.NormalMap => OptimizerTextureImporterTypes.NormalMap,
                TextureImporterType.Shadowmask => OptimizerTextureImporterTypes.Shadowmask,
                TextureImporterType.SingleChannel => OptimizerTextureImporterTypes.SingleChannel,
                TextureImporterType.Sprite => OptimizerTextureImporterTypes.Sprite,
                _ => OptimizerTextureImporterTypes.Default
            };
        }

        private static bool DisableTexturePlatformOverrides(TextureImporter importer, OptimizerPlatforms platforms) {
            bool changed = false;
            foreach (KeyValuePair<OptimizerPlatforms, string> platform in PlatformNames) {
                if (!platforms.HasFlag(platform.Key)) {
                    continue;
                }

                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform.Value);
                if (settings.overridden) {
                    settings.overridden = false;
                    importer.SetPlatformTextureSettings(settings);
                    changed = true;
                }
            }
            return changed;
        }

        private static bool DisableTexturePlatformOverrides(SpriteAtlasImporter importer, OptimizerPlatforms platforms) {
            bool changed = false;
            foreach (KeyValuePair<OptimizerPlatforms, string> platform in PlatformNames) {
                if (!platforms.HasFlag(platform.Key)) {
                    continue;
                }

                TextureImporterPlatformSettings settings = importer.GetPlatformSettings(platform.Value);
                if (settings.overridden) {
                    settings.overridden = false;
                    importer.SetPlatformSettings(settings);
                    changed = true;
                }
            }
            return changed;
        }

        private static bool ResizeSourceTextureIfNeeded(string path, TextureImporter importer, BuildAssetOptimizationProfile profile) {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension is not ".png" and not ".jpg" and not ".jpeg") {
                return false;
            }
            if (profile.TextureSkipResizeIfReadWrite && importer.isReadable) {
                return false;
            }

            Texture2D source = LoadTexture(path);
            if (source == null) {
                return false;
            }

            try {
                if (!TryCalculateResize(source.width, source.height, importer.maxTextureSize, out int width, out int height)) {
                    return false;
                }

                RenderTexture renderTexture = RenderTexture.GetTemporary(width, height);
                RenderTexture previous = RenderTexture.active;
                try {
                    RenderTexture.active = renderTexture;
                    Graphics.Blit(source, renderTexture);
                    Texture2D resized = new(width, height, TextureFormat.RGBA32, false);
                    resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    resized.Apply();

                    byte[] bytes = extension == ".png" ? resized.EncodeToPNG() : resized.EncodeToJPG(profile.TextureCompressionQuality);
                    File.WriteAllBytes(path, bytes);
                    UnityEngine.Object.DestroyImmediate(resized);
                    return true;
                }
                finally {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(renderTexture);
                }
            }
            finally {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static bool TryCalculateResize(int sourceWidth, int sourceHeight, int maxSize, out int width, out int height) {
            width = sourceWidth;
            height = sourceHeight;
            if (sourceWidth < 4 || sourceHeight < 4) {
                return false;
            }

            if (maxSize > 0 && (sourceWidth > maxSize || sourceHeight > maxSize)) {
                float ratio = sourceWidth >= sourceHeight ? maxSize / (float)sourceWidth : maxSize / (float)sourceHeight;
                width = Mathf.RoundToInt(sourceWidth * ratio);
                height = Mathf.RoundToInt(sourceHeight * ratio);
            }

            width = Mathf.Max(4, Mathf.CeilToInt(width / 4f) * 4);
            height = Mathf.Max(4, Mathf.CeilToInt(height / 4f) * 4);
            return width != sourceWidth || height != sourceHeight;
        }

        private static Texture2D LoadTexture(string path) {
            if (!File.Exists(path)) {
                return null;
            }
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new(2, 2);
            return texture.LoadImage(fileData) ? texture : null;
        }

        private static bool SetImporterProperty<T>(AssetImporter importer, string propertyName, T value) {
            PropertyInfo propertyInfo = importer.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null || propertyInfo.CanWrite == false || propertyInfo.PropertyType != typeof(T)) {
                return false;
            }

            object currentValue = propertyInfo.GetValue(importer);
            if (Equals(currentValue, value)) {
                return false;
            }

            propertyInfo.SetValue(importer, value);
            return true;
        }

        private static BuildAssetCategory Categorize(string path, Type assetType) {
            if (assetType == typeof(Texture2D) || assetType == typeof(Sprite) || Path.GetExtension(path).Equals(".spriteatlasv2", StringComparison.OrdinalIgnoreCase)) {
                return BuildAssetCategory.Texture;
            }
            if (assetType == typeof(AudioClip)) {
                return BuildAssetCategory.Audio;
            }
            if (assetType == typeof(Material)) {
                return BuildAssetCategory.Material;
            }
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension is ".fbx" or ".obj" or ".dae" or ".blend") {
                return BuildAssetCategory.Model;
            }
            if (assetType != null && (assetType.Name == "Shader" || assetType.Name == "ComputeShader")) {
                return BuildAssetCategory.Shader;
            }
            if (extension is ".shader" or ".cginc" or ".hlsl" or ".compute") {
                return BuildAssetCategory.Shader;
            }
            if (assetType != null && (assetType.Name == "Font" || assetType.Name == "TMP_FontAsset" || assetType.Name == "FontAsset")) {
                return BuildAssetCategory.Font;
            }
            if (extension is ".ttf" or ".otf" or ".fnt" or ".fontsettings") {
                return BuildAssetCategory.Font;
            }
            return BuildAssetCategory.Other;
        }

        private static bool IsProjectAssetPath(string path) {
            return string.IsNullOrEmpty(path) == false
                && (path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("Packages/", StringComparison.Ordinal));
        }

        private static long GetFileSizeForAssetPath(string path) {
            string fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath)) {
                return new FileInfo(fullPath).Length;
            }
            return 0;
        }

        private static string[] GetEnabledBuildScenes() {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled && string.IsNullOrEmpty(scene.path) == false)
                .Select(scene => scene.path)
                .ToArray();
        }

        private static void SaveReport(BuildAssetAnalysis analysis) {
            try {
                string reportsFolder = Path.Combine(PackageTools.ProjectPath, "BuildReports").NormalizePath();
                Directory.CreateDirectory(reportsFolder);
                string reportPath = Path.Combine(reportsFolder, $"PrimeSDK_BuildAnalysis_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt").NormalizePath();
                StringBuilder builder = new();
                builder.AppendLine("PrimeSDK Build Analysis");
                builder.AppendLine(analysis.StatusMessage);
                builder.AppendLine();
                foreach (BuildAssetInfo asset in analysis.Assets.Take(200)) {
                    builder.AppendLine($"{FormatBytes(asset.SizeBytes),10} | {asset.Category,-8} | {asset.Path}");
                }
                File.WriteAllText(reportPath, builder.ToString());
            }
            catch (Exception exception) {
                Logger.CreateWarning(nameof(BuildAssetAnalyzer), "Failed to save build analysis report", exception.Message);
            }
        }

    }

}
