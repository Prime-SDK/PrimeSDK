using PrimeGames.SDK.Common;
using System;
using UnityEngine;

namespace PrimeGames.SDK.PrimeWeb
{
    [Serializable]
    public class Framework_PropertyGroup : PropertyGroup
    {
        public override string Name => "Framework";
        [SerializeField] public string forcePlatformType = "";
        public override StringProperty[] GetStringProperties()
        {
            return new StringProperty[] {
                new(
                    "Force Platform Type",
                    () => forcePlatformType,
                    (value) => { forcePlatformType = value; }
                )
            };
        }
    }
}