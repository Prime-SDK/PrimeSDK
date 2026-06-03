using PrimeGames.SDK.Common;
using System;

namespace PrimeGames.SDK.UnityEngine {

    [Provider(typeof(IData))]
    public class UnityEngineData : CommonData {

        private const string JsonKey = "PrimeGames.SDK.Data";

        public UnityEngineData(IEventDispatcher eventDispatcher) : base(eventDispatcher) {
            ReadJson((json) => {
                ParseContainers(json);
                SetInitialized();
            });
        }

        protected override void ReadJson(Action<string> jsonRequest) {
            string json = global::UnityEngine.PlayerPrefs.GetString(JsonKey, Naming.EmptyJson);
            jsonRequest?.Invoke(json);
        }

        protected override void WriteJson(string json) {
            global::UnityEngine.PlayerPrefs.SetString(JsonKey, json);
        }

    }

}