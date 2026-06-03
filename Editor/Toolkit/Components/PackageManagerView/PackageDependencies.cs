using System;

namespace PrimeGames.SDK.Editor
{
    [Serializable]
    public class PackageDependencies
    {
        public string[] UnityPackages;
        public PackageGitDependency[] GitPackages;
        public PackageRegistryDependency[] RegistryPackages;
        public PackageWebGLTemplateDependency[] WebGLTemplates;
        public string[] GitUrls;
        public string[] TarballUrls;
        public bool SkipPackageInstall;
    }

    [Serializable]
    public class PackageGitDependency
    {
        public string Name;
        public string Url;
    }

    [Serializable]
    public class PackageRegistryDependency
    {
        public string Name;
        public string Version;
        public string RegistryName;
        public string RegistryUrl;
        public string[] Scopes;
    }

    [Serializable]
    public class PackageWebGLTemplateDependency
    {
        public string Name;
        public string Url;
        public string SourceFolder;
        public string Version;
    }
}
