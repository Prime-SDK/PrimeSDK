using System;

namespace PrimeGames.SDK.Common {

    public class FloatProperty : PropertyInfo<float> {

        public FloatProperty(string name, Func<float> getter, Action<float> setter) : base(name, getter, setter) { }

    }

}