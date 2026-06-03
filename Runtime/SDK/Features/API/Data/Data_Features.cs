using PrimeGames.SDK.Common;
using PrimeGames.SDK.SourceGenerator;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class Data_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<Data_Features> { }

        public Data_Features() {
            SetInfo("Data", nameof(IData), nameof(DataProvider));

            CreateButton(nameof(IDataContainer.GetBool), () => {
                bool value = PrimeSDK.Data.GetBool("customBool");
                Debug.Log($"get customBool {value}");
            });
            CreateButton(nameof(IDataContainer.SetBool), () => {
                bool value = Random.Range(0, 2) == 0;
                PrimeSDK.Data.SetBool("customBool", value);
                Debug.Log($"set customBool {value}");
            });

            CreateButton(nameof(IDataContainer.GetInt), () => {
                int value = PrimeSDK.Data.GetInt("customInt");
                Debug.Log($"get customInt {value}");
            });
            CreateButton(nameof(IDataContainer.SetInt), () => {
                int value = Random.Range(0, int.MaxValue);
                PrimeSDK.Data.SetInt("customInt", value);
                Debug.Log($"set customInt {value}");
            });

            CreateButton(nameof(IDataContainer.GetFloat), () => {
                float value = PrimeSDK.Data.GetFloat("customFloat");
                Debug.Log($"get customFloat '{value}'");
            });
            CreateButton(nameof(IDataContainer.SetFloat), () => {
                float value = Random.Range(0f, float.MaxValue);
                PrimeSDK.Data.SetFloat("customFloat", value);
                Debug.Log($"set customFloat '{value}'");
            });

            CreateButton(nameof(IDataContainer.GetString), () => {
                string value = PrimeSDK.Data.GetString("customString");
                Debug.Log($"get customString '{value}'");
            });
            CreateButton(nameof(IDataContainer.SetString), () => {
                // Encode random integer into base64 string
                int randomInt = Random.Range(0, int.MaxValue);
                string value = global::System.Convert.ToBase64String(global::System.BitConverter.GetBytes(randomInt));
                PrimeSDK.Data.SetString("customString", value);
                Debug.Log($"set customString '{value}'");
            });

            CreateButton(nameof(IDataContainer.GetObject), () => {
                Vector3 value = PrimeSDK.Data.GetObject<Vector3>("customSerializable");
                Debug.Log($"get customSerializable {value}");
            });
            CreateButton(nameof(IDataContainer.SetObject), () => {
                Vector3 value = new(
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f));
                PrimeSDK.Data.SetObject("customSerializable", value);
                Debug.Log($"set customSerializable {value}");
            });

            CreateButton(nameof(IDataContainer.Save), () => {
                PrimeSDK.Data.Save();
            });
            CreateButton(nameof(IDataContainer.HasKey), () => {
                bool value = PrimeSDK.Data.HasKey("customBool");
                Debug.Log($"HasKey customBool {value}");
            });
            CreateButton(nameof(IDataContainer.DeleteKey), () => {
                PrimeSDK.Data.DeleteKey("customKey");
            });
            CreateButton(nameof(IDataContainer.DeleteAll), () => {
                PrimeSDK.Data.DeleteAll();
            });
        }

    }

}