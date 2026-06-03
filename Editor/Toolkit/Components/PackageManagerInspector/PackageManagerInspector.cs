using PrimeGames.SDK.Common;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Editor
{
    internal partial class PackageManagerInspector : VisualElement
    {
        public PackageManagerInspector()
        {
            VisualTreeAsset asset = VisualTreeReference.LoadVisualTree(nameof(PackageManagerInspector));
            asset.CloneTree(this);

            DescriptionLabel.text = Naming.Dash;
            ReadmeLabel.text = Naming.Dash;
        }

        public VisualElement ActionButtonsElement
        {
            get => this.Q<VisualElement>(nameof(ActionButtonsElement));
        }

        public Label DescriptionLabel
        {
            get => this.Q<Label>(nameof(DescriptionLabel));
        }

        public Label ReadmeLabel
        {
            get => this.Q<Label>(nameof(ReadmeLabel));
        }
    }
}
