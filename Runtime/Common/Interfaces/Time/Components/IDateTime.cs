using System;

namespace PrimeGames.SDK.Common {

    [Module]
    public partial interface IDateTime {

        DateTime CurrentDate { get; }
        HolidayType CurrentHoliday { get; }

    }

}