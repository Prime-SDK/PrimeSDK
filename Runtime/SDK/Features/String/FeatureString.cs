using System;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Common {

    public class FeatureString : VisualElement {

        public FeatureString(string text, Func<string> getter) {
            VisualTreeAsset featureStringAsset = VisualTreeReference.LoadVisualTree(nameof(FeatureString));
            featureStringAsset.CloneTree(this);

            Getter = getter;
            PropertyName = text;
            PropertyValue = default;
        }

        private Func<string> Getter { get; }

        public string PropertyName {
            get => this.Q<Label>("property-name").text;
            set => this.Q<Label>("property-name").text = value;
        }

        public string PropertyValue {
            get => this.Q<Label>("property-value").text;
            set => this.Q<Label>("property-value").text = value;
        }

        public void UpdateValue() {
            PropertyValue = Getter();
        }

    }

}
