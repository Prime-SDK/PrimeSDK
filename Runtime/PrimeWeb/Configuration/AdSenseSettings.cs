using System;
using UnityEngine;

namespace PrimeGames.SDK.PrimeWeb {

    [Serializable]
    public class AdSenseSettings {
        [SerializeField] public string dataAdClient = "";
        [SerializeField] public string dataAdChannel = "";
        [SerializeField] public bool dataAdBreakTest = false;
        [SerializeField] public float interstitialInterval = 0;
    }

}