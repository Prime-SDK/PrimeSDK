using AOT;
using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IBootstrap))]
    public class PrimeWebBootstrap : CommonBootstrap {

        [DllImport(Naming.InternalDll)] private static extern void createPrimeSDK(string configurationJson, DelegateVoid onInstance);

        [MonoPInvokeCallback(typeof(DelegateVoid))]
        private static void OnCreateInstance(int senderId) {
            try {
                OnInstanceCallbacks.PopInvokeAll();
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebBootstrap), nameof(OnCreateInstance), exception);
            }
        }

        private static readonly Stack<Action> OnInstanceCallbacks = new();

        public PrimeWebBootstrap(PreferencesReader preferencesReader) {
            if (Application.isEditor) {
                Logger.CreateText(this, "Skip WebGL JavaScript bootstrap in Unity Editor.");
                SetInitialized();
                return;
            }

            OnInstanceCallbacks.Push(SetInitialized);
            PlatformSettings platformSettings = new(preferencesReader);
            string platformSettingsJson = JsonUtility.ToJson(platformSettings);
            if (string.IsNullOrEmpty(platformSettingsJson)) {
                platformSettingsJson = Naming.EmptyJson;
            }
            Logger.CreateText(this, platformSettingsJson);
            createPrimeSDK(platformSettingsJson, OnCreateInstance);
        }

    }

}
