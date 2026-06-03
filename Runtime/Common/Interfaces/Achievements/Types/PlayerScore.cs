using System;
using UnityEngine;

namespace PrimeGames.SDK.Common {

    [Serializable]
    public record PlayerScore {

        [SerializeField] public string displayName;
        [SerializeField] public int position;
        [SerializeField] public int score;
        [SerializeField] public string profilePictureUrl;

    }

}