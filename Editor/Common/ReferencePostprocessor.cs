using UnityEditor;

namespace PrimeGames.SDK.Editor {

    internal class ReferencePostprocessor : AssetPostprocessor {

        [InitializeOnLoadMethod]
        public static void OnMethodLoad() {
            ExecuteIntegrityChecks();
        }

        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            ExecuteIntegrityChecks();
        }

        private static void ExecuteIntegrityChecks() {
            bool preferencesChanged = PreferencesEditor.CheckFileIntegrity();
            bool visualTreeReferencesChanged = VisualTreeReferenceGenerator.UpdateReferences();
            bool prefabReferencesChanged = PrefabReferenceGenerator.UpdateReferences();
            bool hasChanges = preferencesChanged || visualTreeReferencesChanged || prefabReferencesChanged;
            if (hasChanges) {
                AssetDatabase.Refresh();
            }
        }

    }

}
