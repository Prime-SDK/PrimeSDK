using PrimeGames.SDK.Common;
using System;
using UnityEngine;

namespace PrimeGames.SDK.PrimeWeb {

    [Serializable]
    public class YandexAdsSettings {
        [SerializeField] public string appId = "";
        [SerializeField] public string blockIdsJson = Naming.EmptyJson;
        [SerializeField] public float interstitialInterval = 0;
    }

}