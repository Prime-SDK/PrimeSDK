using System;

namespace PrimeGames.SDK.Common {

    public static class EnumExtensions {

        public static string ToReadableString(this Enum enumValue) {
            return enumValue.ToString().InsertSpacing();
        }

    }

}