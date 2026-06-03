using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class DateTime_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<DateTime_Features> { }

        public DateTime_Features() {
            SetInfo("Date Time", nameof(ITime), nameof(DateTimeProvider));

            CreateString(nameof(IDateTime.CurrentDate), () => {
                return PrimeSDK.Time.CurrentDate.ToString("dd/MM/yyyy\nHH:mm:ss");
            });
            CreateString(nameof(IDateTime.CurrentHoliday), () => {
                return PrimeSDK.Time.CurrentHoliday.ToString();
            });
        }

    }

}