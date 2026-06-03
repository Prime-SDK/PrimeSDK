using PrimeGames.SDK.Common;
using UnityEngine;

namespace PrimeGames.SDK.UnityEngine {

    [Provider(typeof(IDeviceBrowser))]
    public class UnityEngineDeviceBrowser : CommonDeviceBrowser {

        protected override void OpenUrlImpl(string url) {
            Application.OpenURL(url);
        }

    }

}