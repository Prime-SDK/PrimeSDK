using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class PlayerAccount_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<PlayerAccount_Features> { }

        public PlayerAccount_Features() {
            SetInfo("Player Account", nameof(IPlayer), nameof(PlayerAccountProvider));

            CreateString(nameof(IPlayerAccount.DisplayName), () => {
                return PrimeSDK.Player.DisplayName;
            });
            CreateString(nameof(IPlayerAccount.FirstName), () => {
                return PrimeSDK.Player.FirstName;
            });
            CreateString(nameof(IPlayerAccount.LastName), () => {
                return PrimeSDK.Player.LastName;
            });
            CreateString(nameof(IPlayerAccount.Username), () => {
                return PrimeSDK.Player.Username;
            });
            CreateString(nameof(IPlayerAccount.UniqueId), () => {
                return PrimeSDK.Player.UniqueId;
            });
            CreateBoolean(nameof(IPlayerAccount.IsLoggedIn), () => {
                return PrimeSDK.Player.IsLoggedIn;
            });
            CreateButton(nameof(IPlayerAccount.InvokeLogin), () => {
                PrimeSDK.Player.InvokeLogin(onLoginSuccess: () => {
                    Debug.Log("Login successful.");
                },
                onLoginError: () => {
                    Debug.LogError("Login failed.");
                });
            });
        }

    }

}