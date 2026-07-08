using PrimeGames.SDK.Common;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace PrimeGames.SDK.Editor {

    [InitializeOnLoad]
    internal static class PrimeSDKBacklogAutoPopup {

        private const string LastInstalledVersionKey = "PrimeSDKBacklog.LastInstalledVersion";
        private const string LastShownAvailableVersionKey = "PrimeSDKBacklog.LastShownAvailableVersion";

        private static bool checkStarted;

        static PrimeSDKBacklogAutoPopup() {
            EditorApplication.delayCall += CheckForBacklog;
        }

        private static async void CheckForBacklog() {
            if (checkStarted || Application.isBatchMode) {
                return;
            }
            checkStarted = true;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
                checkStarted = false;
                EditorApplication.delayCall += CheckForBacklog;
                return;
            }

            await Task.Delay(1000);

            string installedVersion = PrimeSDK.Version;
            string lastInstalledVersion = PackageTools.GetPrefsString(LastInstalledVersionKey);
            bool installedVersionChanged = lastInstalledVersion != installedVersion;

            PackageInfo packageInfo = await PrimeSDKUpdateService.GetLatestPackageInfo();
            string availableVersion = packageInfo?.version;
            bool updateAvailable = !string.IsNullOrEmpty(availableVersion)
                                   && PrimeSDKUpdateService.IsUpdateAvailable(installedVersion, availableVersion);
            bool newAvailableVersion = updateAvailable
                                       && PackageTools.GetPrefsString(LastShownAvailableVersionKey) != availableVersion;

            if (!installedVersionChanged && !newAvailableVersion) {
                return;
            }

            PackageTools.SetPrefsString(LastInstalledVersionKey, installedVersion);
            if (newAvailableVersion) {
                PackageTools.SetPrefsString(LastShownAvailableVersionKey, availableVersion);
            }
            PrimeSDKBacklogWindow.Open(packageInfo);
        }

    }

}
