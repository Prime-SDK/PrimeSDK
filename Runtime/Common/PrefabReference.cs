using UnityEngine;

namespace PrimeGames.SDK.Common {

    public class PrefabReference : ScriptableObject {

        public static PrefabReference Load(string name) {
            return Resources.Load<PrefabReference>($"{Naming.PrimeSDK}/{name}");
        }

        [field: SerializeField] public GameObject Prefab { get; internal set; }

    }

}