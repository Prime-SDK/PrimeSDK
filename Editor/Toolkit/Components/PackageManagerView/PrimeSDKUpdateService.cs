using PrimeGames.SDK.Common;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.Editor {

    internal static class PrimeSDKUpdateService {

        private const string RepositoryHandle = "Prime-SDK/PrimeSDK";
        private const string BranchName = "main";
        private const int RequestTimeoutSeconds = 10;

        public static async Task<PackageInfo> GetLatestPackageInfo() {
            byte[] data = await Get(GetPackageFileUrl("package.json"));
            if (data == null) {
                return null;
            }
            try {
                return JsonUtility.FromJson<PackageInfo>(Encoding.UTF8.GetString(data));
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeSDKUpdateService), nameof(GetLatestPackageInfo), exception.Message);
                return null;
            }
        }

        public static async Task<PackageDependencies> GetLatestDependencies() {
            byte[] data = await Get(GetPackageFileUrl("dependencies.json"), logRequestFailure: false);
            if (data == null) {
                return null;
            }
            try {
                return JsonUtility.FromJson<PackageDependencies>(Encoding.UTF8.GetString(data));
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeSDKUpdateService), nameof(GetLatestDependencies), exception.Message);
                return null;
            }
        }

        public static async Task<PrimeSDKBacklog> GetLatestBacklog() {
            byte[] data = await Get(GetPackageFileUrl("backlog.json"), logRequestFailure: false);
            if (data == null) {
                return null;
            }
            return ParseBacklog(Encoding.UTF8.GetString(data), nameof(GetLatestBacklog));
        }

        public static PrimeSDKBacklog GetLocalBacklog() {
            string backlogPath = $"{PackageFiles.RelativePackageDatabasePath}/backlog.json";
            TextAsset backlogAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(backlogPath);
            if (backlogAsset == null) {
                return null;
            }
            return ParseBacklog(backlogAsset.text, nameof(GetLocalBacklog));
        }

        public static async Task<bool> UpdatePrimeSDK(PackageInfo packageInfo = null, PackageDependencies dependencies = null) {
            packageInfo ??= await GetLatestPackageInfo();
            if (packageInfo == null) {
                Logger.CreateError(nameof(PrimeSDKUpdateService), nameof(UpdatePrimeSDK), "Unable to load PrimeSDK package info");
                return false;
            }
            dependencies ??= await GetLatestDependencies();
            return await InstallPrimeSDK(packageInfo, dependencies);
        }

        public static bool IsUpdateAvailable(string installedVersion, string availableVersion) {
            return CompareVersions(installedVersion, availableVersion) < 0;
        }

        public static int CompareVersions(string installedVersion, string availableVersion) {
            string[] installedParts = GetStableVersion(installedVersion).Split('.');
            string[] availableParts = GetStableVersion(availableVersion).Split('.');
            int partsCount = Mathf.Max(installedParts.Length, availableParts.Length);
            for (int i = 0; i < partsCount; i++) {
                int installedPart = GetVersionPart(installedParts, i);
                int availablePart = GetVersionPart(availableParts, i);
                if (installedPart != availablePart) {
                    return installedPart.CompareTo(availablePart);
                }
            }
            return 0;
        }

        private static async Task<bool> InstallPrimeSDK(PackageInfo packageInfo, PackageDependencies dependencies) {
            if (dependencies != null) {
                if (dependencies.TarballUrls != null) {
                    foreach (string tarballUrl in dependencies.TarballUrls) {
                        if (!await UnityPackageManager.ImportFromTarball(tarballUrl)) {
                            return false;
                        }
                    }
                }
                if (dependencies.RegistryPackages != null) {
                    foreach (PackageRegistryDependency registryPackage in dependencies.RegistryPackages) {
                        if (!await UnityPackageManager.ImportFromRegistry(registryPackage)) {
                            return false;
                        }
                    }
                }
                if (dependencies.GitUrls != null) {
                    foreach (string gitUrl in dependencies.GitUrls) {
                        if (!await UnityPackageManager.ImportFromGit(gitUrl)) {
                            return false;
                        }
                    }
                }
                if (dependencies.GitPackages != null) {
                    foreach (PackageGitDependency gitPackage in dependencies.GitPackages) {
                        if (!await UnityPackageManager.ImportFromGit(gitPackage.Url, gitPackage.Name)) {
                            return false;
                        }
                    }
                }
                if (dependencies.UnityPackages != null) {
                    foreach (string unityPackageUrl in dependencies.UnityPackages) {
                        await UnityPackageManager.ImportFromUnityPackage(unityPackageUrl, false);
                    }
                }
                if (dependencies.WebGLTemplates != null) {
                    foreach (PackageWebGLTemplateDependency template in dependencies.WebGLTemplates) {
                        template.Version = packageInfo.version;
                        if (!await UnityPackageManager.ImportWebGLTemplate(template)) {
                            return false;
                        }
                    }
                    await UnityPackageManager.RemovePackageIfInstalled(packageInfo.name);
                }
            }
            if (dependencies == null || !dependencies.SkipPackageInstall) {
                return await UnityPackageManager.ImportFromGit(GetPackageGitUrl(), packageInfo.name, false);
            }
            return true;
        }

        private static string GetPackageFileUrl(string fileName) {
            return $"https://raw.githubusercontent.com/{RepositoryHandle}/refs/heads/{BranchName}/{fileName}";
        }

        private static string GetPackageGitUrl() {
            return $"https://github.com/{RepositoryHandle}.git#{BranchName}";
        }

        private static async Task<byte[]> Get(string url, bool logRequestFailure = true) {
            Logger.CreateText(nameof(PrimeSDKUpdateService), nameof(Get), Naming.Quote(url));
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            UnityWebRequestAsyncOperation asyncOperation = webRequest.SendWebRequest();
            DateTime timeoutDateTime = DateTime.UtcNow.AddSeconds(RequestTimeoutSeconds);
            while (!asyncOperation.isDone && DateTime.UtcNow < timeoutDateTime) {
                await Task.Delay(100);
            }
            if (!asyncOperation.isDone) {
                webRequest.Abort();
                Logger.CreateWarning(nameof(PrimeSDKUpdateService), nameof(Get), "Request timed out", Naming.Quote(url));
                return null;
            }
            if (webRequest.result != UnityWebRequest.Result.Success) {
                if (logRequestFailure) {
                    Logger.CreateWarning(nameof(PrimeSDKUpdateService), nameof(Get), "Request failed", Naming.Quote(webRequest.error), Naming.Quote(url));
                }
                return null;
            }
            return webRequest.downloadHandler.data;
        }

        private static string GetStableVersion(string version) {
            if (string.IsNullOrEmpty(version)) {
                return "0";
            }
            return version.Trim().TrimStart('v', 'V').Split('-')[0];
        }

        private static int GetVersionPart(string[] parts, int index) {
            if (index >= parts.Length) {
                return 0;
            }
            return int.TryParse(parts[index], out int value) ? value : 0;
        }

        private static PrimeSDKBacklog ParseBacklog(string json, string context) {
            try {
                return JsonUtility.FromJson<PrimeSDKBacklog>(json);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeSDKUpdateService), context, exception.Message);
                return null;
            }
        }

    }

    [Serializable]
    internal sealed class PrimeSDKBacklog {

        public PrimeSDKReleaseNoteInfo[] Releases;

    }

    [Serializable]
    internal sealed class PrimeSDKReleaseNoteInfo {

        public string Version;
        public string Title;
        public string[] Ru;
        public string[] En;

    }

}
