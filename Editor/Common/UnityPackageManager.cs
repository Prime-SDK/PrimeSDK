using PrimeGames.SDK.Common;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.Editor
{
    public static class UnityPackageManager
    {
        private const string ExternalPackagesFolder = "ExternalPackages";
        private const int DownloadTimeoutSeconds = 120;
        private const int UpmTimeoutSeconds = 300;
        private const int PollIntervalMs = 100;
        private const int ExclusiveAccessRetryCount = 30;
        private const int ExclusiveAccessRetryDelayMs = 1000;
        private static readonly SemaphoreSlim PackageManagerSemaphore = new(1, 1);
        private static readonly HttpClient HttpClient = new();

        public static async Task ImportFromUnityPackage(string unityPackageUrl, bool interactive = true)
        {
            Logger.CreateText(nameof(UnityPackageManager), nameof(ImportFromUnityPackage), "Downloading", Naming.Quote(unityPackageUrl));

            string projectPath = GetProjectPath();
            string externalPackagesPath = Path.Combine(projectPath, ExternalPackagesFolder);

            if (!Directory.Exists(externalPackagesPath))
            {
                Directory.CreateDirectory(externalPackagesPath);
            }

            string fileName = GetFileNameFromUrl(unityPackageUrl, ".unitypackage");
            string localFilePath = Path.Combine(externalPackagesPath, fileName).Replace('\\', '/');

            if (File.Exists(localFilePath))
            {
                File.Delete(localFilePath);
            }

            try
            {
                DownloadHandlerFile downloadHandler = new(localFilePath)
                {
                    removeFileOnAbort = true
                };

                using UnityWebRequest request = new(unityPackageUrl, UnityWebRequest.kHttpVerbGET)
                {
                    downloadHandler = downloadHandler,
                    redirectLimit = 10,
                    timeout = DownloadTimeoutSeconds
                };

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                DateTime timeoutTime = DateTime.UtcNow.AddSeconds(DownloadTimeoutSeconds);

                while (!operation.isDone && DateTime.UtcNow < timeoutTime)
                {
                    float progress = request.downloadProgress;
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Importing Package",
                        $"Downloading {fileName}... {progress * 100f:F0}%",
                        progress))
                    {
                        request.Abort();
                        EditorUtility.ClearProgressBar();
                        Logger.CreateWarning(nameof(UnityPackageManager), nameof(ImportFromUnityPackage), "Download cancelled by user");
                        return;
                    }
                    await Task.Delay(PollIntervalMs);
                }

                if (!operation.isDone)
                {
                    request.Abort();
                    EditorUtility.ClearProgressBar();
                    Logger.CreateError(nameof(UnityPackageManager), nameof(ImportFromUnityPackage), "Download timed out", Naming.Quote(unityPackageUrl));
                    return;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    EditorUtility.ClearProgressBar();
                    Logger.CreateError(nameof(UnityPackageManager), nameof(ImportFromUnityPackage),
                        "Download failed", Naming.Quote(request.error), Naming.Quote(unityPackageUrl));
                    return;
                }

                EditorUtility.ClearProgressBar();

                Logger.CreateText(nameof(UnityPackageManager), nameof(ImportFromUnityPackage),
                    "Opening import dialog", Naming.Quote(localFilePath));

                AssetDatabase.ImportPackage(localFilePath, interactive);
                ApplyUnityPackagePostImportFixes(unityPackageUrl);
            }
            catch (Exception exception)
            {
                Logger.CreateError(nameof(UnityPackageManager), nameof(ImportFromUnityPackage), exception.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void ApplyUnityPackagePostImportFixes(string unityPackageUrl)
        {
            if (unityPackageUrl.IndexOf("CrazyGamesSDK", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            EnsureCrazyGamesSdkAssemblyDefinitions();
        }

        private static void EnsureCrazyGamesSdkAssemblyDefinitions()
        {
            const string crazySdkFolder = "Assets/CrazySDK";
            const string crazySdkAssemblyPath = crazySdkFolder + "/CrazySDK.asmdef";
            const string crazySdkEditorAssemblyPath = crazySdkFolder + "/Scripts/Editor/CrazySDK.Editor.asmdef";

            if (!AssetDatabase.IsValidFolder(crazySdkFolder))
            {
                Logger.CreateWarning(nameof(UnityPackageManager), nameof(EnsureCrazyGamesSdkAssemblyDefinitions),
                    "CrazySDK folder was not found after import", Naming.Quote(crazySdkFolder));
                return;
            }

            WriteTextAssetIfMissing(crazySdkAssemblyPath,
                "{\n" +
                "    \"name\": \"CrazySDK\",\n" +
                "    \"rootNamespace\": \"CrazyGames\",\n" +
                "    \"references\": [],\n" +
                "    \"includePlatforms\": [],\n" +
                "    \"excludePlatforms\": [],\n" +
                "    \"allowUnsafeCode\": false,\n" +
                "    \"overrideReferences\": false,\n" +
                "    \"precompiledReferences\": [],\n" +
                "    \"autoReferenced\": true,\n" +
                "    \"defineConstraints\": [],\n" +
                "    \"versionDefines\": [],\n" +
                "    \"noEngineReferences\": false\n" +
                "}\n");

            WriteTextAssetIfMissing(crazySdkEditorAssemblyPath,
                "{\n" +
                "    \"name\": \"CrazySDK.Editor\",\n" +
                "    \"rootNamespace\": \"CrazyGames.Editor\",\n" +
                "    \"references\": [\n" +
                "        \"CrazySDK\"\n" +
                "    ],\n" +
                "    \"includePlatforms\": [\n" +
                "        \"Editor\"\n" +
                "    ],\n" +
                "    \"excludePlatforms\": [],\n" +
                "    \"allowUnsafeCode\": false,\n" +
                "    \"overrideReferences\": false,\n" +
                "    \"precompiledReferences\": [],\n" +
                "    \"autoReferenced\": true,\n" +
                "    \"defineConstraints\": [],\n" +
                "    \"versionDefines\": [],\n" +
                "    \"noEngineReferences\": false\n" +
                "}\n");

            AssetDatabase.Refresh();
        }

        private static void WriteTextAssetIfMissing(string assetPath, string content)
        {
            if (File.Exists(assetPath))
            {
                return;
            }

            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(assetPath, content, Encoding.UTF8);
            Logger.CreateText(nameof(UnityPackageManager), nameof(WriteTextAssetIfMissing),
                "Created asset", Naming.Quote(assetPath));
        }

        public static async Task<bool> ImportFromTarball(string tarballUrl, string packageName = null)
        {
            if (!string.IsNullOrEmpty(packageName) && IsPackageInstalled(packageName))
            {
                Logger.CreateText(nameof(UnityPackageManager), nameof(ImportFromTarball),
                    "Package already installed, skipping", Naming.Quote(packageName));
                return true;
            }

            Logger.CreateText(nameof(UnityPackageManager), nameof(ImportFromTarball), "Downloading", Naming.Quote(tarballUrl));

            string projectPath = GetProjectPath();
            string externalPackagesPath = Path.Combine(projectPath, ExternalPackagesFolder);

            if (!Directory.Exists(externalPackagesPath))
            {
                Directory.CreateDirectory(externalPackagesPath);
            }

            string fileName = GetFileNameFromUrl(tarballUrl);
            string localFilePath = Path.Combine(externalPackagesPath, fileName).Replace('\\', '/');

            try
            {
                if (File.Exists(localFilePath))
                {
                    File.Delete(localFilePath);
                }

                using HttpResponseMessage response = await HttpClient.GetAsync(tarballUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.CreateError(nameof(UnityPackageManager), nameof(ImportFromTarball),
                        "Download failed", response.StatusCode.ToString(), Naming.Quote(tarballUrl));
                    return false;
                }

                byte[] data = await response.Content.ReadAsByteArrayAsync();
                File.WriteAllBytes(localFilePath, data);

                string packageIdentifier = $"file:{localFilePath}";
                Logger.CreateText(nameof(UnityPackageManager), nameof(ImportFromTarball),
                    "Adding package", Naming.Quote(packageIdentifier));

                return await AddPackage(packageIdentifier, packageName);
            }
            catch (Exception exception)
            {
                Logger.CreateError(nameof(UnityPackageManager), nameof(ImportFromTarball), exception.Message);
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static async Task<bool> ImportFromRegistry(PackageRegistryDependency package)
        {
            if (package == null || string.IsNullOrEmpty(package.Name))
            {
                Logger.CreateError(nameof(UnityPackageManager), nameof(ImportFromRegistry), "Invalid registry package dependency");
                return false;
            }

            if (IsPackageInstalled(package.Name))
            {
                Logger.CreateText(nameof(UnityPackageManager), nameof(ImportFromRegistry),
                    "Package already installed, skipping", Naming.Quote(package.Name));
                return true;
            }

            if (!EnsureScopedRegistry(package))
            {
                return false;
            }

            string packageIdentifier = string.IsNullOrEmpty(package.Version)
                ? package.Name
                : $"{package.Name}@{package.Version}";

            Logger.CreateText(nameof(UnityPackageManager), nameof(ImportFromRegistry),
                "Adding registry package", Naming.Quote(packageIdentifier));

            return await AddPackage(packageIdentifier, package.Name);
        }

        public static async Task<bool> ImportWebGLTemplate(PackageWebGLTemplateDependency template)
        {
            if (template == null || string.IsNullOrEmpty(template.Name) || string.IsNullOrEmpty(template.Url))
            {
                Logger.CreateError(nameof(UnityPackageManager), nameof(ImportWebGLTemplate), "Invalid WebGL template dependency");
                return false;
            }

            string projectPath = GetProjectPath();
            string externalPackagesPath = Path.Combine(projectPath, ExternalPackagesFolder);
            string templatesRootPath = Path.GetFullPath(Path.Combine(projectPath, "Assets", "WebGLTemplates"));
            string targetPath = Path.GetFullPath(Path.Combine(templatesRootPath, template.Name));

            if (!targetPath.StartsWith(templatesRootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                Logger.CreateError(nameof(UnityPackageManager), nameof(ImportWebGLTemplate),
                    "Invalid WebGL template target path", Naming.Quote(targetPath));
                return false;
            }

            Directory.CreateDirectory(externalPackagesPath);
            Directory.CreateDirectory(templatesRootPath);

            string zipPath = Path.Combine(externalPackagesPath, $"{template.Name}-template.zip").Replace('\\', '/');
            string extractPath = Path.Combine(externalPackagesPath, $"{template.Name}-template-extract").Replace('\\', '/');

            try
            {
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }

                Logger.CreateText(nameof(UnityPackageManager), nameof(ImportWebGLTemplate),
                    "Downloading WebGL template", Naming.Quote(template.Url));
                using HttpResponseMessage response = await HttpClient.GetAsync(template.Url);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.CreateError(nameof(UnityPackageManager), nameof(ImportWebGLTemplate),
                        "Download failed", response.StatusCode.ToString(), Naming.Quote(template.Url));
                    return false;
                }

                byte[] data = await response.Content.ReadAsByteArrayAsync();
                File.WriteAllBytes(zipPath, data);

                Directory.CreateDirectory(extractPath);
                ExtractZip(zipPath, extractPath);

                string sourcePath = ResolveTemplateSourcePath(extractPath, template.SourceFolder);
                if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
                {
                    Logger.CreateError(nameof(UnityPackageManager), nameof(ImportWebGLTemplate),
                        "Template source folder not found", Naming.Quote(template.SourceFolder));
                    return false;
                }

                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
                Directory.CreateDirectory(targetPath);

                CopyDirectoryContents(sourcePath, targetPath);
                DeletePackageMetadataFromTemplate(targetPath);
                WriteWebGLTemplateManifest(targetPath, template);
                AssetDatabase.Refresh();

                Logger.CreateText(nameof(UnityPackageManager), nameof(ImportWebGLTemplate),
                    "Installed WebGL template", Naming.Quote(targetPath));
                return true;
            }
            catch (Exception exception)
            {
                Logger.CreateError(nameof(UnityPackageManager), nameof(ImportWebGLTemplate), exception.Message);
                return false;
            }
        }

        public static async Task<bool> ImportFromGit(string gitUrl, string packageName = null)
        {
            if (!string.IsNullOrEmpty(packageName) && IsPackageInstalled(packageName))
            {
                Logger.CreateText(nameof(UnityPackageManager), nameof(ImportFromGit),
                    "Package already installed, skipping", Naming.Quote(packageName));
                return true;
            }

            Logger.CreateText(nameof(UnityPackageManager), nameof(ImportFromGit),
                "Adding git package", Naming.Quote(gitUrl));

            try
            {
                return await AddPackage(gitUrl, packageName);
            }
            catch (Exception exception)
            {
                Logger.CreateError(nameof(UnityPackageManager), nameof(ImportFromGit), exception.Message);
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static async Task<bool> AddPackage(string packageIdentifier, string packageName = null)
        {
            await PackageManagerSemaphore.WaitAsync();
            try
            {
                for (int attempt = 1; attempt <= ExclusiveAccessRetryCount; attempt++)
                {
                    UnityEditor.PackageManager.Requests.AddRequest addRequest =
                        UnityEditor.PackageManager.Client.Add(packageIdentifier);

                    DateTime startTime = DateTime.UtcNow;
                    DateTime timeoutTime = startTime.AddSeconds(UpmTimeoutSeconds);

                    while (!addRequest.IsCompleted && DateTime.UtcNow < timeoutTime)
                    {
                        await Task.Delay(PollIntervalMs);
                    }

                    if (!addRequest.IsCompleted)
                    {
                        Logger.CreateWarning(nameof(UnityPackageManager), nameof(AddPackage),
                            "Package Manager request timed out, UPM will continue in background", Naming.Quote(packageIdentifier));
                        return false;
                    }

                    if (addRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                    {
                        string errorMessage = addRequest.Error?.message;
                        if (IsExclusiveAccessError(errorMessage) && attempt < ExclusiveAccessRetryCount)
                        {
                            Logger.CreateWarning(nameof(UnityPackageManager), nameof(AddPackage),
                                $"Package Manager is busy, retry {attempt}/{ExclusiveAccessRetryCount}", Naming.Quote(packageIdentifier));
                            await Task.Delay(ExclusiveAccessRetryDelayMs);
                            continue;
                        }

                        Logger.CreateError(nameof(UnityPackageManager), nameof(AddPackage),
                            "Failed to add package", Naming.Quote(errorMessage));
                        return false;
                    }

                    Logger.CreateText(nameof(UnityPackageManager), nameof(AddPackage),
                        "Successfully added package", Naming.Quote(packageIdentifier));
                    return true;
                }

                return false;
            }
            finally
            {
                PackageManagerSemaphore.Release();
                ClearProgressAndRepaint();
            }
        }

        public static async Task<bool> RemovePackageIfInstalled(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || !IsPackageInstalled(packageName))
            {
                return true;
            }

            await PackageManagerSemaphore.WaitAsync();
            try
            {
                for (int attempt = 1; attempt <= ExclusiveAccessRetryCount; attempt++)
                {
                    UnityEditor.PackageManager.Requests.RemoveRequest removeRequest =
                        UnityEditor.PackageManager.Client.Remove(packageName);

                    DateTime timeoutTime = DateTime.UtcNow.AddSeconds(UpmTimeoutSeconds);
                    while (!removeRequest.IsCompleted && DateTime.UtcNow < timeoutTime)
                    {
                        await Task.Delay(PollIntervalMs);
                    }

                    if (!removeRequest.IsCompleted)
                    {
                        Logger.CreateWarning(nameof(UnityPackageManager), nameof(RemovePackageIfInstalled),
                            "Package Manager request timed out, UPM will continue in background", Naming.Quote(packageName));
                        return false;
                    }

                    if (removeRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                    {
                        string errorMessage = removeRequest.Error?.message;
                        if (IsExclusiveAccessError(errorMessage) && attempt < ExclusiveAccessRetryCount)
                        {
                            Logger.CreateWarning(nameof(UnityPackageManager), nameof(RemovePackageIfInstalled),
                                $"Package Manager is busy, retry {attempt}/{ExclusiveAccessRetryCount}", Naming.Quote(packageName));
                            await Task.Delay(ExclusiveAccessRetryDelayMs);
                            continue;
                        }

                        Logger.CreateError(nameof(UnityPackageManager), nameof(RemovePackageIfInstalled),
                            "Failed to remove package", Naming.Quote(errorMessage));
                        return false;
                    }

                    Logger.CreateText(nameof(UnityPackageManager), nameof(RemovePackageIfInstalled),
                        "Removed package", Naming.Quote(packageName));
                    return true;
                }

                return false;
            }
            finally
            {
                PackageManagerSemaphore.Release();
                ClearProgressAndRepaint();
            }
        }

        private static bool IsExclusiveAccessError(string errorMessage)
        {
            return !string.IsNullOrEmpty(errorMessage)
                   && errorMessage.IndexOf("exclusive access", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool EnsureScopedRegistry(PackageRegistryDependency package)
        {
            if (string.IsNullOrEmpty(package.RegistryUrl) || package.Scopes == null || package.Scopes.Length == 0)
            {
                return true;
            }

            string manifestPath = Path.Combine(GetProjectPath(), "Packages", "manifest.json").Replace('\\', '/');
            if (!File.Exists(manifestPath))
            {
                Logger.CreateError(nameof(UnityPackageManager), nameof(EnsureScopedRegistry),
                    "manifest.json not found", Naming.Quote(manifestPath));
                return false;
            }

            string manifest = File.ReadAllText(manifestPath);
            if (manifest.Contains($"\"url\": \"{package.RegistryUrl}\"") || manifest.Contains($"\"url\":\"{package.RegistryUrl}\""))
            {
                return true;
            }

            string registryName = string.IsNullOrEmpty(package.RegistryName) ? "OpenUPM" : package.RegistryName;
            string registryJson = CreateScopedRegistryJson(registryName, package.RegistryUrl, package.Scopes);

            string updatedManifest;
            int scopedRegistriesIndex = manifest.IndexOf("\"scopedRegistries\"", StringComparison.Ordinal);
            if (scopedRegistriesIndex >= 0)
            {
                int arrayStart = manifest.IndexOf('[', scopedRegistriesIndex);
                int arrayEnd = FindMatchingBracket(manifest, arrayStart);
                if (arrayStart < 0 || arrayEnd < 0)
                {
                    Logger.CreateError(nameof(UnityPackageManager), nameof(EnsureScopedRegistry), "Invalid scopedRegistries block");
                    return false;
                }

                string existingRegistries = manifest.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                string separator = string.IsNullOrWhiteSpace(existingRegistries) ? string.Empty : ",";
                updatedManifest = manifest.Insert(arrayEnd, $"{separator}\n    {registryJson}\n  ");
            }
            else
            {
                int dependenciesIndex = manifest.IndexOf("\"dependencies\"", StringComparison.Ordinal);
                if (dependenciesIndex < 0)
                {
                    Logger.CreateError(nameof(UnityPackageManager), nameof(EnsureScopedRegistry), "dependencies block not found");
                    return false;
                }

                updatedManifest = manifest.Insert(dependenciesIndex, $"\"scopedRegistries\": [\n    {registryJson}\n  ],\n  ");
            }

            File.WriteAllText(manifestPath, updatedManifest);
            AssetDatabase.Refresh();
            Logger.CreateText(nameof(UnityPackageManager), nameof(EnsureScopedRegistry),
                "Added scoped registry", Naming.Quote(registryName), Naming.Quote(package.RegistryUrl));
            return true;
        }

        private static string ResolveTemplateSourcePath(string extractPath, string sourceFolder)
        {
            string rootPath = extractPath;
            string[] directories = Directory.GetDirectories(extractPath);
            if (directories.Length == 1)
            {
                rootPath = directories[0];
            }

            if (string.IsNullOrEmpty(sourceFolder))
            {
                return rootPath;
            }

            return Path.GetFullPath(Path.Combine(rootPath, sourceFolder));
        }

        private static void ExtractZip(string zipPath, string extractPath)
        {
            using FileStream zipStream = File.OpenRead(zipPath);
            using ZipArchive archive = new(zipStream, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                string entryPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                string fullExtractPath = Path.GetFullPath(extractPath);
                if (!entryPath.StartsWith(fullExtractPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Invalid zip entry path: {entry.FullName}");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(entryPath));
                using Stream entryStream = entry.Open();
                using FileStream outputStream = File.Create(entryPath);
                entryStream.CopyTo(outputStream);
            }
        }

        private static void CopyDirectoryContents(string sourcePath, string targetPath)
        {
            foreach (string directoryPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                string relativePath = directoryPath.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(targetPath, relativePath));
            }

            foreach (string filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                string relativePath = filePath.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string targetFilePath = Path.Combine(targetPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));
                File.Copy(filePath, targetFilePath, true);
            }
        }

        private static void DeletePackageMetadataFromTemplate(string targetPath)
        {
            string[] metadataFiles = {
                "package.json",
                "package.json.meta",
                "dependencies.json",
                "dependencies.json.meta",
                "README.md",
                "README.md.meta",
                "package.png",
                "package.png.meta"
            };

            foreach (string metadataFile in metadataFiles)
            {
                string filePath = Path.Combine(targetPath, metadataFile);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        public static bool IsWebGLTemplateInstalled(PackageWebGLTemplateDependency template)
        {
            if (template == null || string.IsNullOrEmpty(template.Name))
            {
                return false;
            }

            string projectPath = GetProjectPath();
            string targetPath = Path.Combine(projectPath, "Assets", "WebGLTemplates", template.Name).Replace('\\', '/');
            return Directory.Exists(targetPath) && File.Exists(Path.Combine(targetPath, "index.html"));
        }

        public static string GetWebGLTemplateVersion(PackageWebGLTemplateDependency template)
        {
            if (template == null || string.IsNullOrEmpty(template.Name))
            {
                return null;
            }

            string projectPath = GetProjectPath();
            string manifestPath = Path.Combine(projectPath, "Assets", "WebGLTemplates", template.Name, ".primesdk-template").Replace('\\', '/');
            if (!File.Exists(manifestPath))
            {
                return null;
            }

            string[] lines = File.ReadAllLines(manifestPath);
            foreach (string line in lines)
            {
                const string prefix = "version=";
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return line[prefix.Length..];
                }
            }
            return null;
        }

        public static bool RemoveWebGLTemplate(PackageWebGLTemplateDependency template)
        {
            if (template == null || string.IsNullOrEmpty(template.Name))
            {
                Logger.CreateError(nameof(UnityPackageManager), nameof(RemoveWebGLTemplate), "Invalid WebGL template dependency");
                return false;
            }

            string projectPath = GetProjectPath();
            string templatesRootPath = Path.GetFullPath(Path.Combine(projectPath, "Assets", "WebGLTemplates"));
            string targetPath = Path.GetFullPath(Path.Combine(templatesRootPath, template.Name));

            if (!targetPath.StartsWith(templatesRootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                Logger.CreateError(nameof(UnityPackageManager), nameof(RemoveWebGLTemplate),
                    "Invalid WebGL template target path", Naming.Quote(targetPath));
                return false;
            }

            if (!Directory.Exists(targetPath))
            {
                return true;
            }

            try
            {
                Directory.Delete(targetPath, true);
                string metaPath = $"{targetPath}.meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }

                AssetDatabase.Refresh();
                Logger.CreateText(nameof(UnityPackageManager), nameof(RemoveWebGLTemplate),
                    "Removed WebGL template", Naming.Quote(targetPath));
                return true;
            }
            catch (Exception exception)
            {
                Logger.CreateError(nameof(UnityPackageManager), nameof(RemoveWebGLTemplate), exception.Message);
                return false;
            }
        }

        private static void WriteWebGLTemplateManifest(string targetPath, PackageWebGLTemplateDependency template)
        {
            string manifestPath = Path.Combine(targetPath, ".primesdk-template").Replace('\\', '/');
            string version = string.IsNullOrEmpty(template.Version) ? string.Empty : template.Version;
            string content = $"name={template.Name}\nversion={version}\nurl={template.Url}\n";
            File.WriteAllText(manifestPath, content);
        }

        private static string CreateScopedRegistryJson(string name, string url, string[] scopes)
        {
            StringBuilder builder = new();
            builder.Append("{\n");
            builder.Append($"      \"name\": \"{name}\",\n");
            builder.Append($"      \"url\": \"{url}\",\n");
            builder.Append("      \"scopes\": [\n");
            for (int i = 0; i < scopes.Length; i++)
            {
                string comma = i < scopes.Length - 1 ? "," : string.Empty;
                builder.Append($"        \"{scopes[i]}\"{comma}\n");
            }
            builder.Append("      ]\n");
            builder.Append("    }");
            return builder.ToString();
        }

        private static int FindMatchingBracket(string text, int openBracketIndex)
        {
            if (openBracketIndex < 0)
            {
                return -1;
            }

            int depth = 0;
            for (int i = openBracketIndex; i < text.Length; i++)
            {
                if (text[i] == '[')
                {
                    depth++;
                }
                else if (text[i] == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private static void ClearProgressAndRepaint()
        {
            EditorUtility.ClearProgressBar();
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        public static void LockReload()
        {
            EditorApplication.LockReloadAssemblies();
            AssetDatabase.DisallowAutoRefresh();
            Logger.CreateText(nameof(UnityPackageManager), nameof(LockReload), "Assembly reload locked");
        }

        public static void UnlockReload()
        {
            AssetDatabase.AllowAutoRefresh();
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.Refresh();
            Logger.CreateText(nameof(UnityPackageManager), nameof(UnlockReload), "Assembly reload unlocked, refreshing");
        }

        public static bool IsPackageInstalled(string packageName)
        {
#if UNITY_2022_2_OR_NEWER
            UnityEditor.PackageManager.PackageInfo[] packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (UnityEditor.PackageManager.PackageInfo package in packages)
            {
                if (package.name == packageName)
                {
                    return true;
                }
            }
            return false;
#else
            return UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName) != null;
#endif
        }

        private static string GetProjectPath()
        {
            string dataPath = Application.dataPath;
            int assetsIndex = dataPath.LastIndexOf("/Assets");
            if (assetsIndex >= 0)
            {
                return dataPath[..assetsIndex];
            }
            return dataPath;
        }

        private static string GetFileNameFromUrl(string url, string expectedExtension = ".tgz")
        {
            try
            {
                Uri uri = new(url);
                string fileName = Path.GetFileName(uri.LocalPath);
                if (!string.IsNullOrEmpty(fileName) && fileName.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return fileName;
                }
            }
            catch
            {
                // Fall through to generate a default name
            }
            return $"package-{DateTime.UtcNow:yyyyMMddHHmmss}{expectedExtension}";
        }
    }
}
