using PrimeGames.SDK.Common;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Editor {

    public class SettingsGroup : VisualElement {

        public SettingsGroup() {
            VisualTreeAsset settingsFoldoutTree = VisualTreeReference.LoadVisualTree(nameof(SettingsGroup));
            settingsFoldoutTree.CloneTree(this);
        }

        public override VisualElement contentContainer => this.Q<VisualElement>(Naming.ContentContainer);

    }

}
