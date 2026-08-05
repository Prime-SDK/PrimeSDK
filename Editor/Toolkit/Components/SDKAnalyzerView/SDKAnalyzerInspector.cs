using PrimeGames.SDK.Common;
using UnityEditor;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Editor {

    internal sealed class SDKAnalyzerInspector : VisualElement {

        private readonly Label titleLabel;
        private readonly Label reasonLabel;
        private readonly Label folderLabel;
        private readonly Label matchedLabel;
        private readonly Label keywordLabel;
        private readonly Label codeLineTitleLabel;
        private readonly Label codeLineLabel;
        private readonly Button selectButton;

        private string selectedPath;

        public SDKAnalyzerInspector() {
            VisualTreeAsset asset = VisualTreeReference.LoadVisualTree(nameof(SDKAnalyzerInspector));
            asset.CloneTree(this);

            titleLabel = this.Q<Label>("Title");
            reasonLabel = this.Q<Label>("Reason");
            folderLabel = this.Q<Label>("Folder");
            matchedLabel = this.Q<Label>("Matched");
            keywordLabel = this.Q<Label>("Keyword");
            codeLineTitleLabel = this.Q<Label>("CodeLineTitle");
            codeLineLabel = this.Q<Label>("CodeLine");
            selectButton = this.Q<Button>("SelectButton");
            selectButton.clicked += SelectMatchedAsset;
            selectButton.SetEnabled(false);
        }

        public void ShowFinding(string name, string reason, string folderPath, string matchedPath, string keyword, string codeLine) {
            selectedPath = matchedPath;
            titleLabel.text = name;
            reasonLabel.text = reason;
            folderLabel.text = $"Folder: {folderPath}";
            matchedLabel.text = $"Matched: {matchedPath}";
            keywordLabel.text = $"Reason keyword: {keyword}";
            codeLineTitleLabel.text = string.IsNullOrEmpty(codeLine) ? string.Empty : "Code line";
            codeLineLabel.text = codeLine;
            selectButton.SetEnabled(!string.IsNullOrEmpty(selectedPath));
        }

        public void ClearSelection() {
            selectedPath = null;
            titleLabel.text = "No SDK selected";
            reasonLabel.text = "Select an item from the analyzer list to see why it was highlighted.";
            folderLabel.text = string.Empty;
            matchedLabel.text = string.Empty;
            keywordLabel.text = string.Empty;
            codeLineTitleLabel.text = string.Empty;
            codeLineLabel.text = string.Empty;
            selectButton.SetEnabled(false);
        }

        private void SelectMatchedAsset() {
            if (string.IsNullOrEmpty(selectedPath)) {
                return;
            }

            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectedPath);
            Selection.activeObject = asset;
            if (asset != null) {
                EditorGUIUtility.PingObject(asset);
            }
        }
    }

}
