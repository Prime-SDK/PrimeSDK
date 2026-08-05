using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.Editor {

    internal sealed class SDKAnalyzerView : VisualElement {

        private readonly List<SDKFinding> findings = new();
        private readonly SDKAnalyzerInspector inspector;
        private readonly VisualTreeAsset resultItemTemplate;

        private Label statusLabel;
        private VisualElement resultsContainer;
        private const string AssetsFolder = "Assets";

        public SDKAnalyzerView(SDKAnalyzerInspector inspector) {
            this.inspector = inspector;
            AddToClassList("sdk-analyzer-root");

            VisualTreeAsset asset = VisualTreeReference.LoadVisualTree(nameof(SDKAnalyzerView));
            asset.CloneTree(this);
            resultItemTemplate = VisualTreeReference.LoadVisualTree("SDKAnalyzerResultItem");

            statusLabel = this.Q<Label>("Status");
            resultsContainer = this.Q<VisualElement>("Results");

            this.Q<Button>("Scan").clicked += Scan;
        }

        private void Scan() {
            findings.Clear();
            resultsContainer.Clear();
            inspector.ClearSelection();
            HashSet<string> uniqueFindings = new(StringComparer.OrdinalIgnoreCase);

            string absoluteFolder = Path.Combine(PackageTools.ProjectPath, AssetsFolder).NormalizePath();
            if (!Directory.Exists(absoluteFolder)) {
                statusLabel.text = "Folder not found: Assets";
                return;
            }

            foreach (string path in Directory.EnumerateFileSystemEntries(absoluteFolder, "*", SearchOption.AllDirectories)) {
                if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                string assetPath = ToAssetPath(path.NormalizePath());
                string scanText = BuildScanText(path, assetPath);
                foreach (SDKSignature signature in Signatures) {
                    string matchedKeyword = signature.Keywords.FirstOrDefault(keyword => scanText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!string.IsNullOrEmpty(matchedKeyword)) {
                        string folderPath = ResolveSdkFolder(path, assetPath, signature);
                        string uniqueKey = $"{signature.Name}|{folderPath}";
                        if (uniqueFindings.Add(uniqueKey)) {
                            string codeLine = TryGetMatchedCodeLine(path, matchedKeyword);
                            findings.Add(new SDKFinding(signature.Name, signature.Reason, assetPath, folderPath, matchedKeyword, codeLine));
                        }
                        break;
                    }
                }
            }

            statusLabel.text = findings.Count == 0
                ? "No SDK candidates found in Assets."
                : $"Found {findings.Count} SDK/plugin candidates in Assets.";
            RenderResults();
        }

        private void RenderResults() {
            resultsContainer.Clear();

            foreach (SDKFinding finding in findings.OrderBy(finding => finding.Name).ThenBy(finding => finding.Path)) {
                VisualElement row = resultItemTemplate.Instantiate();
                row.Q<Label>("Title").text = $"{finding.Name}  |  {finding.Reason}";
                row.Q<Label>("Path").text = $"Folder: {finding.FolderPath}";

                Label evidence = row.Q<Label>("Evidence");
                if (finding.Path != finding.FolderPath) {
                    evidence.text = $"Matched: {finding.Path}";
                }
                else {
                    evidence.text = string.Empty;
                }

                Button select = row.Q<Button>("SelectButton");
                select.clicked += () => SelectAsset(finding.Path);
                row.RegisterCallback<ClickEvent>(_ => SelectFinding(finding));

                resultsContainer.Add(row);
            }
        }

        private void SelectFinding(SDKFinding finding) {
            inspector.ShowFinding(finding.Name, finding.Reason, finding.FolderPath, finding.Path, finding.Keyword, finding.CodeLine);
            SelectAsset(finding.Path);
        }

        private static string BuildScanText(string systemPath, string assetPath) {
            string text = assetPath;
            if (Directory.Exists(systemPath)) {
                return text;
            }

            string extension = Path.GetExtension(systemPath).ToLowerInvariant();
            if (extension is ".cs" or ".asmdef" or ".json" or ".xml" or ".gradle" or ".properties" or ".txt") {
                try {
                    text += "\n" + File.ReadAllText(systemPath);
                }
                catch (Exception exception) {
                    Logger.CreateError(nameof(SDKAnalyzerView), nameof(BuildScanText), exception.Message);
                }
            }
            return text;
        }

        private static string ResolveSdkFolder(string systemPath, string assetPath, SDKSignature signature) {
            if (Directory.Exists(systemPath)) {
                return assetPath;
            }

            string[] segments = assetPath.Split('/');
            for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++) {
                string segment = segments[segmentIndex];
                foreach (string keyword in signature.Keywords) {
                    string normalizedKeyword = keyword.Trim('/', '\\', '.');
                    if (normalizedKeyword.Length < 2 || normalizedKeyword.Contains('.') || normalizedKeyword.Contains(" ")) {
                        continue;
                    }
                    if (segment.IndexOf(normalizedKeyword, StringComparison.OrdinalIgnoreCase) >= 0) {
                        return string.Join("/", segments.Take(segmentIndex + 1));
                    }
                }
            }

            string directory = Path.GetDirectoryName(assetPath)?.NormalizePath();
            return string.IsNullOrEmpty(directory) ? AssetsFolder : directory.Replace(Path.AltDirectorySeparatorChar, '/');
        }

        private static string TryGetMatchedCodeLine(string systemPath, string keyword) {
            if (!Path.GetExtension(systemPath).Equals(".cs", StringComparison.OrdinalIgnoreCase) || !File.Exists(systemPath)) {
                return string.Empty;
            }

            try {
                string[] lines = File.ReadAllLines(systemPath);
                for (int index = 0; index < lines.Length; index++) {
                    string line = lines[index];
                    if (line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) {
                        string preview = line.Trim();
                        const int maxLength = 260;
                        if (preview.Length > maxLength) {
                            preview = preview.Substring(0, maxLength) + "...";
                        }
                        return $"{index + 1}: {preview}";
                    }
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(SDKAnalyzerView), nameof(TryGetMatchedCodeLine), exception.Message);
            }
            return string.Empty;
        }

        private static void SelectAsset(string assetPath) {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null && Directory.Exists(Path.Combine(PackageTools.ProjectPath, assetPath).NormalizePath())) {
                asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            }
            Selection.activeObject = asset;
            if (asset != null) {
                EditorGUIUtility.PingObject(asset);
            }
        }

        private static string ToAssetPath(string systemPath) {
            string projectPath = PackageTools.ProjectPath;
            string relative = Path.GetRelativePath(projectPath, systemPath).NormalizePath();
            return relative.Replace(Path.AltDirectorySeparatorChar, '/');
        }

        private static readonly SDKSignature[] Signatures = {
            new("Firebase", "Firebase SDK is usually unnecessary for WebGL export cleanup.", "firebase", "FirebaseApp", "FirebaseAnalytics", "google-services.json", "GoogleService-Info.plist"),
            new("Adjust", "Mobile attribution SDK.", "adjust", "AdjustConfig", "com.adjust"),
            new("LevelPlay", "Unity LevelPlay/IronSource mediation SDK.", "LevelPlay", "UnityLevelPlay", "IronSource", "com.unity.services.levelplay", "com.ironsource"),
            new("Appodeal", "Appodeal mobile mediation SDK.", "Appodeal", "AppodealController", "AppodealAds", "com.appodeal.mediation"),
            new("CAS", "Clever Ads Solutions mobile mediation SDK.", "CleverAdsSolutions", "CASFactory", "CASInitSettings", "CAS.", "com.cleversolutions.ads"),
            new("AppLovin", "AppLovin mobile ads SDK.", "AppLovin", "AppLovinSettings", "com.applovin"),
            new("MAX SDK", "AppLovin MAX mobile mediation SDK.", "MaxSdk", "MAXSdk", "MaxSdkCallbacks", "MaxSdkBase"),
            new("GameAnalytics", "External analytics SDK.", "GameAnalytics", "GameAnalyticsSDK"),
            new("YG Plugin", "Yandex Games plugin code.", "YandexGame", "YandexGameSDK", "YG2", "/YG", "\\YG"),
            new("Google Mobile Ads", "Mobile ads SDK.", "GoogleMobileAds", "AdMob", "MobileAds"),
            new("Xsolla", "Payments/login SDK.", "Xsolla", "XPayments"),
            new("AppsFlyer", "Mobile attribution SDK.", "AppsFlyer"),
            new("Facebook SDK", "Social/mobile SDK.", "Facebook.Unity", "FBSDK", "FacebookSDK"),
            new("OneSignal", "Push notification SDK.", "OneSignal"),
            new("Native Android Plugin", "Native Android plugin is not used by WebGL.", ".aar", ".jar", "/Android/", "\\Android\\"),
            new("Native iOS Plugin", "Native iOS plugin is not used by WebGL.", ".framework", ".xcframework", ".podspec", "/iOS/", "\\iOS\\"),
            new("Native Library", "Native plugin is not used by WebGL.", ".so", ".bundle")
        };

        private readonly struct SDKSignature {
            public SDKSignature(string name, string reason, params string[] keywords) {
                Name = name;
                Reason = reason;
                Keywords = keywords;
            }

            public string Name { get; }
            public string Reason { get; }
            public string[] Keywords { get; }
        }

        private readonly struct SDKFinding {
            public SDKFinding(string name, string reason, string path, string folderPath, string keyword, string codeLine) {
                Name = name;
                Reason = reason;
                Path = path;
                FolderPath = folderPath;
                Keyword = keyword;
                CodeLine = codeLine;
            }

            public string Name { get; }
            public string Reason { get; }
            public string Path { get; }
            public string FolderPath { get; }
            public string Keyword { get; }
            public string CodeLine { get; }
        }

    }

}
