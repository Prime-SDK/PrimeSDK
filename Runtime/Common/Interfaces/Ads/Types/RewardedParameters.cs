using System;

namespace PrimeGames.SDK.Common
{
    public class RewardedParameters
    {
        public Action OnOpen = null;
        public Action<bool> OnClose = null;
        public Action OnAdBlockDetected = null;
        public string PlacementId = string.Empty;
    }
}
