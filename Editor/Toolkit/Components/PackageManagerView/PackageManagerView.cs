using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.Editor
{
    internal partial class PackageManagerView : VisualElement
    {
        public class PackageCardInfo
        {
            public PackageSource Source;
            public PackageInfo Info;
            public PackageDependencies Dependencies;
            public bool DependenciesLoaded;
            public Texture2D Icon;
            public string Readme;
        }

        public class PackageSource
        {
            public string RepositoryHandle;
            public string PackagePath;

            public PackageSource(string repositoryHandle, string packagePath = null)
            {
                RepositoryHandle = repositoryHandle;
                PackagePath = packagePath;
            }
        }

        private readonly PackageManagerInspector PackageManagerInspector;
        private readonly Dictionary<HorizontalCard, PackageCardInfo> PackageCards = new();

        public PackageManagerView(PackageManagerInspector packageManagerInspector)
        {
            PackageManagerInspector = packageManagerInspector;
            VisualTreeAsset asset = VisualTreeReference.LoadVisualTree(nameof(PackageManagerView));
            asset.CloneTree(this);
            style.flexGrow = 1;
            _ = InitializeView();
        }

        private async Task InitializeView()
        {
            await CreatePackageCard(new PackageSource("Prime-SDK/PrimeSDK"));
            await CreatePackageCard(new PackageSource("Prime-SDK/SDK-Playgama-API"));
            await CreatePackageCard(new PackageSource("Prime-SDK/RuStore-API"));
            await CreatePackageCard(new PackageSource("Prime-SDK/SDK-XSolla-API"));
            await CreatePackageCard(new PackageSource("Prime-SDK/SDK-YandexMobileAds-API"));
            await CreatePackageCard(new PackageSource("Prime-SDK/PrimeGamesTemplate"));
        }

        private async Task CreatePackageCard(PackageSource source)
        {
            PackageInfo packageInfo = await GetPackageInfo(source);
            if (packageInfo == null)
            {
                Logger.CreateWarning(this, nameof(CreatePackageCard), "Unable to access repository", Naming.Quote(source.RepositoryHandle));
                return;
            }

            bool isPackageInstalled = IsPackageInstalled(packageInfo.name);
            string localPackageVersion = GetLocalPackageVersion(packageInfo.name);
            HorizontalCard card = new()
            {
                HeaderText = $"{packageInfo.displayName}",
                DescriptionText = packageInfo.name,
                LetterText = packageInfo.displayName[..1].ToUpper(),
                HintText = isPackageInstalled ? $"Available: {packageInfo.version}\nInstalled: {localPackageVersion}" : $"Available: {packageInfo.version}\nNot installed"
            };
            PackageCardInfo cardInfo = new()
            {
                Source = source,
                Info = packageInfo
            };
            PackageCards.Add(card, cardInfo);
            contentContainer.Add(card);

            Task<Texture2D> packageIcon = GetPackagePng(source);
            _ = packageIcon.ContinueWith(task =>
            {
                if (task.Result != null)
                {
                    cardInfo.Icon = task.Result;
                    card.SetIcon(task.Result);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());

            Task<string> packageReadme = GetPackageReadme(source);
            _ = packageReadme.ContinueWith(task =>
            {
                if (task.Result != null)
                {
                    cardInfo.Readme = task.Result;
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());

            Task<PackageDependencies> packageDependencies = GetPackageDependencies(source);
            _ = packageDependencies.ContinueWith(task =>
            {
                cardInfo.DependenciesLoaded = true;
                if (task.Result != null)
                {
                    cardInfo.Dependencies = task.Result;
                    RefreshCardInstallationState(card, cardInfo);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());

            card.RegisterCallback<ClickEvent>(callback =>
            {
                SelectCard(card);
            });
        }

        private void SelectCard(HorizontalCard card)
        {
            DeselectCards();
            card.Select();

            string description = PackageCards[card].Info.description;
            if (string.IsNullOrEmpty(description))
            {
                description = Naming.Dash;
            }
            PackageManagerInspector.DescriptionLabel.text = description;

            string readme = PackageCards[card].Readme;
            if (string.IsNullOrEmpty(readme))
            {
                readme = Naming.Dash;
            }
            PackageManagerInspector.ReadmeLabel.text = readme;

            PackageCardInfo cardInfo = PackageCards[card];

            Button installButton = new()
            {
                text = $"Install {cardInfo.Info.displayName}"
            };
            installButton.clicked += async () =>
            {
                await InstallPackageWithDependencies(card, cardInfo, installButton);
            };

            Button updateButton = new()
            {
                text = $"Update {cardInfo.Info.displayName} to {cardInfo.Info.version}"
            };
            updateButton.clicked += async () =>
            {
                await InstallPackageWithDependencies(card, cardInfo, updateButton);
            };

            Button removeButton = new()
            {
                text = $"Remove {cardInfo.Info.displayName}"
            };
            removeButton.clicked += async () =>
            {
                await RemovePackage(card, cardInfo);
            };

            PackageManagerInspector.ActionButtonsElement.Clear();
            if (IsPackageInstalled(cardInfo))
            {
                if (GetLocalPackageVersion(cardInfo) != cardInfo.Info.version)
                {
                    PackageManagerInspector.ActionButtonsElement.Add(updateButton);
                }
                PackageManagerInspector.ActionButtonsElement.Add(removeButton);
            }
            else
            {
                PackageManagerInspector.ActionButtonsElement.Add(installButton);
            }
        }

        private void DeselectCards()
        {
            foreach (HorizontalCard card in PackageCards.Keys)
            {
                card.Deselect();
            }
        }

        private async Task RemovePackage(HorizontalCard card, PackageCardInfo cardInfo)
        {
            PackageWebGLTemplateDependency template = GetPrimaryWebGLTemplate(cardInfo);
            if (template != null)
            {
                if (UnityPackageManager.RemoveWebGLTemplate(template))
                {
                    await UnityPackageManager.RemovePackageIfInstalled(cardInfo.Info.name);
                    RefreshCardInstallationState(card, cardInfo);
                    SelectCard(card);
                }
                return;
            }

            UnityEditor.PackageManager.Client.Remove(cardInfo.Info.name);
        }

        private async Task InstallPackageWithDependencies(HorizontalCard card, PackageCardInfo cardInfo, params Button[] buttons)
        {
            try
            {
                foreach (Button button in buttons)
                {
                    button.SetEnabled(false);
                }

                PackageDependencies dependencies = cardInfo.Dependencies;
                if (!cardInfo.DependenciesLoaded)
                {
                    dependencies = await GetPackageDependencies(cardInfo.Source);
                    cardInfo.Dependencies = dependencies;
                    cardInfo.DependenciesLoaded = true;
                }

                if (dependencies != null)
                {
                    if (dependencies.TarballUrls != null)
                    {
                        foreach (string tarballUrl in dependencies.TarballUrls)
                        {
                            Logger.CreateText(this, nameof(InstallPackageWithDependencies), "Installing tarball dependency", Naming.Quote(tarballUrl));
                            if (!await UnityPackageManager.ImportFromTarball(tarballUrl))
                            {
                                Logger.CreateError(this, nameof(InstallPackageWithDependencies), "Tarball dependency installation failed", Naming.Quote(tarballUrl));
                                return;
                            }
                        }
                    }
                    if (dependencies.RegistryPackages != null)
                    {
                        foreach (PackageRegistryDependency registryPackage in dependencies.RegistryPackages)
                        {
                            Logger.CreateText(this, nameof(InstallPackageWithDependencies), "Installing registry dependency", Naming.Quote(registryPackage.Name));
                            if (!await UnityPackageManager.ImportFromRegistry(registryPackage))
                            {
                                Logger.CreateError(this, nameof(InstallPackageWithDependencies), "Registry dependency installation failed", Naming.Quote(registryPackage.Name));
                                return;
                            }
                        }
                    }
                    if (dependencies.GitUrls != null)
                    {
                        foreach (string gitUrl in dependencies.GitUrls)
                        {
                            Logger.CreateText(this, nameof(InstallPackageWithDependencies), "Installing git dependency", Naming.Quote(gitUrl));
                            if (!await UnityPackageManager.ImportFromGit(gitUrl))
                            {
                                Logger.CreateError(this, nameof(InstallPackageWithDependencies), "Git dependency installation failed", Naming.Quote(gitUrl));
                                return;
                            }
                        }
                    }
                    if (dependencies.GitPackages != null)
                    {
                        foreach (PackageGitDependency gitPackage in dependencies.GitPackages)
                        {
                            Logger.CreateText(this, nameof(InstallPackageWithDependencies), "Installing git dependency", Naming.Quote(gitPackage.Url));
                            if (!await UnityPackageManager.ImportFromGit(gitPackage.Url, gitPackage.Name))
                            {
                                Logger.CreateError(this, nameof(InstallPackageWithDependencies), "Git dependency installation failed", Naming.Quote(gitPackage.Url));
                                return;
                            }
                        }
                    }
                    if (dependencies.UnityPackages != null)
                    {
                        foreach (string unityPackageUrl in dependencies.UnityPackages)
                        {
                            Logger.CreateText(this, nameof(InstallPackageWithDependencies), "Importing unity package dependency", Naming.Quote(unityPackageUrl));
                            await UnityPackageManager.ImportFromUnityPackage(unityPackageUrl, false);
                        }
                    }
                    if (dependencies.WebGLTemplates != null)
                    {
                        foreach (PackageWebGLTemplateDependency template in dependencies.WebGLTemplates)
                        {
                            template.Version = cardInfo.Info.version;
                            Logger.CreateText(this, nameof(InstallPackageWithDependencies), "Installing WebGL template", Naming.Quote(template.Name));
                            if (!await UnityPackageManager.ImportWebGLTemplate(template))
                            {
                                Logger.CreateError(this, nameof(InstallPackageWithDependencies), "WebGL template installation failed", Naming.Quote(template.Name));
                                return;
                            }
                        }
                        await UnityPackageManager.RemovePackageIfInstalled(cardInfo.Info.name);
                    }
                }

                if (dependencies == null || !dependencies.SkipPackageInstall)
                {
                    string packageGitUrl = GetPackageGitUrl(cardInfo.Source);
                    Logger.CreateText(this, nameof(InstallPackageWithDependencies), "Installing API package", Naming.Quote(cardInfo.Info.displayName));
                    if (!await UnityPackageManager.ImportFromGit(packageGitUrl, cardInfo.Info.name))
                    {
                        Logger.CreateError(this, nameof(InstallPackageWithDependencies), "API package installation failed", Naming.Quote(cardInfo.Info.displayName));
                        return;
                    }
                }

                Logger.CreateText(this, nameof(InstallPackageWithDependencies), "Automatic installation completed for", Naming.Quote(cardInfo.Info.displayName));
                RefreshCardInstallationState(card, cardInfo);
            }
            catch (Exception exception)
            {
                Logger.CreateError(this, nameof(InstallPackageWithDependencies), "Automatic installation failed", exception.Message);
            }
            finally
            {
                foreach (Button button in buttons)
                {
                    button.SetEnabled(true);
                }
            }
        }

        private bool IsPackageInstalled(PackageCardInfo cardInfo)
        {
            PackageWebGLTemplateDependency template = GetPrimaryWebGLTemplate(cardInfo);
            if (template != null)
            {
                return UnityPackageManager.IsWebGLTemplateInstalled(template);
            }

            return IsPackageInstalled(cardInfo.Info.name);
        }

        private string GetLocalPackageVersion(PackageCardInfo cardInfo)
        {
            PackageWebGLTemplateDependency template = GetPrimaryWebGLTemplate(cardInfo);
            if (template != null)
            {
                return UnityPackageManager.GetWebGLTemplateVersion(template);
            }

            return GetLocalPackageVersion(cardInfo.Info.name);
        }

        private PackageWebGLTemplateDependency GetPrimaryWebGLTemplate(PackageCardInfo cardInfo)
        {
            if (cardInfo?.Dependencies?.WebGLTemplates == null || cardInfo.Dependencies.WebGLTemplates.Length == 0)
            {
                return null;
            }

            return cardInfo.Dependencies.WebGLTemplates[0];
        }

        private void RefreshCardInstallationState(HorizontalCard card, PackageCardInfo cardInfo)
        {
            bool isPackageInstalled = IsPackageInstalled(cardInfo);
            string localPackageVersion = GetLocalPackageVersion(cardInfo);
            if (string.IsNullOrEmpty(localPackageVersion))
            {
                localPackageVersion = Naming.Dash;
            }
            card.HintText = isPackageInstalled ? $"Available: {cardInfo.Info.version}\nInstalled: {localPackageVersion}" : $"Available: {cardInfo.Info.version}\nNot installed";
        }

        private bool IsPackageInstalled(string packageName)
        {
            return GetInstalledPackageInfo(packageName) != null;
        }

        private string GetLocalPackageVersion(string packageName)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo = GetInstalledPackageInfo(packageName);
            return packageInfo?.version;
        }

        private UnityEditor.PackageManager.PackageInfo GetInstalledPackageInfo(string packageName)
        {
#if UNITY_2022_2_OR_NEWER
            UnityEditor.PackageManager.PackageInfo[] packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (UnityEditor.PackageManager.PackageInfo package in packages)
            {
                if (package.name == packageName)
                {
                    return package;
                }
            }
            return null;
#else
            return UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
#endif
        }

        private string GetUnityPackageDisplayName(string url)
        {
            try
            {
                Uri uri = new(url);
                return Path.GetFileNameWithoutExtension(uri.LocalPath);
            }
            catch
            {
                return "UnityPackage";
            }
        }

        private string GetPackageFileUrl(PackageSource source, string fileName)
        {
            string packagePath = string.IsNullOrEmpty(source.PackagePath) ? string.Empty : $"{source.PackagePath.Trim('/')}/";
            return $"https://raw.githubusercontent.com/{source.RepositoryHandle}/refs/heads/main/{packagePath}{fileName}";
        }

        private string GetPackageJsonUrl(PackageSource source)
        {
            return GetPackageFileUrl(source, "package.json");
        }

        private string GetPackagePngUrl(PackageSource source)
        {
            return GetPackageFileUrl(source, "package.png");
        }

        private string GetPackageReadmeUrl(PackageSource source)
        {
            return GetPackageFileUrl(source, "README.md");
        }

        private string GetPackageGitUrl(PackageSource source)
        {
            string packagePath = string.IsNullOrEmpty(source.PackagePath) ? string.Empty : $"?path=/{source.PackagePath.Trim('/')}";
            return $"https://github.com/{source.RepositoryHandle}.git{packagePath}#main";
        }

        private string GetDependenciesJsonUrl(PackageSource source)
        {
            return GetPackageFileUrl(source, "dependencies.json");
        }

        private async Task<PackageDependencies> GetPackageDependencies(PackageSource source)
        {
            string dependenciesJsonUrl = GetDependenciesJsonUrl(source);
            byte[] data = await Get(dependenciesJsonUrl);
            if (data == null)
            {
                return null;
            }
            try
            {
                string jsonResponse = Encoding.UTF8.GetString(data);
                PackageDependencies dependencies = JsonUtility.FromJson<PackageDependencies>(jsonResponse);
                return dependencies;
            }
            catch (Exception exception)
            {
                Logger.CreateError(this, nameof(GetPackageDependencies), exception, Naming.Quote(Encoding.UTF8.GetString(data)));
                return null;
            }
        }

        private async Task<string> GetPackageReadme(PackageSource source)
        {
            string readmeUrl = GetPackageReadmeUrl(source);
            byte[] data = await Get(readmeUrl);
            if (data == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(data);
        }

        private async Task<Texture2D> GetPackagePng(PackageSource source)
        {
            string packagePngUrl = GetPackagePngUrl(source);
            byte[] data = await Get(packagePngUrl);
            if (data == null)
            {
                return null;
            }
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(data, true);
            return texture;
        }

        private async Task<PackageInfo> GetPackageInfo(PackageSource source)
        {
            string packageJsonUrl = GetPackageJsonUrl(source);
            byte[] data = await Get(packageJsonUrl);
            if(data == null)
            {
                return null;
            }
            try
            {
                string jsonResponse = Encoding.UTF8.GetString(data);
                PackageInfo packageInfo = JsonUtility.FromJson<PackageInfo>(jsonResponse);
                return packageInfo;
            }
            catch (Exception exception)
            {
                Logger.CreateError(this, nameof(GetPackageInfo), exception, Naming.Quote(Encoding.UTF8.GetString(data)));
                return null;
            }
        }

        private async Task<byte[]> Get(string url)
        {
            Logger.CreateText(this, nameof(Get), Naming.Quote(url));
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            UnityWebRequestAsyncOperation asyncOperation = webRequest.SendWebRequest();
            DateTime timeoutDateTime = DateTime.UtcNow.AddSeconds(10);
            while (!asyncOperation.isDone && DateTime.UtcNow < timeoutDateTime)
            {
                await Task.Delay(100);
            }
            if (!asyncOperation.isDone)
            {
                Logger.CreateWarning(this, nameof(Get), "Request timed out", Naming.Quote(url));
                webRequest.Abort();
                return null;
            }
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Logger.CreateWarning(this, nameof(Get), "Request failed", Naming.Quote(webRequest.error), Naming.Quote(url));
                return null;
            }
            return webRequest.downloadHandler.data;
        }

        private new VisualElement contentContainer
        {
            get => this.Q<VisualElement>(nameof(contentContainer));
        }
    }
}
