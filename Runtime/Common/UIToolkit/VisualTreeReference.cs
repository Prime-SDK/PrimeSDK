using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Common {

    public class VisualTreeReference : ScriptableObject {

        public static VisualTreeReference Load(string name) {
            return Resources.Load<VisualTreeReference>($"{Naming.PrimeSDK}/{name}");
        }

        public static VisualTreeAsset LoadVisualTree(string name) {
            VisualTreeReference reference = Load(name);
            if (TryGetVisualTree(reference, out VisualTreeAsset visualTree)) {
                return visualTree;
            }
#if UNITY_EDITOR
            return LoadVisualTreeFromAssetDatabase(name);
#else
            return null;
#endif
        }

        [field: SerializeField] public VisualTreeAsset VisualTree { get; internal set; }

        private static bool TryGetVisualTree(VisualTreeReference reference, out VisualTreeAsset visualTree) {
            visualTree = null;
            if (reference == null) {
                return false;
            }
            try {
                visualTree = reference.VisualTree;
                return visualTree != null;
            }
            catch (MissingReferenceException) {
                return false;
            }
        }

#if UNITY_EDITOR
        private static VisualTreeAsset LoadVisualTreeFromAssetDatabase(string name) {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:VisualTreeAsset {name}");
            foreach (string guid in guids) {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                VisualTreeAsset visualTree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
                if (visualTree != null && string.Equals(visualTree.name, name, StringComparison.Ordinal)) {
                    return visualTree;
                }
            }
            return null;
        }
#endif

    }

}
