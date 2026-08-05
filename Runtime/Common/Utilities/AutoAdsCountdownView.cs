using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Common {

    public static class AutoAdsCountdownView {

        private const int PanelSortingOrder = 32765;
        private const int DocumentSortingOrder = 32765;
        private static AutoAdsCountdownBehaviour instance;

        public static void Show(int seconds, string messageFormat, Action onComplete) {
            Show(seconds, GetAdvertisementTitle(LanguageType.English), messageFormat, onComplete);
        }

        public static void Show(int seconds, string title, string messageFormat, Action onComplete) {
            GetInstance().Show(seconds, title, messageFormat, onComplete);
        }

        public static void StartAutoAds(float startDelaySeconds, float intervalSeconds, Func<Action, Action<bool>, bool> invokeCountdown) {
            GetInstance().StartAutoAds(startDelaySeconds, intervalSeconds, invokeCountdown);
        }

        public static void StopAutoAds() {
            if (instance != null) {
                instance.StopAutoAds();
            }
        }

        public static void Delay(float delaySeconds, Action onComplete) {
            GetInstance().Delay(delaySeconds, onComplete);
        }

        public static void Hide() {
            if (instance != null) {
                instance.Hide();
            }
        }

        public static string GetAdvertisementTitle(LanguageType language) {
            return language switch {
                LanguageType.Russian => "\u0420\u0435\u043a\u043b\u0430\u043c\u0430",
                LanguageType.Japanese => "\u5e83\u544a",
                LanguageType.Chinese => "\u5e7f\u544a",
                LanguageType.Turkish => "Reklam",
                LanguageType.Hindi => "\u0935\u093f\u091c\u094d\u091e\u093e\u092a\u0928",
                LanguageType.Korean => "\uad11\uace0",
                LanguageType.Portuguese => "An\u00fancio",
                LanguageType.Indonesian => "Iklan",
                LanguageType.German => "Werbung",
                LanguageType.Spanish => "Anuncio",
                LanguageType.Italian => "Annuncio",
                LanguageType.Ukrainian => "\u0420\u0435\u043a\u043b\u0430\u043c\u0430",
                LanguageType.Polish => "Reklama",
                LanguageType.French => "Publicit\u00e9",
                LanguageType.Danish => "Annonce",
                LanguageType.Czech => "Reklama",
                LanguageType.Afrikaans => "Advertensie",
                LanguageType.Icelandic => "Augl\u00fdsing",
                LanguageType.Norwegian => "Annonse",
                LanguageType.Swedish => "Annons",
                LanguageType.Dutch => "Advertentie",
                LanguageType.Slovak => "Reklama",
                LanguageType.Thai => "\u0e42\u0e06\u0e29\u0e13\u0e32",
                LanguageType.Vietnamese => "Qu\u1ea3ng c\u00e1o",
                _ => "Advertisement"
            };
        }

        private static AutoAdsCountdownBehaviour GetInstance() {
            if (instance != null) {
                return instance;
            }

            GameObject gameObject = new(nameof(AutoAdsCountdownView));
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            instance = gameObject.AddComponent<AutoAdsCountdownBehaviour>();
            return instance;
        }

        private sealed class AutoAdsCountdownBehaviour : MonoBehaviour {

            private UIDocument document;
            private PanelSettings panelSettings;
            private VisualElement overlayElement;
            private Label titleLabel;
            private Label countdownLabel;
            private Coroutine countdownCoroutine;
            private Coroutine autoAdsCoroutine;
            private Coroutine delayCoroutine;

            public void Show(int seconds, string messageFormat, Action onComplete) {
                Show(seconds, GetAdvertisementTitle(LanguageType.English), messageFormat, onComplete);
            }

            public void Show(int seconds, string title, string messageFormat, Action onComplete) {
                EnsureUi();
                if (countdownCoroutine != null) {
                    StopCoroutine(countdownCoroutine);
                }
                countdownCoroutine = StartCoroutine(Countdown(seconds, title, messageFormat, onComplete));
            }

            public void StartAutoAds(float startDelaySeconds, float intervalSeconds, Func<Action, Action<bool>, bool> invokeCountdown) {
                if (autoAdsCoroutine != null) {
                    StopCoroutine(autoAdsCoroutine);
                }
                autoAdsCoroutine = StartCoroutine(AutoAds(startDelaySeconds, intervalSeconds, invokeCountdown));
            }

            public void StopAutoAds() {
                if (autoAdsCoroutine != null) {
                    StopCoroutine(autoAdsCoroutine);
                    autoAdsCoroutine = null;
                }
                if (delayCoroutine != null) {
                    StopCoroutine(delayCoroutine);
                    delayCoroutine = null;
                }
                if (countdownCoroutine != null) {
                    StopCoroutine(countdownCoroutine);
                    countdownCoroutine = null;
                }
                Hide();
            }

            public void Delay(float delaySeconds, Action onComplete) {
                if (delayCoroutine != null) {
                    StopCoroutine(delayCoroutine);
                }
                delayCoroutine = StartCoroutine(DelayCoroutine(delaySeconds, onComplete));
            }

            public void Hide() {
                if (overlayElement != null) {
                    overlayElement.style.display = DisplayStyle.None;
                }
                if (document != null) {
                    document.rootVisualElement.pickingMode = PickingMode.Ignore;
                }
            }

            private IEnumerator AutoAds(float startDelaySeconds, float intervalSeconds, Func<Action, Action<bool>, bool> invokeCountdown) {
                if (startDelaySeconds > 0.0f) {
                    yield return new WaitForSecondsRealtime(startDelaySeconds);
                }

                while (true) {
                    bool completed = false;
                    bool invoked = false;
                    try {
                        invoked = invokeCountdown(null, _ => { completed = true; });
                    }
                    catch (Exception exception) {
                        Logger.CreateError(nameof(AutoAdsCountdownView), nameof(AutoAds), exception);
                    }

                    if (invoked) {
                        yield return new WaitUntil(() => completed);
                    }

                    yield return new WaitForSecondsRealtime(Mathf.Max(1.0f, intervalSeconds));
                }
            }

            private IEnumerator DelayCoroutine(float delaySeconds, Action onComplete) {
                if (delaySeconds > 0.0f) {
                    yield return new WaitForSecondsRealtime(delaySeconds);
                }

                delayCoroutine = null;
                onComplete?.Invoke();
            }

            private IEnumerator Countdown(int seconds, string title, string messageFormat, Action onComplete) {
                int clampedSeconds = Mathf.Max(0, seconds);
                if (panelSettings != null) {
                    panelSettings.sortingOrder = PanelSortingOrder;
                }
                document.sortingOrder = DocumentSortingOrder;
                document.rootVisualElement.BringToFront();
                document.rootVisualElement.pickingMode = PickingMode.Position;
                overlayElement.BringToFront();
                overlayElement.style.display = DisplayStyle.Flex;
                titleLabel.text = GetCountdownTitle(messageFormat, title);

                for (int value = clampedSeconds; value > 0; value--) {
                    countdownLabel.text = value.ToString();
                    yield return new WaitForSecondsRealtime(1.0f);
                }

                Hide();
                countdownCoroutine = null;
                onComplete?.Invoke();
            }

            private void EnsureUi() {
                if (document != null) {
                    return;
                }

                GameObject documentPrefab = PrefabReference.Load("PrototypeDocument").Prefab;
                GameObject documentObject = Instantiate(documentPrefab);
                documentObject.name = nameof(AutoAdsCountdownView);
                DontDestroyOnLoad(documentObject);

                document = documentObject.GetComponent<UIDocument>();
                if (document.panelSettings != null) {
                    panelSettings = Instantiate(document.panelSettings);
                    panelSettings.name = nameof(AutoAdsCountdownView) + "PanelSettings";
                    panelSettings.sortingOrder = PanelSortingOrder;
                    document.panelSettings = panelSettings;
                }
                document.sortingOrder = DocumentSortingOrder;
                document.rootVisualElement.Clear();
                document.rootVisualElement.pickingMode = PickingMode.Ignore;

                VisualTreeAsset visualTree = VisualTreeReference.LoadVisualTree(nameof(AutoAdsCountdownView));
                visualTree.CloneTree(document.rootVisualElement);

                overlayElement = document.rootVisualElement.Q<VisualElement>("Overlay");
                overlayElement.pickingMode = PickingMode.Position;
                RegisterInputBlocker(overlayElement);
                titleLabel = document.rootVisualElement.Q<Label>("TitleLabel");
                countdownLabel = document.rootVisualElement.Q<Label>("CountdownLabel");
                Hide();
            }

            private static void RegisterInputBlocker(VisualElement element) {
                element.RegisterCallback<PointerDownEvent>(StopInputEvent);
                element.RegisterCallback<PointerUpEvent>(StopInputEvent);
                element.RegisterCallback<PointerMoveEvent>(StopInputEvent);
                element.RegisterCallback<ClickEvent>(StopInputEvent);
                element.RegisterCallback<MouseDownEvent>(StopInputEvent);
                element.RegisterCallback<MouseUpEvent>(StopInputEvent);
                element.RegisterCallback<MouseMoveEvent>(StopInputEvent);
            }

            private static void StopInputEvent(EventBase evt) {
                evt.StopPropagation();
                evt.PreventDefault();
            }

            private static string GetCountdownTitle(string messageFormat, string fallbackTitle) {
                string message = string.IsNullOrWhiteSpace(messageFormat) ? string.Empty : messageFormat;
                if (!string.IsNullOrEmpty(message)) {
                    message = message.Replace("{0}", string.Empty).Trim();
                    message = message.Trim('-', '\u2013', '\u2014', ':', ';', ',', '.', ' ');
                }

                if (!string.IsNullOrWhiteSpace(message)) {
                    return message;
                }
                return string.IsNullOrEmpty(fallbackTitle) ? GetAdvertisementTitle(LanguageType.English) : fallbackTitle;
            }

            private void OnDisable() {
                Hide();
            }

            private void OnDestroy() {
                Hide();
                if (panelSettings != null) {
                    Destroy(panelSettings);
                    panelSettings = null;
                }
            }

        }

    }

}
