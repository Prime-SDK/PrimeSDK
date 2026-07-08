using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class Pause_Features : FeaturesContainer {

        private int continueCallbacks;

        public new class UxmlFactory : UxmlFactory<Pause_Features> { }

        public Pause_Features() {
            SetInfo("Pause", nameof(IPause), nameof(PauseProvider));

            CreateString("Continue Callbacks", () => continueCallbacks.ToString());

            CreateButton(nameof(IPause.ShowContinuePrompt), () => {
                PrimeSDK.Pause.ShowContinuePrompt(() => {
                    continueCallbacks++;
                    Debug.Log($"{nameof(IPause.ShowContinuePrompt)} callback");
                });
            });
        }

    }

}
