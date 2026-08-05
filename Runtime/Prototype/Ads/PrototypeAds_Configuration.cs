using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PrimeGames.SDK.Prototype {

    [Serializable]
    public class PrototypeCountdownMessageLocalization {

        public LanguageType Language = LanguageType.English;
        public string Message = "Ad starts in";

    }

    [ProviderConfiguration(typeof(PrototypeAds))]
    public class PrototypeAds_Configuration : PropertyGroup {

        public override string Name => nameof(PrototypeAds);

        [field: SerializeField] public bool AutoAdsEnabled { get; private set; } = false;
        [field: SerializeField] public bool PauseDuringCountdown { get; private set; } = true;
        [field: SerializeField] public float AutoAdsIntervalSeconds { get; private set; } = 120.0f;
        [field: SerializeField] public float AutoAdsStartDelaySeconds { get; private set; } = 120.0f;
        [field: SerializeField] public int CountdownSeconds { get; private set; } = 2;
        [SerializeField] private string countdownMessage = "Ad starts in";
        [SerializeField] private List<PrototypeCountdownMessageLocalization> countdownLocalizations = CreateDefaultCountdownLocalizations();

        public List<PrototypeCountdownMessageLocalization> CountdownLocalizations => countdownLocalizations;

        public string GetCountdownMessage(LanguageType language) {
            PrototypeCountdownMessageLocalization localization = countdownLocalizations?.FirstOrDefault(item => item != null && item.Language == language);
            if (localization != null && !string.IsNullOrWhiteSpace(localization.Message)) {
                return localization.Message;
            }
            return countdownMessage;
        }

        private static List<PrototypeCountdownMessageLocalization> CreateDefaultCountdownLocalizations() {
            return new List<PrototypeCountdownMessageLocalization> {
                new() { Language = LanguageType.English, Message = "Ad starts in" },
                new() { Language = LanguageType.Russian, Message = "\u0420\u0435\u043a\u043b\u0430\u043c\u0430 \u043d\u0430\u0447\u043d\u0451\u0442\u0441\u044f \u0447\u0435\u0440\u0435\u0437" },
                new() { Language = LanguageType.Japanese, Message = "\u5e83\u544a\u958b\u59cb\u307e\u3067" },
                new() { Language = LanguageType.Chinese, Message = "\u5e7f\u544a\u5c06\u5728" },
                new() { Language = LanguageType.Turkish, Message = "Reklam \u015fu s\u00fcre i\u00e7inde ba\u015flayacak" },
                new() { Language = LanguageType.Hindi, Message = "\u0935\u093f\u091c\u094d\u091e\u093e\u092a\u0928 \u0936\u0941\u0930\u0942 \u0939\u094b\u0917\u093e" },
                new() { Language = LanguageType.Korean, Message = "\uad11\uace0 \uc2dc\uc791\uae4c\uc9c0" },
                new() { Language = LanguageType.Portuguese, Message = "O an\u00fancio come\u00e7a em" },
                new() { Language = LanguageType.Indonesian, Message = "Iklan dimulai dalam" },
                new() { Language = LanguageType.German, Message = "Werbung startet in" },
                new() { Language = LanguageType.Spanish, Message = "El anuncio empieza en" },
                new() { Language = LanguageType.Italian, Message = "L'annuncio inizia tra" },
                new() { Language = LanguageType.Ukrainian, Message = "\u0420\u0435\u043a\u043b\u0430\u043c\u0430 \u043f\u043e\u0447\u043d\u0435\u0442\u044c\u0441\u044f \u0447\u0435\u0440\u0435\u0437" },
                new() { Language = LanguageType.Polish, Message = "Reklama zacznie si\u0119 za" },
                new() { Language = LanguageType.French, Message = "La publicit\u00e9 commence dans" },
                new() { Language = LanguageType.Danish, Message = "Annoncen starter om" },
                new() { Language = LanguageType.Czech, Message = "Reklama za\u010dne za" },
                new() { Language = LanguageType.Afrikaans, Message = "Advertensie begin oor" },
                new() { Language = LanguageType.Icelandic, Message = "Augl\u00fdsing byrjar eftir" },
                new() { Language = LanguageType.Norwegian, Message = "Annonse starter om" },
                new() { Language = LanguageType.Swedish, Message = "Annonsen startar om" },
                new() { Language = LanguageType.Dutch, Message = "Advertentie start over" },
                new() { Language = LanguageType.Slovak, Message = "Reklama za\u010dne o" },
                new() { Language = LanguageType.Thai, Message = "\u0e42\u0e06\u0e29\u0e13\u0e32\u0e08\u0e30\u0e40\u0e23\u0e34\u0e48\u0e21\u0e43\u0e19" },
                new() { Language = LanguageType.Vietnamese, Message = "Qu\u1ea3ng c\u00e1o b\u1eaft \u0111\u1ea7u sau" }
            };
        }

        public override BoolProperty[] GetBoolProperties() {
            return new BoolProperty[] {
                new(
                    "Auto Ads Enabled",
                    getter: () => AutoAdsEnabled,
                    setter: value => { AutoAdsEnabled = value; }
                ),
                new(
                    "Pause During Countdown",
                    getter: () => PauseDuringCountdown,
                    setter: value => { PauseDuringCountdown = value; }
                )
            };
        }

        public override IntProperty[] GetIntProperties() {
            return new IntProperty[] {
                new(
                    "Countdown Seconds",
                    getter: () => CountdownSeconds,
                    setter: value => { CountdownSeconds = value; }
                )
            };
        }

        public override FloatProperty[] GetFloatProperties() {
            return new FloatProperty[] {
                new(
                    "Auto Ads Interval Seconds",
                    getter: () => AutoAdsIntervalSeconds,
                    setter: value => { AutoAdsIntervalSeconds = value; }
                ),
                new(
                    "Auto Ads Start Delay Seconds",
                    getter: () => AutoAdsStartDelaySeconds,
                    setter: value => { AutoAdsStartDelaySeconds = value; }
                )
            };
        }

    }

}
