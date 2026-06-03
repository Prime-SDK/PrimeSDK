using AOT;
using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IPlayerAccount))]
    public class PrimeWebPlayerAccount : CommonPlayerAccount {

        [DllImport(Naming.InternalDll)] private static extern string primeSDK_player_displayName();
        [DllImport(Naming.InternalDll)] private static extern string primeSDK_player_firstName();
        [DllImport(Naming.InternalDll)] private static extern string primeSDK_player_lastName();
        [DllImport(Naming.InternalDll)] private static extern string primeSDK_player_username();
        [DllImport(Naming.InternalDll)] private static extern string primeSDK_player_uniqueId();
        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_player_isLoggedIn();
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_player_invokeLogin(int senderId, DelegateVoid onSuccess, DelegateVoid onError);

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnLoginSuccess(int senderId) {
            try {
                if (invokeLoginInfo.TryGetValue(senderId, out InvokeLoginInfo info)) {
                    info.onLoginSuccess?.Invoke();
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebPlayerAccount), nameof(OnLoginSuccess), exception);
            }
        }

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnLoginError(int senderId) {
            try {
                if (invokeLoginInfo.TryGetValue(senderId, out InvokeLoginInfo info)) {
                    info.onLoginError?.Invoke();
                }
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebPlayerAccount), nameof(OnLoginError), exception);
            }
        }

        private class InvokeLoginInfo {
            public Action onLoginSuccess;
            public Action onLoginError;
        }

        private static readonly Dictionary<int, InvokeLoginInfo> invokeLoginInfo = new();

        public PrimeWebPlayerAccount() {
            SetInitialized();
        }

        protected override string GetDisplayNameImpl() {
            return primeSDK_player_displayName();
        }

        protected override string GetFirstNameImpl() {
            return primeSDK_player_firstName();
        }

        protected override string GetLastNameImpl() {
            return primeSDK_player_lastName();
        }

        protected override string GetUsernameImpl() {
            return primeSDK_player_username();
        }

        protected override string GetUniqueIdImpl() {
            return primeSDK_player_uniqueId();
        }

        protected override bool IsLoggedInImpl() {
            return primeSDK_player_isLoggedIn();
        }

        protected override void InvokeLoginImpl(Action onLoginSuccess = null, Action onLoginError = null) {
            int senderId = invokeLoginInfo.Count;
            invokeLoginInfo[senderId] = new InvokeLoginInfo {
                onLoginSuccess = onLoginSuccess,
                onLoginError = onLoginError
            };
            primeSDK_player_invokeLogin(senderId, OnLoginSuccess, OnLoginError);
        }

    }

}