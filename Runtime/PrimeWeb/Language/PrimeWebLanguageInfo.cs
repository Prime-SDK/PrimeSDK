using PrimeGames.SDK.Common;
using System.Runtime.InteropServices;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(ILanguageInfo))]
    public class PrimeWebLanguageInfo : CommonLanguageInfo {

        [DllImport(Naming.InternalDll)] private static extern int primeSDK_language_current();

        public PrimeWebLanguageInfo() {
            SetInitialized();
        }

        protected override LanguageType GetCurrentImpl() {
            return (LanguageType)primeSDK_language_current();
        }

    }

}