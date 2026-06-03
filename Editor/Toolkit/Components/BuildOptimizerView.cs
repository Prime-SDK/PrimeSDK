using PrimeGames.SDK.Common;
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
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.Editor {

    internal sealed class BuildOptimizerView : VisualElement {

        private enum OptimizerStep {
            Overview,
            Textures,
            Audio,
            Models,
            Materials,
            Apply
        }

        private const string CurrentBuildsFolderPathKey = "CurrentBuildsFolderPath";
        private const string CurrentBuildFileNameKey = "CurrentBuildFileName";
        private const string CurrentBuildExportFormatKey = "CurrentBuildExportFormat";
        private const int TopAssetCount = 12;
        private const int CandidatePreviewCount = 80;
        private const float SettingLabelWidth = 190f;
        private const float SettingFieldWidth = 360f;
        private const float SettingTextFieldWidth = 520f;
        private const float SettingValueFieldWidth = 64f;
        private static readonly Color Accent = new(1f, 0.49f, 0.08f);
        private static readonly Color PanelBackground = new(0.055f, 0.058f, 0.064f);
        private static readonly Color Muted = new(0.68f, 0.68f, 0.68f);

        private readonly BuildAssetOptimizationProfile profile = new();
        private readonly Dictionary<OptimizerStep, Button> stepButtons = new();

        private Label statusLabel;
        private VisualElement summaryCards;
        private VisualElement categoryContainer;
        private VisualElement topAssetsContainer;
        private VisualElement wizardContainer;
        private Label wizardStatusLabel;

        private BuildAssetAnalysis currentAnalysis;
        private BuildAssetOptimizationPlan currentPlan = new();
        private long lastBuildSizeBytes;
        private OptimizerStep currentStep = OptimizerStep.Overview;

        public BuildOptimizerView() {
            style.flexGrow = 1;
            BuildLayout();
            RebuildPlan();
            RenderEmptyState();
            RenderWizard();
        }

        private void BuildLayout() {
            ScrollView scrollView = new(ScrollViewMode.Vertical) {
                style = {
                    flexGrow = 1
                }
            };
            Add(scrollView);

            VisualElement content = new() {
                style = {
                    paddingLeft = 18,
                    paddingRight = 18,
                    paddingTop = 14,
                    paddingBottom = 18
                }
            };
            scrollView.Add(content);

            VisualElement header = CreatePanel();
            header.style.minHeight = 110;
            header.style.borderTopColor = Accent;
            header.style.borderTopWidth = 3;
            content.Add(header);

            Label title = CreateLabel("WebGL Build Optimizer", 22, FontStyle.Bold, Accent);
            title.style.marginTop = 12;
            title.style.marginLeft = 16;
            header.Add(title);

            Label subtitle = CreateLabel("Analyze build dependencies, configure asset optimization and apply safe importer changes.", 12, FontStyle.Normal, new Color(0.78f, 0.78f, 0.78f));
            subtitle.style.marginLeft = 16;
            subtitle.style.marginTop = 4;
            header.Add(subtitle);

            VisualElement actionRow = new() {
                style = {
                    flexDirection = FlexDirection.Row,
                    marginLeft = 16,
                    marginTop = 14,
                    marginBottom = 14
                }
            };
            header.Add(actionRow);

            actionRow.Add(CreateActionButton("Build & Analyze", BuildAndAnalyze));
            actionRow.Add(CreateActionButton("Analyze Only", AnalyzeOnly));
            actionRow.Add(CreateActionButton("Open Reports", OpenReportsFolder));

            statusLabel = CreateLabel("No analysis yet.", 12, FontStyle.Normal, new Color(0.82f, 0.82f, 0.82f));
            statusLabel.style.marginTop = 10;
            statusLabel.style.whiteSpace = WhiteSpace.Normal;
            content.Add(statusLabel);

            summaryCards = new() {
                style = {
                    flexDirection = FlexDirection.Row,
                    marginTop = 12,
                    marginBottom = 12
                }
            };
            content.Add(summaryCards);

            content.Add(CreateSectionHeader("Build Breakdown"));
            categoryContainer = CreatePanel();
            content.Add(categoryContainer);

            content.Add(CreateSectionHeader("Largest Assets"));
            topAssetsContainer = CreatePanel();
            content.Add(topAssetsContainer);

            wizardContainer = CreatePanel();
            wizardContainer.style.marginTop = 12;
            content.Add(wizardContainer);
        }

        private void AnalyzeOnly() {
            try {
                currentAnalysis = BuildAssetAnalyzer.AnalyzeEnabledBuildScenes();
                lastBuildSizeBytes = 0;
                statusLabel.text = currentAnalysis.StatusMessage;
                RebuildPlan();
                RenderAnalysis();
                RenderWizard();
            }
            catch (Exception exception) {
                statusLabel.text = exception.Message;
                Logger.CreateError(nameof(BuildOptimizerView), "Build analysis failed", exception.Message);
            }
        }

        private void BuildAndAnalyze() {
            try {
                string buildPath = GetBuildFilePath();
                EditorUserBuildSettings.SetBuildLocation(BuildTarget.WebGL, buildPath);

                BuildPlayerOptions options = GetBuildPlayerOptions();
                options.locationPathName = buildPath;
                options.target = BuildTarget.WebGL;

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded) {
                    statusLabel.text = $"Build failed: {report.summary.result}";
                    return;
                }

                CleanBuildOutputArtifacts(buildPath);
                string outputPath = ApplyBuildExportFormat(buildPath);
                currentAnalysis = BuildAssetAnalyzer.AnalyzeEnabledBuildScenes();
                lastBuildSizeBytes = GetOutputSizeBytes(outputPath);
                statusLabel.text = $"{currentAnalysis.StatusMessage} | Build: {BuildAssetAnalyzer.FormatBytes(lastBuildSizeBytes)}";
                RebuildPlan();
                RenderAnalysis();
                RenderWizard();
                EditorUtility.RevealInFinder(outputPath);
            }
            catch (Exception exception) {
                statusLabel.text = exception.Message;
                Logger.CreateError(nameof(BuildOptimizerView), "Build and analysis failed", exception.Message);
            }
        }

        private void ApplySelectedOptimization() {
            if (!currentPlan.SelectedCandidates.Any()) {
                wizardStatusLabel.text = "No selected assets to optimize.";
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "PrimeSDK Asset Optimization",
                "This will change import settings for selected assets under Assets/.\n\nPackages/ assets are never modified.",
                "Apply",
                "Cancel"
            );
            if (!confirmed) {
                return;
            }

            try {
                BuildAssetOptimizationResult result = BuildAssetAnalyzer.ApplyOptimization(currentPlan, profile);
                statusLabel.text = result.StatusMessage;
                currentAnalysis = BuildAssetAnalyzer.AnalyzeEnabledBuildScenes();
                RebuildPlan();
                RenderAnalysis();
                RenderWizard();
            }
            catch (Exception exception) {
                statusLabel.text = exception.Message;
                Logger.CreateError(nameof(BuildOptimizerView), "Asset optimization failed", exception.Message);
            }
        }

        private void OpenReportsFolder() {
            string reportsFolder = Path.Combine(PackageTools.ProjectPath, "BuildReports").NormalizePath();
            Directory.CreateDirectory(reportsFolder);
            EditorUtility.RevealInFinder(reportsFolder + Path.AltDirectorySeparatorChar);
        }

        private void RebuildPlan() {
            currentPlan = BuildAssetAnalyzer.CreateOptimizationPlan(currentAnalysis, profile);
        }

        private void RenderEmptyState() {
            summaryCards.Clear();
            summaryCards.Add(CreateSummaryCard("Build Size", "No build", "Run Build & Analyze"));
            summaryCards.Add(CreateSummaryCard("Tracked Assets", "0", "No snapshot"));
            summaryCards.Add(CreateSummaryCard("Estimated Assets", "0 B", "No analysis"));

            categoryContainer.Clear();
            categoryContainer.Add(CreateMutedText("No analysis data. Click Build & Analyze or Analyze Only."));

            topAssetsContainer.Clear();
            topAssetsContainer.Add(CreateMutedText("Largest assets will appear here after analysis."));
        }

        private void RenderAnalysis() {
            if (currentAnalysis == null) {
                RenderEmptyState();
                return;
            }

            summaryCards.Clear();
            summaryCards.Add(CreateSummaryCard("Build Size", lastBuildSizeBytes > 0 ? BuildAssetAnalyzer.FormatBytes(lastBuildSizeBytes) : "Not built", lastBuildSizeBytes > 0 ? "Export output" : "Analysis only"));
            summaryCards.Add(CreateSummaryCard("Tracked Assets", currentAnalysis.Assets.Count.ToString(), "Build scene dependencies"));
            summaryCards.Add(CreateSummaryCard("Estimated Assets", BuildAssetAnalyzer.FormatBytes(currentAnalysis.TotalSizeBytes), "Source file sizes"));

            Dictionary<BuildAssetCategory, List<BuildAssetInfo>> byCategory = currentAnalysis.Assets
                .GroupBy(asset => asset.Category)
                .ToDictionary(group => group.Key, group => group.ToList());

            categoryContainer.Clear();
            foreach (BuildAssetCategory category in Enum.GetValues(typeof(BuildAssetCategory))) {
                byCategory.TryGetValue(category, out List<BuildAssetInfo> assets);
                assets ??= new List<BuildAssetInfo>();
                long bytes = assets.Sum(asset => asset.SizeBytes);
                categoryContainer.Add(CreateCategoryRow(category.ToString(), assets.Count, bytes, currentAnalysis.TotalSizeBytes));
            }

            topAssetsContainer.Clear();
            foreach (BuildAssetInfo asset in currentAnalysis.Assets.Take(TopAssetCount)) {
                topAssetsContainer.Add(CreateAssetRow(asset));
            }
        }

        private void RenderWizard() {
            wizardContainer.Clear();
            stepButtons.Clear();

            VisualElement header = new() {
                style = {
                    paddingLeft = 14,
                    paddingRight = 14,
                    paddingTop = 12,
                    paddingBottom = 12
                }
            };
            wizardContainer.Add(header);

            Label title = CreateLabel("Optimize Assets", 20, FontStyle.Bold, Accent);
            header.Add(title);

            Label description = CreateLabel("Configure imported asset optimization before applying changes. Run analysis first to populate candidates.", 12, FontStyle.Normal, Muted);
            description.style.marginTop = 3;
            header.Add(description);

            VisualElement steps = new() {
                style = {
                    flexDirection = FlexDirection.Row,
                    marginTop = 12
                }
            };
            header.Add(steps);

            foreach (OptimizerStep step in Enum.GetValues(typeof(OptimizerStep))) {
                Button stepButton = new(() => SetStep(step)) {
                    text = GetStepName(step)
                };
                stepButton.style.flexGrow = 1;
                stepButton.style.marginRight = 4;
                stepButton.style.height = 24;
                steps.Add(stepButton);
                stepButtons[step] = stepButton;
            }
            UpdateStepButtons();

            wizardStatusLabel = CreateLabel(GetWizardStatus(), 12, FontStyle.Normal, new Color(0.82f, 0.82f, 0.82f));
            wizardStatusLabel.style.marginLeft = 14;
            wizardStatusLabel.style.marginBottom = 10;
            wizardContainer.Add(wizardStatusLabel);

            VisualElement body = new() {
                style = {
                    paddingLeft = 14,
                    paddingRight = 14,
                    paddingBottom = 14
                }
            };
            wizardContainer.Add(body);

            switch (currentStep) {
                case OptimizerStep.Overview: RenderOptimizationOverview(body); break;
                case OptimizerStep.Textures: RenderTexturesStep(body); break;
                case OptimizerStep.Audio: RenderAudioStep(body); break;
                case OptimizerStep.Models: RenderModelsStep(body); break;
                case OptimizerStep.Materials: RenderMaterialsStep(body); break;
                case OptimizerStep.Apply: RenderApplyStep(body); break;
            }

            body.Add(CreateWizardNavigation());
        }

        private void RenderOptimizationOverview(VisualElement body) {
            VisualElement cards = new() {
                style = {
                    flexDirection = FlexDirection.Row,
                    marginTop = 8,
                    marginBottom = 8
                }
            };
            body.Add(cards);
            cards.Add(CreateSummaryCard("Textures", currentPlan.Textures.Count.ToString(), BuildAssetAnalyzer.FormatBytes(BuildAssetAnalyzer.GetCandidatesSize(currentPlan.Textures))));
            cards.Add(CreateSummaryCard("Audio", currentPlan.Audio.Count.ToString(), BuildAssetAnalyzer.FormatBytes(BuildAssetAnalyzer.GetCandidatesSize(currentPlan.Audio))));
            cards.Add(CreateSummaryCard("Models", currentPlan.Models.Count.ToString(), BuildAssetAnalyzer.FormatBytes(BuildAssetAnalyzer.GetCandidatesSize(currentPlan.Models))));
            cards.Add(CreateSummaryCard("Materials", currentPlan.Materials.Count.ToString(), BuildAssetAnalyzer.FormatBytes(BuildAssetAnalyzer.GetCandidatesSize(currentPlan.Materials))));

            body.Add(CreateMutedText(currentAnalysis == null
                ? "Run Build & Analyze or Analyze Only before applying optimization."
                : "Use the next steps to review candidates and tune import settings."));
        }

        private void RenderTexturesStep(VisualElement body) {
            body.Add(CreateCandidateHeader("Optimize Textures", currentPlan.Textures));

            VisualElement settings = CreateSettingsPanel();
            body.Add(settings);
            settings.Add(CreateToggleRow("Enable texture optimization", profile.OptimizeTextures, value => { profile.OptimizeTextures = value; RefreshPlanAndWizard(); }));
            settings.Add(CreateEnumRow("Max Size", profile.TextureMaxSize, new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 }, value => { profile.TextureMaxSize = value; RefreshPlanAndWizard(); }));
            settings.Add(CreateToggleRow("Override Max Size", profile.TextureOverrideMaxSize, value => profile.TextureOverrideMaxSize = value));
            settings.Add(CreateToggleRow("Generate MipMaps", profile.TextureGenerateMipMaps, value => profile.TextureGenerateMipMaps = value));
            settings.Add(CreateToggleRow("Override MipMaps", profile.TextureOverrideGenerateMipMaps, value => profile.TextureOverrideGenerateMipMaps = value));
            settings.Add(CreateEnumFieldRow("Compression", profile.TextureCompression, value => profile.TextureCompression = (TextureImporterCompression)value));
            settings.Add(CreateToggleRow("Crunch Compression", profile.TextureUseCrunchCompression, value => profile.TextureUseCrunchCompression = value));
            settings.Add(CreateSliderRow("Quality", profile.TextureCompressionQuality, 1, 100, value => profile.TextureCompressionQuality = value));
            settings.Add(CreateEnumFieldRow("Resize Algorithm", profile.TextureResizeAlgorithm, value => profile.TextureResizeAlgorithm = (TextureResizeAlgorithm)value));
            settings.Add(CreateEnumFieldRow("Format", profile.TextureFormat, value => profile.TextureFormat = (TextureImporterFormat)value));
            settings.Add(CreateFlagsRow("Extensions", profile.TextureExtensions, value => { profile.TextureExtensions = (OptimizerTextureExtensions)value; RefreshPlanAndWizard(); }));
            settings.Add(CreateFlagsRow("Texture Types", profile.TextureImporterTypes, value => { profile.TextureImporterTypes = (OptimizerTextureImporterTypes)value; RefreshPlanAndWizard(); }));
            settings.Add(CreateToggleRow("Exclude WebGLTemplates", profile.TextureExcludeFolders, value => { profile.TextureExcludeFolders = value; RefreshPlanAndWizard(); }));
            settings.Add(CreateToggleRow("Resize PNG/JPG source files", profile.TextureResizeSourceFiles, value => profile.TextureResizeSourceFiles = value));
            settings.Add(CreateToggleRow("Skip resize if Read/Write", profile.TextureSkipResizeIfReadWrite, value => profile.TextureSkipResizeIfReadWrite = value));

            body.Add(CreateSelectionRow(currentPlan.Textures, "Textures"));
            body.Add(CreateCandidateList(currentPlan.Textures));
        }

        private void RenderAudioStep(VisualElement body) {
            body.Add(CreateCandidateHeader("Optimize Audio", currentPlan.Audio));

            VisualElement settings = CreateSettingsPanel();
            body.Add(settings);
            settings.Add(CreateToggleRow("Enable audio optimization", profile.OptimizeAudio, value => { profile.OptimizeAudio = value; RefreshPlanAndWizard(); }));
            settings.Add(CreateToggleRow("Force To Mono", profile.AudioForceToMono, value => profile.AudioForceToMono = value));
            settings.Add(CreateToggleRow("Override Force To Mono", profile.AudioOverrideForceToMono, value => profile.AudioOverrideForceToMono = value));
            settings.Add(CreateToggleRow("Load In Background", profile.AudioLoadInBackground, value => profile.AudioLoadInBackground = value));
            settings.Add(CreateToggleRow("Override Load In Background", profile.AudioOverrideLoadInBackground, value => profile.AudioOverrideLoadInBackground = value));
            settings.Add(CreateToggleRow("Preload Audio Data", profile.AudioPreloadAudioData, value => profile.AudioPreloadAudioData = value));
            settings.Add(CreateToggleRow("Override Preload Audio Data", profile.AudioOverridePreloadAudioData, value => profile.AudioOverridePreloadAudioData = value));
            settings.Add(CreateEnumFieldRow("Load Type", profile.AudioLoadType, value => profile.AudioLoadType = (AudioClipLoadType)value));
            settings.Add(CreateEnumFieldRow("Compression Format", profile.AudioCompressionFormat, value => profile.AudioCompressionFormat = (AudioCompressionFormat)value));
            settings.Add(CreateFloatSliderRow("Quality", profile.AudioQuality, 0.01f, 1f, value => profile.AudioQuality = value));
            settings.Add(CreateEnumFieldRow("Sample Rate", profile.AudioSampleRateSetting, value => profile.AudioSampleRateSetting = (AudioSampleRateSetting)value));
            settings.Add(CreateFlagsRow("Extensions", profile.AudioExtensions, value => { profile.AudioExtensions = (OptimizerAudioExtensions)value; RefreshPlanAndWizard(); }));

            body.Add(CreateSelectionRow(currentPlan.Audio, "Audio"));
            body.Add(CreateCandidateList(currentPlan.Audio));
        }

        private void RenderModelsStep(VisualElement body) {
            body.Add(CreateCandidateHeader("Optimize Models", currentPlan.Models));

            VisualElement settings = CreateSettingsPanel();
            body.Add(settings);
            settings.Add(CreateToggleRow("Enable model optimization", profile.OptimizeModels, value => { profile.OptimizeModels = value; RefreshPlanAndWizard(); }));
            settings.Add(CreateEnumFieldRow("Mesh Compression", profile.ModelMeshCompression, value => profile.ModelMeshCompression = (ModelImporterMeshCompression)value));
            settings.Add(CreateToggleRow("Override Mesh Compression", profile.ModelOverrideMeshCompression, value => profile.ModelOverrideMeshCompression = value));
            settings.Add(CreateToggleRow("Disable Read/Write", profile.ModelDisableReadWrite, value => profile.ModelDisableReadWrite = value));
            settings.Add(CreateToggleRow("Optimize Mesh", profile.ModelOptimizeMesh, value => profile.ModelOptimizeMesh = value));

            body.Add(CreateSelectionRow(currentPlan.Models, "Models"));
            body.Add(CreateCandidateList(currentPlan.Models));
        }

        private void RenderMaterialsStep(VisualElement body) {
            body.Add(CreateCandidateHeader("Optimize Materials", currentPlan.Materials));

            VisualElement settings = CreateSettingsPanel();
            body.Add(settings);
            settings.Add(CreateToggleRow("Enable material optimization", profile.OptimizeMaterials, value => { profile.OptimizeMaterials = value; RefreshPlanAndWizard(); }));
            settings.Add(CreateToggleRow("Enable GPU Instancing", profile.MaterialEnableGPUInstancing, value => profile.MaterialEnableGPUInstancing = value));
            settings.Add(CreateToggleRow("Override GPU Instancing", profile.MaterialOverrideGPUInstancing, value => profile.MaterialOverrideGPUInstancing = value));
            settings.Add(CreateToggleRow("Change Shaders", profile.MaterialChangeShaders, value => profile.MaterialChangeShaders = value));
            settings.Add(CreateTextRow("Old Shader", profile.MaterialOldShader, value => profile.MaterialOldShader = value));
            settings.Add(CreateTextRow("New Shader", profile.MaterialNewShader, value => profile.MaterialNewShader = value));

            body.Add(CreateSelectionRow(currentPlan.Materials, "Materials"));
            body.Add(CreateCandidateList(currentPlan.Materials));
        }

        private void RenderApplyStep(VisualElement body) {
            int selected = currentPlan.SelectedCandidates.Count();
            body.Add(CreateCandidateHeader("Apply Optimization", currentPlan.SelectedCandidates.ToList()));
            body.Add(CreateMutedText("Review selected assets before applying importer changes. Source texture resizing is optional and only affects selected PNG/JPG files."));

            Button apply = CreateActionButton($"Apply to {selected} Assets", ApplySelectedOptimization);
            apply.SetEnabled(selected > 0);
            apply.style.width = 180;
            apply.style.height = 28;
            body.Add(apply);

            body.Add(CreateSectionHeader("Selected Preview"));
            body.Add(CreateCandidateList(currentPlan.SelectedCandidates.Take(CandidatePreviewCount).ToList()));
        }

        private VisualElement CreateCandidateHeader(string title, IReadOnlyCollection<BuildAssetOptimizationCandidate> candidates) {
            VisualElement panel = CreatePanel();
            panel.style.paddingLeft = 12;
            panel.style.paddingRight = 12;
            panel.style.paddingTop = 10;
            panel.style.paddingBottom = 10;

            Label titleLabel = CreateLabel(title, 15, FontStyle.Bold, Color.white);
            panel.Add(titleLabel);

            int selected = candidates.Count(candidate => candidate.Selected);
            long totalBytes = BuildAssetAnalyzer.GetCandidatesSize(candidates);
            Label stats = CreateLabel($"Found: {candidates.Count}     Total: {BuildAssetAnalyzer.FormatBytes(totalBytes)}     Selected: {selected}", 12, FontStyle.Normal, Muted);
            stats.style.marginTop = 6;
            panel.Add(stats);
            return panel;
        }

        private VisualElement CreateSelectionRow(List<BuildAssetOptimizationCandidate> candidates, string label) {
            VisualElement row = new() {
                style = {
                    flexDirection = FlexDirection.Row,
                    marginTop = 8,
                    marginBottom = 8
                }
            };
            row.Add(CreateActionButton("Select All", () => SelectCandidates(candidates, true)));
            row.Add(CreateActionButton("Select None", () => SelectCandidates(candidates, false)));
            row.Add(CreateActionButton("Select Large (>100KB)", () => {
                foreach (BuildAssetOptimizationCandidate candidate in candidates) {
                    candidate.Selected = candidate.Asset.SizeBytes > 100 * 1024;
                }
                RenderWizard();
            }));
            Button apply = CreateActionButton($"Apply {label}", ApplySelectedOptimization);
            apply.SetEnabled(candidates.Any(candidate => candidate.Selected));
            apply.style.marginLeft = 12;
            row.Add(apply);
            return row;
        }

        private VisualElement CreateCandidateList(IReadOnlyList<BuildAssetOptimizationCandidate> candidates) {
            VisualElement list = CreatePanel();
            list.style.marginTop = 4;

            if (candidates.Count == 0) {
                list.Add(CreateMutedText("No assets found. Run Build & Analyze first or adjust filters."));
                return list;
            }

            foreach (BuildAssetOptimizationCandidate candidate in candidates.Take(CandidatePreviewCount)) {
                VisualElement row = CreateRow();
                Toggle toggle = new() {
                    value = candidate.Selected
                };
                toggle.RegisterValueChangedCallback(evt => candidate.Selected = evt.newValue);
                toggle.style.width = 24;
                row.Add(toggle);
                row.Add(CreateFixedLabel(BuildAssetAnalyzer.FormatBytes(candidate.Asset.SizeBytes), 92, Accent, FontStyle.Bold));
                row.Add(CreateFixedLabel(candidate.Asset.Category.ToString(), 82, Muted, FontStyle.Normal));

                Label path = CreateLabel(candidate.Asset.Path, 12, FontStyle.Normal, new Color(0.9f, 0.9f, 0.9f));
                path.style.flexGrow = 1;
                path.style.unityTextAlign = TextAnchor.MiddleLeft;
                row.Add(path);
                list.Add(row);
            }

            if (candidates.Count > CandidatePreviewCount) {
                list.Add(CreateMutedText($"Showing first {CandidatePreviewCount} assets. Selection buttons still affect all {candidates.Count} candidates."));
            }
            return list;
        }

        private void SelectCandidates(IEnumerable<BuildAssetOptimizationCandidate> candidates, bool selected) {
            foreach (BuildAssetOptimizationCandidate candidate in candidates) {
                candidate.Selected = selected;
            }
            RenderWizard();
        }

        private VisualElement CreateWizardNavigation() {
            VisualElement row = new() {
                style = {
                    flexDirection = FlexDirection.Row,
                    marginTop = 14,
                    alignItems = Align.Center
                }
            };

            Button back = CreateActionButton("< Back", () => SetStep(GetPreviousStep()));
            back.SetEnabled(currentStep != OptimizerStep.Overview);
            back.style.width = 110;
            row.Add(back);

            Label current = CreateLabel(GetStepName(currentStep), 13, FontStyle.Bold, Color.white);
            current.style.flexGrow = 1;
            current.style.unityTextAlign = TextAnchor.MiddleCenter;
            row.Add(current);

            Button next = CreateActionButton("Next >", () => SetStep(GetNextStep()));
            next.SetEnabled(currentStep != OptimizerStep.Apply);
            next.style.width = 110;
            row.Add(next);
            return row;
        }

        private void SetStep(OptimizerStep step) {
            currentStep = step;
            RenderWizard();
        }

        private void RefreshPlanAndWizard() {
            RebuildPlan();
            RenderWizard();
        }

        private OptimizerStep GetPreviousStep() {
            return currentStep == OptimizerStep.Overview ? OptimizerStep.Overview : (OptimizerStep)((int)currentStep - 1);
        }

        private OptimizerStep GetNextStep() {
            return currentStep == OptimizerStep.Apply ? OptimizerStep.Apply : (OptimizerStep)((int)currentStep + 1);
        }

        private void UpdateStepButtons() {
            foreach (KeyValuePair<OptimizerStep, Button> pair in stepButtons) {
                pair.Value.style.backgroundColor = pair.Key == currentStep ? new Color(1f, 0.42f, 0f, 0.75f) : new Color(0.17f, 0.17f, 0.18f);
                pair.Value.style.color = pair.Key == currentStep ? Color.white : new Color(0.82f, 0.82f, 0.82f);
            }
        }

        private string GetWizardStatus() {
            int selected = currentPlan.SelectedCandidates.Count();
            int total = currentPlan.AllCandidates.Count();
            return currentAnalysis == null
                ? "No analysis data. Optimization candidates will appear after analysis."
                : $"Optimization candidates: {total} | Selected: {selected}";
        }

        private static string GetStepName(OptimizerStep step) {
            return step switch {
                OptimizerStep.Overview => "1 Overview",
                OptimizerStep.Textures => "2 Textures",
                OptimizerStep.Audio => "3 Audio",
                OptimizerStep.Models => "4 Models",
                OptimizerStep.Materials => "5 Materials",
                OptimizerStep.Apply => "6 Apply",
                _ => step.ToString()
            };
        }

        private VisualElement CreateSettingsPanel() {
            VisualElement panel = CreatePanel();
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            panel.style.paddingTop = 8;
            panel.style.paddingBottom = 8;
            return panel;
        }

        private VisualElement CreateToggleRow(string label, bool value, Action<bool> changed) {
            VisualElement row = CreateSettingRow(label);
            Toggle toggle = new() {
                value = value
            };
            toggle.RegisterValueChangedCallback(evt => changed(evt.newValue));
            row.Add(toggle);
            return row;
        }

        private VisualElement CreateTextRow(string label, string value, Action<string> changed) {
            VisualElement row = CreateSettingRow(label);
            TextField field = new() {
                value = value
            };
            ApplyFieldWidth(field, SettingTextFieldWidth);
            field.RegisterValueChangedCallback(evt => changed(evt.newValue));
            row.Add(field);
            return row;
        }

        private VisualElement CreateEnumRow(string label, int value, IReadOnlyList<int> options, Action<int> changed) {
            VisualElement row = CreateSettingRow(label);
            List<string> optionLabels = options.Select(option => option.ToString()).ToList();
            string currentValue = optionLabels.Contains(value.ToString()) ? value.ToString() : optionLabels.FirstOrDefault() ?? value.ToString();
            PopupField<string> popup = new(optionLabels, currentValue);
            ApplyFieldWidth(popup, 180f);
            popup.RegisterValueChangedCallback(evt => {
                if (int.TryParse(evt.newValue, out int parsedValue)) {
                    changed(parsedValue);
                }
            });
            row.Add(popup);
            return row;
        }

        private VisualElement CreateEnumFieldRow(string label, Enum value, Action<Enum> changed) {
            VisualElement row = CreateSettingRow(label);
            EnumField field = new(value);
            ApplyFieldWidth(field, SettingFieldWidth);
            field.RegisterValueChangedCallback(evt => changed(evt.newValue));
            row.Add(field);
            return row;
        }

        private VisualElement CreateFlagsRow(string label, Enum value, Action<Enum> changed) {
            VisualElement row = CreateSettingRow(label);
            EnumFlagsField field = new(value);
            ApplyFieldWidth(field, SettingFieldWidth);
            field.RegisterValueChangedCallback(evt => changed(evt.newValue));
            row.Add(field);
            return row;
        }

        private VisualElement CreateSliderRow(string label, int value, int min, int max, Action<int> changed) {
            VisualElement row = CreateSettingRow(label);
            SliderInt slider = new(min, max) {
                value = value
            };
            ApplyFieldWidth(slider, SettingFieldWidth);
            IntegerField field = new() {
                value = value
            };
            ApplyFieldWidth(field, SettingValueFieldWidth);
            slider.RegisterValueChangedCallback(evt => {
                field.value = evt.newValue;
                changed(evt.newValue);
            });
            field.RegisterValueChangedCallback(evt => {
                int clamped = Mathf.Clamp(evt.newValue, min, max);
                slider.value = clamped;
                changed(clamped);
            });
            row.Add(slider);
            row.Add(field);
            return row;
        }

        private VisualElement CreateFloatSliderRow(string label, float value, float min, float max, Action<float> changed) {
            VisualElement row = CreateSettingRow(label);
            Slider slider = new(min, max) {
                value = value
            };
            ApplyFieldWidth(slider, SettingFieldWidth);
            FloatField field = new() {
                value = value
            };
            ApplyFieldWidth(field, SettingValueFieldWidth);
            slider.RegisterValueChangedCallback(evt => {
                field.value = evt.newValue;
                changed(evt.newValue);
            });
            field.RegisterValueChangedCallback(evt => {
                float clamped = Mathf.Clamp(evt.newValue, min, max);
                slider.value = clamped;
                changed(clamped);
            });
            row.Add(slider);
            row.Add(field);
            return row;
        }

        private VisualElement CreateSettingRow(string label) {
            VisualElement row = new() {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    minHeight = 28,
                    marginBottom = 2,
                    flexShrink = 1
                }
            };
            row.Add(CreateFixedLabel(label, SettingLabelWidth, new Color(0.82f, 0.82f, 0.82f), FontStyle.Normal));
            return row;
        }

        private static void ApplyFieldWidth(VisualElement element, float width) {
            element.style.width = width;
            element.style.maxWidth = width;
            element.style.flexGrow = 0;
            element.style.flexShrink = 1;
        }

        private VisualElement CreateSummaryCard(string title, string value, string detail) {
            VisualElement card = CreatePanel();
            card.style.flexGrow = 1;
            card.style.marginRight = 8;
            card.style.minHeight = 86;

            Label titleLabel = CreateLabel(title, 11, FontStyle.Bold, Accent);
            titleLabel.style.marginLeft = 12;
            titleLabel.style.marginTop = 10;
            card.Add(titleLabel);

            Label valueLabel = CreateLabel(value, 18, FontStyle.Bold, Color.white);
            valueLabel.style.marginLeft = 12;
            valueLabel.style.marginTop = 4;
            card.Add(valueLabel);

            Label detailLabel = CreateLabel(detail, 11, FontStyle.Normal, Muted);
            detailLabel.style.marginLeft = 12;
            detailLabel.style.marginTop = 2;
            card.Add(detailLabel);

            return card;
        }

        private VisualElement CreateCategoryRow(string name, int count, long bytes, long totalBytes) {
            VisualElement row = CreateRow();
            row.Add(CreateFixedLabel(name, 120, new Color(0.92f, 0.92f, 0.92f), FontStyle.Bold));
            row.Add(CreateFixedLabel(count.ToString(), 72, new Color(0.78f, 0.78f, 0.78f), FontStyle.Normal));
            row.Add(CreateFixedLabel(BuildAssetAnalyzer.FormatBytes(bytes), 120, Accent, FontStyle.Bold));

            float percent = totalBytes <= 0 ? 0 : bytes / (float)totalBytes;
            VisualElement barFrame = new() {
                style = {
                    flexGrow = 1,
                    height = 8,
                    marginTop = 8,
                    backgroundColor = new Color(0.12f, 0.12f, 0.13f),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };
            VisualElement bar = new() {
                style = {
                    width = Length.Percent(Mathf.Clamp01(percent) * 100f),
                    height = 8,
                    backgroundColor = new Color(1f, 0.42f, 0f)
                }
            };
            barFrame.Add(bar);
            row.Add(barFrame);
            return row;
        }

        private VisualElement CreateAssetRow(BuildAssetInfo asset) {
            VisualElement row = CreateRow();
            row.Add(CreateFixedLabel(BuildAssetAnalyzer.FormatBytes(asset.SizeBytes), 92, Accent, FontStyle.Bold));
            row.Add(CreateFixedLabel(asset.Category.ToString(), 82, Muted, FontStyle.Normal));

            Label path = CreateLabel(asset.Path, 12, FontStyle.Normal, new Color(0.9f, 0.9f, 0.9f));
            path.style.flexGrow = 1;
            path.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.Add(path);
            return row;
        }

        private static VisualElement CreatePanel() {
            VisualElement panel = new() {
                style = {
                    backgroundColor = PanelBackground,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftColor = new Color(1f, 0.42f, 0f, 0.22f),
                    borderRightColor = new Color(1f, 0.42f, 0f, 0.22f),
                    borderTopColor = new Color(1f, 0.42f, 0f, 0.22f),
                    borderBottomColor = new Color(1f, 0.42f, 0f, 0.22f),
                    marginBottom = 8
                }
            };
            return panel;
        }

        private static VisualElement CreateRow() {
            VisualElement row = new() {
                style = {
                    flexDirection = FlexDirection.Row,
                    minHeight = 30,
                    alignItems = Align.Center,
                    paddingLeft = 10,
                    paddingRight = 10,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(1f, 1f, 1f, 0.05f)
                }
            };
            return row;
        }

        private static Label CreateSectionHeader(string text) {
            Label label = CreateLabel(text, 14, FontStyle.Bold, Accent);
            label.style.marginTop = 10;
            label.style.marginBottom = 6;
            return label;
        }

        private static Label CreateMutedText(string text) {
            Label label = CreateLabel(text, 12, FontStyle.Normal, Muted);
            label.style.marginLeft = 10;
            label.style.marginTop = 10;
            label.style.marginBottom = 10;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static Label CreateFixedLabel(string text, float width, Color color, FontStyle fontStyle) {
            Label label = CreateLabel(text, 12, fontStyle, color);
            label.style.width = width;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            return label;
        }

        private static Label CreateLabel(string text, int fontSize, FontStyle fontStyle, Color color) {
            Label label = new(text);
            label.style.fontSize = fontSize;
            label.style.unityFontStyleAndWeight = fontStyle;
            label.style.color = color;
            label.style.marginTop = 0;
            label.style.marginBottom = 0;
            label.style.marginLeft = 0;
            label.style.marginRight = 0;
            return label;
        }

        private static Button CreateActionButton(string text, Action action) {
            Button button = new(action) {
                text = text
            };
            button.style.marginRight = 6;
            button.style.minHeight = 24;
            return button;
        }

        private static string GetBuildFilePath() {
            string defaultBuildsFolder = Path.Combine(PackageTools.ProjectPath, Naming.Builds).NormalizePath();
            string buildsFolder = PackageTools.GetPrefsString(CurrentBuildsFolderPathKey, defaultBuildsFolder);
            DirectoryInfo buildsDirectory = new(buildsFolder);
            if (!buildsDirectory.Exists) {
                buildsDirectory.Create();
            }

            string defaultProjectName = PlayerSettings.productName.ToSafeFileName("build");
            string defaultFileName = $"{defaultProjectName}[#NUMBER]-primeSDK[#VERSION]";
            string fileName = PackageTools.GetPrefsString(CurrentBuildFileNameKey, defaultFileName);
            int versionHandle = buildsDirectory.GetFileSystemInfos().Count() + 1;
            fileName = fileName.Replace("#NUMBER", versionHandle.ToString());
            fileName = fileName.Replace("#VERSION", PrimeSDK.Version);

            string buildDirectoryPath = Path.Combine(buildsDirectory.FullName, fileName).NormalizePath();
            Directory.CreateDirectory(buildDirectoryPath);
            return buildDirectoryPath;
        }

        private static BuildPlayerOptions GetBuildPlayerOptions() {
            Type defaultBuildMethodsType = typeof(BuildPlayerWindow.DefaultBuildMethods);
            MethodInfo getBuildPlayerOptionsInternalMethod = defaultBuildMethodsType.GetMethod(
                name: "GetBuildPlayerOptionsInternal",
                bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(bool), typeof(BuildPlayerOptions) },
                modifiers: null
            );

            if (getBuildPlayerOptionsInternalMethod != null) {
                return (BuildPlayerOptions)getBuildPlayerOptionsInternalMethod.Invoke(null, new object[] { false, new BuildPlayerOptions() });
            }
            throw new InvalidOperationException("Failed to get BuildPlayerOptions");
        }

        private static BuildExportFormat GetCurrentBuildExportFormat() {
            string valueName = PackageTools.GetPrefsString(CurrentBuildExportFormatKey);
            return valueName.ToEnumOrDefault<BuildExportFormat>();
        }

        private static string ApplyBuildExportFormat(string buildFolderPath) {
            return GetCurrentBuildExportFormat() switch {
                BuildExportFormat.UncompressedZip => CompressFolder(buildFolderPath, true),
                _ => buildFolderPath
            };
        }

        private static void CleanBuildOutputArtifacts(string folderPath) {
            foreach (string filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)) {
                if (IsBuildOutputArtifactIgnored(filePath)) {
                    File.Delete(filePath);
                }
            }
        }

        private static bool IsBuildOutputArtifactIgnored(string filePath) {
            return Path.GetFileName(filePath).EndsWith("~", StringComparison.Ordinal);
        }

        private static long GetOutputSizeBytes(string outputPath) {
            if (File.Exists(outputPath)) {
                return new FileInfo(outputPath).Length;
            }

            if (!Directory.Exists(outputPath)) {
                return 0;
            }

            long sizeBytes = 0;
            foreach (string filePath in Directory.EnumerateFiles(outputPath, "*", SearchOption.AllDirectories)) {
                if (IsBuildOutputArtifactIgnored(filePath)) {
                    continue;
                }
                sizeBytes += new FileInfo(filePath).Length;
            }
            return sizeBytes;
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

                string relativePath = Path.GetRelativePath(folderPath, filePath).NormalizePath();
                archive.CreateEntryFromFile(filePath, relativePath, System.IO.Compression.CompressionLevel.NoCompression);
            }

            if (deleteFolder) {
                Directory.Delete(folderPath, true);
            }
            return zipPath;
        }

    }

}
