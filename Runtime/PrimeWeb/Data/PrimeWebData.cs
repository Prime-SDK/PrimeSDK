using PrimeGames.SDK.Common;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Logger = PrimeGames.SDK.Common.Logger;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IData))]
    public class PrimeWebData : IData {

        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_data_getBool(string key, bool defaultValue = false);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_data_setBool(string key, bool writeValue, bool important = true);
        [DllImport(Naming.InternalDll)] private static extern int primeSDK_data_getInt(string key, int defaultValue = 0);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_data_setInt(string key, int writeValue, bool important = true);
        [DllImport(Naming.InternalDll)] private static extern float primeSDK_data_getFloat(string key, float defaultValue = 0.0f);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_data_setFloat(string key, float writeValue, bool important = true);
        [DllImport(Naming.InternalDll)] private static extern string primeSDK_data_getString(string key, string defaultValue = "");
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_data_setString(string key, string writeValue, bool important = true);

        [DllImport(Naming.InternalDll)] private static extern void primeSDK_data_save();
        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_data_hasKey(string key);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_data_deleteKey(string key);
        [DllImport(Naming.InternalDll)] private static extern void primeSDK_data_deleteAll();

        public bool IsDataInitialized { get; } = true;
        public bool IsDataAvailable { get; } = true;

        public void WaitForData(Action onInitialized) {
            onInitialized?.Invoke();
        }

        public bool GetBool(string key, bool defaultValue = false) {
            try {
                return primeSDK_data_getBool(key, defaultValue);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(GetBool), exception);
                return defaultValue;
            }
        }

        public void SetBool(string key, bool writeValue, bool important = true) {
            try {
                primeSDK_data_setBool(key, writeValue, important);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(SetBool), exception);
            }
        }

        public int GetInt(string key, int defaultValue = 0) {
            try {
                return primeSDK_data_getInt(key, defaultValue);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(GetInt), exception);
                return defaultValue;
            }
        }

        public void SetInt(string key, int writeValue, bool important = true) {
            try {
                primeSDK_data_setInt(key, writeValue, important);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(SetInt), exception);
            }
        }

        public float GetFloat(string key, float defaultValue = 0.0f) {
            try {
                return primeSDK_data_getFloat(key, defaultValue);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(GetFloat), exception);
                return defaultValue;
            }
        }

        public void SetFloat(string key, float writeValue, bool important = true) {
            try {
                primeSDK_data_setFloat(key, writeValue, important);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(SetFloat), exception);
            }
        }

        public string GetString(string key, string defaultValue = "") {
            try {
                return primeSDK_data_getString(key, defaultValue);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(GetString), exception);
                return defaultValue;
            }
        }

        public void SetString(string key, string writeValue, bool important = true) {
            try {
                primeSDK_data_setString(key, writeValue, important);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(SetString), exception);
            }
        }

        public TSerializable GetObject<TSerializable>(string key, TSerializable defaultValue = default) {
            try {
                string json = primeSDK_data_getString(key, JsonUtility.ToJson(defaultValue));
                return JsonUtility.FromJson<TSerializable>(json);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(GetObject), exception);
                return defaultValue;
            }
        }

        public void SetObject<TSerializable>(string key, TSerializable writeValue, bool important = true) {
            try {
                string json = JsonUtility.ToJson(writeValue);
                primeSDK_data_setString(key, json, important);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(SetObject), exception);
            }
        }

        public void Save() {
            try {
                primeSDK_data_save();
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(Save), exception);
            }
        }

        public bool HasKey(string key) {
            try {
                return primeSDK_data_hasKey(key);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(HasKey), exception);
                return false;
            }
        }

        public void DeleteKey(string key) {
            try {
                primeSDK_data_deleteKey(key);
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(DeleteKey), exception);
            }
        }

        public void DeleteAll() {
            try {
                primeSDK_data_deleteAll();
            }
            catch (Exception exception) {
                Logger.CreateError(nameof(PrimeWebData), nameof(DeleteAll), exception);
            }
        }

    }

}