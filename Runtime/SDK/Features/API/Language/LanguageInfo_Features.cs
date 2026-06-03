using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class LanguageInfo_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<LanguageInfo_Features> { }

        public LanguageInfo_Features() {
            SetInfo("Language Info", nameof(ILanguage), nameof(LanguageInfoProvider));

            CreateString(nameof(ILanguage.Current), () => {
                return PrimeSDK.Language.Current.ToString();
            });
        }

    }

}