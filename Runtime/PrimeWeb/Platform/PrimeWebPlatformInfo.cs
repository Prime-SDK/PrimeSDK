using PrimeGames.SDK.Common;
using System.Runtime.InteropServices;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IPlatformInfo))]
    public class PrimeWebPlatformInfo : CommonPlatformInfo {

        [DllImport(Naming.InternalDll)] private static extern string primeSDK_platform_current();
        [DllImport(Naming.InternalDll)] private static extern string primeSDK_platform_appId();

        protected override PlatformType GetCurrentImpl() {
            string platformName = primeSDK_platform_current();
            return platformName.ToEnumOrDefault<PlatformType>(PlatformType.Unknown);
        }

        protected override DeploymentType GetDeploymentImpl() {
            return DeploymentType.Web;
        }

        protected override string GetAppIdImpl() {
            return primeSDK_platform_appId();
        }

    }

}