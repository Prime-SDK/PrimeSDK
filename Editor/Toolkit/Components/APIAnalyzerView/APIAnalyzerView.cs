using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.Editor {

    internal sealed class APIAnalyzerView : VisualElement {

        private readonly List<APIFinding> findings = new();
        private readonly List<APIFinding> visibleFindings = new();
        private Label statusLabel;
        private VisualElement resultsContainer;
        private ListView resultsList;
        private VisualTreeAsset resultItemTemplate;

        public APIAnalyzerView() {
            AddToClassList("api-analyzer-root");

            VisualTreeAsset asset = VisualTreeReference.LoadVisualTree(nameof(APIAnalyzerView));
            asset.CloneTree(this);
            resultItemTemplate = VisualTreeReference.LoadVisualTree("APIAnalyzerResultItem");

            statusLabel = this.Q<Label>("Status");
            resultsContainer = this.Q<VisualElement>("Results");
            InitializeResultsList();

            this.Q<Button>("Scan").clicked += Scan;
            this.Q<Button>("Convert").clicked += Convert;
        }

        private void InitializeResultsList() {
            resultsList = new ListView {
                fixedItemHeight = 96,
                selectionType = SelectionType.None,
                makeItem = CreateListItem,
                bindItem = BindListItem
            };
            resultsList.AddToClassList("api-analyzer-results-list");
            resultsContainer.Add(resultsList);
        }

        private void Scan() {
            findings.Clear();
            visibleFindings.Clear();
            resultsList.Rebuild();

            string assetsPath = Path.Combine(PackageTools.ProjectPath, "Assets").NormalizePath();
            foreach (string filePath in Directory.EnumerateFiles(assetsPath, "*.cs", SearchOption.AllDirectories)) {
                if (ShouldSkip(filePath)) {
                    continue;
                }
                ScanFile(filePath.NormalizePath());
            }

            int convertCount = findings.Count(finding => finding.CanConvert);
            int adCount = findings.Count(finding => finding.Category == "Ad Keyword");
            int iapCount = findings.Count(finding => finding.Category == "IAP Keyword");
            statusLabel.text = $"Found {findings.Count} findings. Convertible: {convertCount}. Ad review: {adCount}. IAP review: {iapCount}.";
            RenderResults();
        }

        private void ScanFile(string filePath) {
            string text;
            try {
                text = File.ReadAllText(filePath);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(APIAnalyzerView), nameof(ScanFile), exception.Message);
                return;
            }

            string assetPath = ToAssetPath(filePath);
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int index = 0; index < lines.Length; index++) {
                string line = lines[index];
                int lineNumber = index + 1;

                foreach (ReplacementRule rule in ReplacementRules) {
                    if (line.Contains(rule.Search, StringComparison.Ordinal)) {
                        findings.Add(new APIFinding(assetPath, lineNumber, rule.Category, TrimPreview(line), true, rule.Search, rule.Replace));
                    }
                }

                foreach (string keyword in AdKeywords) {
                    if (LineContainsKeyword(line, keyword)) {
                        findings.Add(new APIFinding(assetPath, lineNumber, "Ad Keyword", TrimPreview(line), false, keyword, string.Empty));
                        break;
                    }
                }

                foreach (string keyword in IapKeywords) {
                    if (LineContainsKeyword(line, keyword)) {
                        findings.Add(new APIFinding(assetPath, lineNumber, "IAP Keyword", TrimPreview(line), false, keyword, string.Empty));
                        break;
                    }
                }
            }
        }

        private void RenderResults() {
            visibleFindings.Clear();
            visibleFindings.AddRange(findings.OrderBy(finding => finding.Path).ThenBy(finding => finding.Line));
            resultsList.itemsSource = visibleFindings;
            resultsList.Rebuild();
        }

        private VisualElement CreateListItem() {
            VisualElement row = resultItemTemplate.Instantiate();
            Button select = row.Q<Button>("SelectButton");
            select.clicked += () => {
                if (select.userData is string path) {
                    SelectScript(path);
                }
            };
            return row;
        }

        private void BindListItem(VisualElement element, int index) {
            APIFinding finding = visibleFindings[index];

            string titleText = finding.CanConvert
                ? $"{finding.Category}: {finding.Search} -> {finding.Replace}"
                : $"{finding.Category}: {finding.Search}";

            Label title = element.Q<Label>("Title");
            title.text = titleText;
            title.EnableInClassList("api-analyzer-result-title-convertible", finding.CanConvert);

            element.Q<Label>("Path").text = $"{finding.Path}:{finding.Line}";
            element.Q<Label>("Preview").text = finding.Preview;

            Button select = element.Q<Button>("SelectButton");
            select.text = finding.CanConvert ? "Select" : "Select Script";
            select.userData = finding.Path;
        }

        private void Convert() {
            List<APIFinding> convertible = findings.Where(finding => finding.CanConvert).ToList();
            if (convertible.Count == 0) {
                EditorUtility.DisplayDialog("PrimeSDK API Analyzer", "No convertible API usages found. Run Scan first.", "OK");
                return;
            }

            int fileCount = convertible.Select(finding => finding.Path).Distinct().Count();
            string summary = BuildConvertSummary(convertible);
            bool confirmed = EditorUtility.DisplayDialog(
                "PrimeSDK API Analyzer",
                $"Experimental Convert will update {convertible.Count} usages in {fileCount} files.\n\n{summary}\n\nAd and IAP keyword findings will not be changed.\n\nContinue?",
                "Convert",
                "Cancel"
            );
            if (!confirmed) {
                return;
            }

            foreach (IGrouping<string, APIFinding> group in convertible.GroupBy(finding => finding.Path)) {
                ConvertFile(group.Key);
            }

            AssetDatabase.Refresh();
            Scan();
        }

        private static void ConvertFile(string assetPath) {
            string filePath = Path.Combine(PackageTools.ProjectPath, assetPath).NormalizePath();
            string text = File.ReadAllText(filePath);
            string converted = text;

            foreach (ReplacementRule rule in ReplacementRules) {
                converted = converted.Replace(rule.Search, rule.Replace);
            }

            if (!converted.Contains("using PrimeGames.SDK;", StringComparison.Ordinal)) {
                converted = AddPrimeSDKUsing(converted);
            }

            if (converted != text) {
                File.WriteAllText(filePath, converted);
            }
        }

        private static string AddPrimeSDKUsing(string text) {
            MatchCollection matches = Regex.Matches(text, @"^using\s+[^;]+;\s*$", RegexOptions.Multiline);
            if (matches.Count == 0) {
                return "using PrimeGames.SDK;\n" + text;
            }

            Match last = matches[^1];
            int insertIndex = last.Index + last.Length;
            return text.Insert(insertIndex, "\nusing PrimeGames.SDK;");
        }

        private static string BuildConvertSummary(List<APIFinding> convertible) {
            IEnumerable<string> replacements = convertible
                .GroupBy(finding => new { finding.Search, finding.Replace })
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.Search)
                .Select(group => $"{group.Key.Search} -> {group.Key.Replace}: {group.Count()}");

            IEnumerable<string> examples = convertible
                .OrderBy(finding => finding.Path)
                .ThenBy(finding => finding.Line)
                .Take(12)
                .Select(finding => $"{finding.Path}:{finding.Line}  {finding.Search} -> {finding.Replace}");

            string replacementText = string.Join("\n", replacements);
            string exampleText = string.Join("\n", examples);
            int remaining = convertible.Count - 12;
            if (remaining > 0) {
                exampleText += $"\n...and {remaining} more usages";
            }
            return $"Replacements:\n{replacementText}\n\nExamples:\n{exampleText}";
        }

        private static bool ShouldSkip(string filePath) {
            string normalized = filePath.NormalizePath();
            return normalized.Contains($"{Path.AltDirectorySeparatorChar}Library{Path.AltDirectorySeparatorChar}")
                || normalized.Contains($"{Path.AltDirectorySeparatorChar}Packages{Path.AltDirectorySeparatorChar}")
                || normalized.Contains($"{Path.AltDirectorySeparatorChar}obj{Path.AltDirectorySeparatorChar}");
        }

        private static void SelectScript(string assetPath) {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            Selection.activeObject = asset;
            if (asset != null) {
                EditorGUIUtility.PingObject(asset);
            }
        }

        private static bool LineContainsKeyword(string line, string keyword) {
            int index = line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            while (index >= 0) {
                if (keyword.Length > 3 || (IsKeywordBoundary(line, index - 1) && IsKeywordBoundary(line, index + keyword.Length))) {
                    return true;
                }
                index = line.IndexOf(keyword, index + keyword.Length, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private static bool IsKeywordBoundary(string text, int index) {
            if (index < 0 || index >= text.Length) {
                return true;
            }
            char character = text[index];
            return !char.IsLetterOrDigit(character) && character != '_';
        }

        private static string TrimPreview(string line) {
            string preview = line.Trim();
            const int maxLength = 260;
            return preview.Length <= maxLength ? preview : preview.Substring(0, maxLength) + "...";
        }

        private static string ToAssetPath(string systemPath) {
            string relative = Path.GetRelativePath(PackageTools.ProjectPath, systemPath).NormalizePath();
            return relative.Replace(Path.AltDirectorySeparatorChar, '/');
        }

        private static readonly ReplacementRule[] ReplacementRules = {
            new("Audio", "AudioListener.pause", "PrimeSDK.Audio.Pause"),
            new("Audio", "AudioListener.volume", "PrimeSDK.Audio.Volume"),
            new("Cursor", "Cursor.visible", "PrimeSDK.Device.CursorVisible"),
            new("Cursor", "Cursor.lockState", "PrimeSDK.Device.CursorLock"),
            new("Time", "Time.timeScale", "PrimeSDK.Time.Scale"),
            new("Data", "PlayerPrefs.GetInt", "PrimeSDK.Data.GetInt"),
            new("Data", "PlayerPrefs.SetInt", "PrimeSDK.Data.SetInt"),
            new("Data", "PlayerPrefs.GetFloat", "PrimeSDK.Data.GetFloat"),
            new("Data", "PlayerPrefs.SetFloat", "PrimeSDK.Data.SetFloat"),
            new("Data", "PlayerPrefs.GetString", "PrimeSDK.Data.GetString"),
            new("Data", "PlayerPrefs.SetString", "PrimeSDK.Data.SetString"),
            new("Data", "PlayerPrefs.HasKey", "PrimeSDK.Data.HasKey"),
            new("Data", "PlayerPrefs.DeleteKey", "PrimeSDK.Data.DeleteKey"),
            new("Data", "PlayerPrefs.DeleteAll", "PrimeSDK.Data.DeleteAll"),
            new("Data", "PlayerPrefs.Save", "PrimeSDK.Data.Save")
        };

        private static readonly string[] AdKeywords = {
            "ads",
            "ad",
            "Interstitial",
            "Rewarded",
            "ShowAd",
            "AdManager",
            "Advertisement",
            "IronSource",
            "LevelPlay",
            "GoogleMobileAds",
            "MobileAds",
            "CrazySDK",
            "GameDistribution",
            "gdsdk",
            "YandexGame",
            "Lagged",
            "Poki"
        };

        private static readonly string[] IapKeywords = {
            "IAP",
            "InApp",
            "Purchase",
            "Purchases",
            "Product",
            "ProductId",
            "RestorePurchases",
            "ProcessPurchase",
            "OnPurchase",
            "UnityPurchasing",
            "IStoreListener",
            "IDetailedStoreListener",
            "CodelessIAP",
            "CrossPlatformValidator",
            "ProductCatalog",
            "PrimeSDK.Payments"
        };

        private readonly struct ReplacementRule {
            public ReplacementRule(string category, string search, string replace) {
                Category = category;
                Search = search;
                Replace = replace;
            }

            public string Category { get; }
            public string Search { get; }
            public string Replace { get; }
        }

        private readonly struct APIFinding {
            public APIFinding(string path, int line, string category, string preview, bool canConvert, string search, string replace) {
                Path = path;
                Line = line;
                Category = category;
                Preview = preview;
                CanConvert = canConvert;
                Search = search;
                Replace = replace;
            }

            public string Path { get; }
            public int Line { get; }
            public string Category { get; }
            public string Preview { get; }
            public bool CanConvert { get; }
            public string Search { get; }
            public string Replace { get; }
        }

    }

}
