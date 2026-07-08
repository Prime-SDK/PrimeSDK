using System;

namespace PrimeGames.SDK.Common {

    [Module]
    public partial interface IPause {

        void Register(object source, bool value);

        void ShowContinuePrompt(Action onContinue = null);

    }

}
