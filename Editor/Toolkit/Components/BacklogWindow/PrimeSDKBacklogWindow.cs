using PrimeGames.SDK.Common;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Editor {

    internal sealed class PrimeSDKBacklogWindow : EditorWindow {

        private const string DocumentationUrl = "https://prime-publisher.com/sdk/";

        private ReleaseNote[] releaseNotes = Array.Empty<ReleaseNote>();
        private Label statusLabel;
        private Label installedVersionLabel;
        private Label availableVersionLabel;
        private Button updateButton;
        private PackageInfo latestPackageInfo;

        [MenuItem(Naming.PrimeSDK + "/Backlog")]
        public static void Open() {
            Open(null);
        }

        public static void Open(PackageInfo packageInfo) {
            PrimeSDKBacklogWindow window = GetWindow<PrimeSDKBacklogWindow>();
            window.titleContent = new GUIContent("PrimeSDK Backlog");
            window.minSize = new Vector2(760, 560);
            window.latestPackageInfo = packageInfo;
            window.Show();
            window.RefreshState();
        }

        private void OnEnable() {
            SetReleaseNotes(PrimeSDKUpdateService.GetLocalBacklog());
            BuildView();
            RefreshState();
            RefreshBacklog();
        }

        private void BuildView() {
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1;
            rootVisualElement.style.backgroundColor = new Color(0.035f, 0.039f, 0.043f);
            rootVisualElement.style.color = new Color(0.86f, 0.86f, 0.86f);

            VisualElement header = CreateHeader();
            rootVisualElement.Add(header);

            ScrollView scrollView = new(ScrollViewMode.Vertical) {
                style = {
                    flexGrow = 1,
                    paddingLeft = 18,
                    paddingRight = 18,
                    paddingTop = 14,
                    paddingBottom = 14
                }
            };

            foreach (ReleaseNote releaseNote in releaseNotes) {
                scrollView.Add(CreateReleaseNote(releaseNote));
            }
            if (releaseNotes.Length == 0) {
                scrollView.Add(CreateEmptyState());
            }

            rootVisualElement.Add(scrollView);
            rootVisualElement.Add(CreateFooter());
        }

        private VisualElement CreateHeader() {
            VisualElement header = new() {
                style = {
                    flexShrink = 0,
                    paddingLeft = 20,
                    paddingRight = 20,
                    paddingTop = 18,
                    paddingBottom = 16,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(1.0f, 0.42f, 0.0f, 0.85f),
                    backgroundColor = new Color(0.045f, 0.049f, 0.055f)
                }
            };

            Label title = new("PrimeSDK Backlog") {
                style = {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 22,
                    color = new Color(1.0f, 0.49f, 0.09f)
                }
            };
            Label subtitle = new("Release notes are shown in RU and EN. Use this window to review SDK changes before updating.") {
                style = {
                    marginTop = 6,
                    whiteSpace = WhiteSpace.Normal,
                    color = new Color(0.70f, 0.70f, 0.70f)
                }
            };

            VisualElement versionsRow = new() {
                style = {
                    flexDirection = FlexDirection.Row,
                    marginTop = 14
                }
            };
            installedVersionLabel = CreateVersionBadge("Installed", PrimeSDK.Version);
            availableVersionLabel = CreateVersionBadge("Available", Naming.Dash);
            versionsRow.Add(installedVersionLabel);
            versionsRow.Add(availableVersionLabel);
            versionsRow.Add(CreateDocumentationButton());

            header.Add(title);
            header.Add(subtitle);
            header.Add(versionsRow);
            return header;
        }

        private VisualElement CreateReleaseNote(ReleaseNote releaseNote) {
            VisualElement card = new() {
                style = {
                    marginBottom = 12,
                    paddingLeft = 14,
                    paddingRight = 14,
                    paddingTop = 12,
                    paddingBottom = 12,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftColor = new Color(1.0f, 0.42f, 0.0f, 0.28f),
                    borderRightColor = new Color(1.0f, 0.42f, 0.0f, 0.28f),
                    borderTopColor = new Color(1.0f, 0.42f, 0.0f, 0.28f),
                    borderBottomColor = new Color(1.0f, 0.42f, 0.0f, 0.28f),
                    backgroundColor = new Color(0.058f, 0.064f, 0.070f)
                }
            };

            Label versionLabel = new($"PrimeSDK {releaseNote.Version} | {releaseNote.Title}") {
                style = {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 15,
                    color = new Color(1.0f, 0.49f, 0.09f),
                    marginBottom = 10
                }
            };
            card.Add(versionLabel);

            VisualElement languageRow = new() {
                style = {
                    flexDirection = FlexDirection.Row
                }
            };
            languageRow.Add(CreateLanguageColumn("RU", releaseNote.RuChanges));
            languageRow.Add(CreateLanguageColumn("EN", releaseNote.EnChanges));
            card.Add(languageRow);

            return card;
        }

        private VisualElement CreateEmptyState() {
            Label emptyLabel = new("No backlog entries found.") {
                style = {
                    marginTop = 10,
                    color = new Color(0.70f, 0.70f, 0.70f)
                }
            };
            return emptyLabel;
        }

        private VisualElement CreateLanguageColumn(string language, string[] changes) {
            VisualElement column = new() {
                style = {
                    flexGrow = 1,
                    flexBasis = 0,
                    paddingRight = 12
                }
            };
            Label languageLabel = new(language) {
                style = {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.86f, 0.86f, 0.86f),
                    marginBottom = 6
                }
            };
            column.Add(languageLabel);

            foreach (string change in changes) {
                Label changeLabel = new($"- {change}") {
                    style = {
                        whiteSpace = WhiteSpace.Normal,
                        marginBottom = 4,
                        color = new Color(0.76f, 0.76f, 0.76f)
                    }
                };
                column.Add(changeLabel);
            }
            return column;
        }

        private VisualElement CreateFooter() {
            VisualElement footer = new() {
                style = {
                    flexShrink = 0,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 20,
                    paddingRight = 20,
                    paddingTop = 12,
                    paddingBottom = 12,
                    borderTopWidth = 1,
                    borderTopColor = new Color(1.0f, 0.42f, 0.0f, 0.85f),
                    backgroundColor = new Color(0.050f, 0.054f, 0.060f)
                }
            };

            statusLabel = new("Checking for updates...") {
                style = {
                    flexGrow = 1,
                    color = new Color(0.70f, 0.70f, 0.70f),
                    whiteSpace = WhiteSpace.Normal
                }
            };

            Button copyButton = new(CopyBacklogText) {
                text = "Copy text"
            };
            copyButton.style.minWidth = 100;
            copyButton.style.marginRight = 8;

            updateButton = new(UpdatePrimeSDK) {
                text = "Обновить"
            };
            updateButton.SetEnabled(false);
            updateButton.style.minWidth = 120;

            Button closeButton = new(Close) {
                text = "Close"
            };
            closeButton.style.marginLeft = 8;
            closeButton.style.minWidth = 90;

            footer.Add(statusLabel);
            footer.Add(copyButton);
            footer.Add(updateButton);
            footer.Add(closeButton);
            return footer;
        }

        private Label CreateVersionBadge(string title, string value) {
            Label label = new($"{title}: {value}") {
                style = {
                    marginRight = 10,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 4,
                    paddingBottom = 4,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftColor = new Color(1.0f, 0.42f, 0.0f, 0.35f),
                    borderRightColor = new Color(1.0f, 0.42f, 0.0f, 0.35f),
                    borderTopColor = new Color(1.0f, 0.42f, 0.0f, 0.35f),
                    borderBottomColor = new Color(1.0f, 0.42f, 0.0f, 0.35f),
                    backgroundColor = new Color(0.075f, 0.078f, 0.084f),
                    color = new Color(0.84f, 0.84f, 0.84f)
                }
            };
            return label;
        }

        private Button CreateDocumentationButton() {
            Button button = new(OpenDocumentation) {
                text = "Documentation"
            };
            button.style.minWidth = 116;
            button.style.height = 25;
            button.style.marginRight = 10;
            button.style.paddingLeft = 10;
            button.style.paddingRight = 10;
            button.style.paddingTop = 3;
            button.style.paddingBottom = 3;
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderTopWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftColor = new Color(1.0f, 0.42f, 0.0f, 0.65f);
            button.style.borderRightColor = new Color(1.0f, 0.42f, 0.0f, 0.65f);
            button.style.borderTopColor = new Color(1.0f, 0.42f, 0.0f, 0.65f);
            button.style.borderBottomColor = new Color(1.0f, 0.42f, 0.0f, 0.65f);
            button.style.backgroundColor = new Color(0.075f, 0.078f, 0.084f);
            button.style.color = new Color(1.0f, 0.49f, 0.09f);
            button.tooltip = DocumentationUrl;
            return button;
        }

        private async void RefreshState() {
            if (statusLabel == null) {
                return;
            }
            installedVersionLabel.text = $"Installed: {PrimeSDK.Version}";
            statusLabel.text = "Checking for updates...";
            updateButton.SetEnabled(false);

            latestPackageInfo ??= await PrimeSDKUpdateService.GetLatestPackageInfo();
            string availableVersion = latestPackageInfo?.version ?? PrimeSDK.Version;
            int versionComparison = latestPackageInfo == null ? 0 : PrimeSDKUpdateService.CompareVersions(PrimeSDK.Version, availableVersion);
            bool updateAvailable = versionComparison < 0;

            availableVersionLabel.text = $"Available: {availableVersion}";
            updateButton.SetEnabled(updateAvailable);
            updateButton.text = updateAvailable ? "Обновить" : "Up to date";
            if (updateAvailable) {
                statusLabel.text = $"PrimeSDK {availableVersion} is available. Review the changes and update when ready.";
            }
            else if (versionComparison > 0) {
                statusLabel.text = "Installed PrimeSDK is newer than the public version currently available.";
            }
            else {
                statusLabel.text = "PrimeSDK is already updated to the latest available version.";
            }
        }

        private void CopyBacklogText() {
            EditorGUIUtility.systemCopyBuffer = BuildBacklogText();
            statusLabel.text = "Backlog text copied to clipboard.";
        }

        private void OpenDocumentation() {
            Application.OpenURL(DocumentationUrl);
        }

        private string BuildBacklogText() {
            System.Text.StringBuilder builder = new();
            builder.AppendLine("PrimeSDK Backlog");
            builder.AppendLine();
            foreach (ReleaseNote releaseNote in releaseNotes) {
                builder.AppendLine($"PrimeSDK {releaseNote.Version} | {releaseNote.Title}");
                builder.AppendLine("RU:");
                foreach (string change in releaseNote.RuChanges) {
                    builder.AppendLine($"- {change}");
                }
                builder.AppendLine("EN:");
                foreach (string change in releaseNote.EnChanges) {
                    builder.AppendLine($"- {change}");
                }
                builder.AppendLine();
            }
            return builder.ToString().TrimEnd();
        }

        private async void RefreshBacklog() {
            PrimeSDKBacklog backlog = await PrimeSDKUpdateService.GetLatestBacklog();
            if (!SetReleaseNotes(backlog)) {
                return;
            }
            BuildView();
            RefreshState();
        }

        private bool SetReleaseNotes(PrimeSDKBacklog backlog) {
            if (backlog?.Releases == null || backlog.Releases.Length == 0) {
                return false;
            }

            ReleaseNote[] remoteReleaseNotes = new ReleaseNote[backlog.Releases.Length];
            for (int i = 0; i < backlog.Releases.Length; i++) {
                PrimeSDKReleaseNoteInfo release = backlog.Releases[i];
                remoteReleaseNotes[i] = new ReleaseNote(
                    release.Version,
                    release.Title,
                    release.Ru ?? Array.Empty<string>(),
                    release.En ?? Array.Empty<string>()
                );
            }
            releaseNotes = remoteReleaseNotes;
            return true;
        }

        private async void UpdatePrimeSDK() {
            updateButton.SetEnabled(false);
            updateButton.text = "Обновление...";
            statusLabel.text = "Updating PrimeSDK through Unity Package Manager...";

            bool success = await PrimeSDKUpdateService.UpdatePrimeSDK(latestPackageInfo);
            statusLabel.text = success
                ? "Update request completed. Unity may reload assemblies to finish installation."
                : "Update failed. Check the Unity Console for details.";
            updateButton.text = "Обновить";
            await Task.Delay(200);
            RefreshState();
        }

        private readonly struct ReleaseNote {

            public ReleaseNote(string version, string title, string[] ruChanges, string[] enChanges) {
                Version = version;
                Title = title;
                RuChanges = ruChanges;
                EnChanges = enChanges;
            }

            public string Version { get; }
            public string Title { get; }
            public string[] RuChanges { get; }
            public string[] EnChanges { get; }

        }

    }

}
