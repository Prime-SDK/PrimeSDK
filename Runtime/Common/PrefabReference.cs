using UnityEngine;

namespace PrimeGames.SDK.Common {

    public class PrefabReference : ScriptableObject {

        public static PrefabReference Load(string name) {
            PrefabReference reference = Resources.Load<PrefabReference>($"{Naming.PrimeSDK}/{name}");
#if UNITY_EDITOR
            if (reference == null) {
                return LoadFromAssetDatabase(name);
            }
#endif
            return reference;
        }

        [field: SerializeField] public GameObject Prefab { get; internal set; }

#if UNITY_EDITOR
        private static PrefabReference LoadFromAssetDatabase(string name) {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:PrefabReference {name}");
            foreach (string guid in guids) {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                PrefabReference reference = UnityEditor.AssetDatabase.LoadAssetAtPath<PrefabReference>(assetPath);
                if (reference != null && string.Equals(reference.name, name, System.StringComparison.Ordinal)) {
                    return reference;
                }
            }

            string[] prefabGuids = UnityEditor.AssetDatabase.FindAssets($"t:Prefab {name}");
            foreach (string guid in prefabGuids) {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null && string.Equals(prefab.name, name, System.StringComparison.Ordinal)) {
                    PrefabReference reference = CreateInstance<PrefabReference>();
                    reference.Prefab = prefab;
                    return reference;
                }
            }
            return null;
        }
#endif

    }

}
