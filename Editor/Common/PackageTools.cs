using PrimeGames.SDK.Common;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace PrimeGames.SDK.Editor {

    internal static class PackageTools {

        public static string ProjectId {
            get => Application.dataPath.GetHashCode().ToString();
        }

        public static string ProjectPath {
            get {
                string projectPath = Application.dataPath;
                int assetsIndex = projectPath.LastIndexOf("/Assets");
                if (assetsIndex >= 0) {
                    projectPath = projectPath.Substring(0, assetsIndex);
                }
                return projectPath.NormalizePath();
            }
        }

        public static string GetPrefsString(string key, string defaultValue = "") {
            return EditorPrefs.GetString($"{Naming.PrimeSDK}[{ProjectId}].{key}", defaultValue);
        }

        public static void SetPrefsString(string key, string value) {
            EditorPrefs.SetString($"{Naming.PrimeSDK}[{ProjectId}].{key}", value);
        }

        public static bool GetPrefsBool(string key, bool defaultValue = false) {
            return EditorPrefs.GetBool($"{Naming.PrimeSDK}[{ProjectId}].{key}", defaultValue);
        }

        public static void SetPrefsBool(string key, bool value) {
            EditorPrefs.SetBool($"{Naming.PrimeSDK}[{ProjectId}].{key}", value);
        }

    }

}
