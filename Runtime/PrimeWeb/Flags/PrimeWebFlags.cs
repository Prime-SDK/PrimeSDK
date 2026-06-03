using PrimeGames.SDK.Common;
using System;
using System.Runtime.InteropServices;

namespace PrimeGames.SDK.PrimeWeb {

    [Provider(typeof(IFlags))]
    public class PrimeWebFlags : IFlags {

        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_flags_getBool(string key, bool defaultValue);
        [DllImport(Naming.InternalDll)] private static extern int primeSDK_flags_getInt(string key, int defaultValue);
        [DllImport(Naming.InternalDll)] private static extern float primeSDK_flags_getFloat(string key, float defaultValue);
        [DllImport(Naming.InternalDll)] private static extern string primeSDK_flags_getString(string key, string defaultValue);
        [DllImport(Naming.InternalDll)] private static extern bool primeSDK_flags_hasKey(string key);

        public bool IsFlagsInitialized { get; } = true;
        public bool IsFlagsAvailable { get; } = true;

        public void WaitForFlags(Action onInitialized) {
            onInitialized?.Invoke();
        }

        public bool GetBool(string key, bool defaultValue = false) {
            try {
                return primeSDK_flags_getBool(key, defaultValue);
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(GetBool), exception);
                return defaultValue;
            }
        }

        public int GetInt(string key, int defaultValue = 0) {
            try {
                return primeSDK_flags_getInt(key, defaultValue);
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(GetInt), exception);
                return defaultValue;
            }
        }

        public float GetFloat(string key, float defaultValue = 0.0f) {
            try {
                return primeSDK_flags_getFloat(key, defaultValue);
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(GetFloat), exception);
                return defaultValue;
            }
        }

        public string GetString(string key, string defaultValue = "") {
            try {
                return primeSDK_flags_getString(key, defaultValue);
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(GetString), exception);
                return defaultValue;
            }
        }

        public bool HasKey(string key) {
            try {
                return primeSDK_flags_hasKey(key);
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(HasKey), exception);
                return false;
            }
        }

    }

}