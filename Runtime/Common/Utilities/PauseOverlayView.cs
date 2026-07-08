using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace PrimeGames.SDK.Common {

    public static class PauseOverlayView {

        // Prototype SDK modals use sorting order 1000, so the pause overlay must stay below them.
        private const int SortingOrder = 900;
        private static readonly Action EmptyAction = () => { };

        private static PauseOverlayBehaviour instance;
        private static Action continueAction = EmptyAction;

        public static void SetContinueAction(Action action) {
            continueAction = action ?? EmptyAction;
            if (instance != null) {
                instance.SetContinueAction(continueAction);
            }
        }

        public static void SetVisible(bool value) {
            GetInstance().SetVisible(value);
        }

        public static void ShowContinuePrompt(Action onContinue = null) {
            GetInstance().ShowContinuePrompt(onContinue ?? EmptyAction);
        }

        public static void Clear() {
            if (instance == null) {
                return;
            }

            instance.Clear();
        }

        private static PauseOverlayBehaviour GetInstance() {
            if (instance != null) {
                return instance;
            }

            GameObject gameObject = new(nameof(PauseOverlayView));
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            instance = gameObject.AddComponent<PauseOverlayBehaviour>();
            instance.SetContinueAction(continueAction);
            return instance;
        }

        private class PauseOverlayBehaviour : MonoBehaviour {

            private static readonly Color PrimeTextColor = new(1.0f, 0.58f, 0.0f, 1.0f);

            private Action continueAction = EmptyAction;
            private Action promptContinueAction = EmptyAction;
            private bool visible;
            private bool manualPrompt;
            private bool cursorStateSaved;
            private bool previousCursorVisible;
            private CursorLockMode previousCursorLockMode;

            private UIDocument document;
            private VisualElement overlayElement;
            private VisualElement contentElement;
            private Image playIconImage;
            private Label messageLabel;
            private Image watermarkImage;
            private Texture2D overlayTexture;
            private Texture2D playIconTexture;
            private Texture2D watermarkTexture;

            public void SetContinueAction(Action action) {
                continueAction = action ?? EmptyAction;
            }

            public void ShowContinuePrompt(Action onContinue) {
                manualPrompt = true;
                promptContinueAction = onContinue ?? EmptyAction;
                SetVisible(true);
            }

            public void SetVisible(bool value) {
                if (visible == value) {
                    return;
                }

                visible = value;
                if (visible) {
                    EnsureUi();
                    RefreshLayout();
                    SaveCursorState();
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    overlayElement.style.display = DisplayStyle.Flex;
                    return;
                }

                if (overlayElement != null) {
                    overlayElement.style.display = DisplayStyle.None;
                }
                manualPrompt = false;
                promptContinueAction = EmptyAction;
                RestoreCursorState();
            }

            public void Clear() {
                SetVisible(false);
            }

            private void EnsureUi() {
                if (document != null) {
                    return;
                }

                GameObject documentPrefab = PrefabReference.Load("PrototypeDocument").Prefab;
                GameObject documentObject = Instantiate(documentPrefab);
                documentObject.name = nameof(PauseOverlayView);
                DontDestroyOnLoad(documentObject);

                document = documentObject.GetComponent<UIDocument>();
                document.sortingOrder = SortingOrder;
                document.rootVisualElement.Clear();
                document.rootVisualElement.pickingMode = PickingMode.Ignore;

                overlayTexture = CreateOverlayTexture();
                playIconTexture = CreatePlayIconTexture();
                watermarkTexture = Resources.Load<Texture2D>("PrimeSDK/prime_publishing_watermark");

                overlayElement = new VisualElement {
                    pickingMode = PickingMode.Position
                };
                overlayElement.style.position = Position.Absolute;
                overlayElement.style.left = 0;
                overlayElement.style.top = 0;
                overlayElement.style.right = 0;
                overlayElement.style.bottom = 0;
                overlayElement.style.display = DisplayStyle.None;
                overlayElement.style.flexDirection = FlexDirection.Column;
                overlayElement.style.alignItems = Align.Center;
                overlayElement.style.justifyContent = Justify.Center;
                overlayElement.style.backgroundImage = new StyleBackground(overlayTexture);
                overlayElement.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
                overlayElement.RegisterCallback<PointerDownEvent>(OnOverlayPointerDown);

                contentElement = new VisualElement {
                    pickingMode = PickingMode.Ignore
                };
                contentElement.style.width = Length.Percent(100.0f);
                contentElement.style.flexDirection = FlexDirection.Column;
                contentElement.style.alignItems = Align.Center;
                contentElement.style.justifyContent = Justify.Center;

                playIconImage = new Image {
                    image = playIconTexture,
                    scaleMode = ScaleMode.ScaleToFit,
                    pickingMode = PickingMode.Ignore
                };

                messageLabel = new Label {
                    pickingMode = PickingMode.Ignore
                };
                messageLabel.style.color = PrimeTextColor;
                messageLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                messageLabel.style.whiteSpace = WhiteSpace.Normal;

                watermarkImage = new Image {
                    image = watermarkTexture,
                    scaleMode = ScaleMode.ScaleToFit,
                    pickingMode = PickingMode.Ignore
                };
                watermarkImage.style.position = Position.Absolute;
                watermarkImage.style.left = 34.0f;
                watermarkImage.style.top = 28.0f;

                contentElement.Add(playIconImage);
                contentElement.Add(messageLabel);
                overlayElement.Add(contentElement);
                overlayElement.Add(watermarkImage);
                document.rootVisualElement.Add(overlayElement);
            }

            private void RefreshLayout() {
                float panelWidth = GetPanelSize(document.rootVisualElement.resolvedStyle.width, 600.0f);
                float panelHeight = GetPanelSize(document.rootVisualElement.resolvedStyle.height, 400.0f);

                float iconSize = Mathf.Min(176.0f, Mathf.Max(116.0f, panelHeight * 0.4f));
                float textWidth = Mathf.Max(240.0f, panelWidth - 64.0f);
                float textHeight = Mathf.Min(84.0f, panelHeight * 0.24f);
                float fontSize = Mathf.Max(13.0f, Mathf.Min(20.0f, panelWidth / 34.0f));
                float watermarkWidth = Mathf.Min(170.0f, panelWidth * 0.26f);
                float watermarkHeight = watermarkWidth * 67.0f / 240.0f;

                playIconImage.style.width = iconSize;
                playIconImage.style.height = iconSize;
                playIconImage.style.marginBottom = 14.0f;

                messageLabel.text = GetMessage();
                messageLabel.style.width = textWidth;
                messageLabel.style.height = textHeight;
                messageLabel.style.fontSize = fontSize;

                watermarkImage.style.width = watermarkWidth;
                watermarkImage.style.height = watermarkHeight;
            }

            private float GetPanelSize(float value, float fallback) {
                return float.IsNaN(value) || value <= 0.0f ? fallback : value;
            }

            private void OnOverlayPointerDown(PointerDownEvent pointerDownEvent) {
                if (manualPrompt) {
                    Action onContinue = promptContinueAction;
                    SetVisible(false);
                    onContinue.Invoke();
                }
                else {
                    continueAction.Invoke();
                }
                pointerDownEvent.StopPropagation();
            }

            private void SaveCursorState() {
                if (cursorStateSaved) {
                    return;
                }

                previousCursorVisible = Cursor.visible;
                previousCursorLockMode = Cursor.lockState;
                cursorStateSaved = true;
            }

            private void RestoreCursorState() {
                if (!cursorStateSaved) {
                    return;
                }

                Cursor.visible = previousCursorVisible;
                Cursor.lockState = previousCursorLockMode;
                cursorStateSaved = false;
            }

            private string GetMessage() {
                return Application.systemLanguage == SystemLanguage.Russian
                    ? "Чтобы продолжить, кликни по этой области."
                    : "Click this area to continue.";
            }

            private Texture2D CreateOverlayTexture() {
                const int height = 192;
                Texture2D texture = new(1, height) {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear
                };

                for (int y = 0; y < height; y++) {
                    float value = (float)y / (height - 1);
                    float alpha = Mathf.Lerp(0.0f, 0.96f, value);
                    texture.SetPixel(0, y, new Color(0.0f, 0.0f, 0.0f, alpha));
                }

                texture.Apply();
                return texture;
            }

            private Texture2D CreatePlayIconTexture() {
                const int width = 256;
                const int height = 256;
                Texture2D texture = new(width, height) {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear
                };

                ClearTexture(texture);
                DrawPlayShape(texture, 8.0f, -7.0f, true);
                DrawPlayShape(texture, 0.0f, 0.0f, false);

                texture.Apply();
                return texture;
            }

            private void ClearTexture(Texture2D texture) {
                for (int y = 0; y < texture.height; y++) {
                    for (int x = 0; x < texture.width; x++) {
                        texture.SetPixel(x, y, new Color(0.0f, 0.0f, 0.0f, 0.0f));
                    }
                }
            }

            private Color GetPrimeGradientColor(float t) {
                Color top = new(1.0f, 0.74f, 0.0f, 0.98f);
                Color middle = new(1.0f, 0.38f, 0.0f, 0.98f);
                Color bottom = new(1.0f, 0.12f, 0.05f, 0.98f);
                return t < 0.5f
                    ? Color.Lerp(top, middle, t * 2.0f)
                    : Color.Lerp(middle, bottom, (t - 0.5f) * 2.0f);
            }

            private void DrawPlayShape(Texture2D texture, float offsetX, float offsetY, bool shadow) {
                List<UnityEngine.Vector2> points = CreatePlayShapePoints(offsetX, offsetY);
                int minX = texture.width;
                int maxX = 0;
                int minY = texture.height;
                int maxY = 0;

                foreach (UnityEngine.Vector2 point in points) {
                    minX = Mathf.Min(minX, Mathf.FloorToInt(point.x));
                    maxX = Mathf.Max(maxX, Mathf.CeilToInt(point.x));
                    minY = Mathf.Min(minY, Mathf.FloorToInt(point.y));
                    maxY = Mathf.Max(maxY, Mathf.CeilToInt(point.y));
                }

                minX = Mathf.Clamp(minX, 0, texture.width - 1);
                maxX = Mathf.Clamp(maxX, 0, texture.width - 1);
                minY = Mathf.Clamp(minY, 0, texture.height - 1);
                maxY = Mathf.Clamp(maxY, 0, texture.height - 1);

                for (int y = minY; y <= maxY; y++) {
                    for (int x = minX; x <= maxX; x++) {
                        if (!IsInsidePolygon(x + 0.5f, y + 0.5f, points)) {
                            continue;
                        }

                        float t = Mathf.InverseLerp(minY, maxY, y);
                        Color color = shadow
                            ? new Color(0.0f, 0.0f, 0.0f, 0.3f)
                            : GetPrimeGradientColor(1.0f - t);
                        texture.SetPixel(x, y, color);
                    }
                }
            }

            private List<UnityEngine.Vector2> CreatePlayShapePoints(float offsetX, float offsetY) {
                List<UnityEngine.Vector2> points = new();
                points.Add(new UnityEngine.Vector2(92.0f + offsetX, 55.0f + offsetY));
                AddQuadratic(points, 70.0f + offsetX, 42.0f + offsetY, 70.0f + offsetX, 70.0f + offsetY);
                points.Add(new UnityEngine.Vector2(70.0f + offsetX, 186.0f + offsetY));
                AddQuadratic(points, 70.0f + offsetX, 214.0f + offsetY, 94.0f + offsetX, 199.0f + offsetY);
                points.Add(new UnityEngine.Vector2(198.0f + offsetX, 139.0f + offsetY));
                AddQuadratic(points, 218.0f + offsetX, 128.0f + offsetY, 198.0f + offsetX, 117.0f + offsetY);
                points.Add(new UnityEngine.Vector2(92.0f + offsetX, 55.0f + offsetY));
                return points;
            }

            private void AddQuadratic(List<UnityEngine.Vector2> points, float controlX, float controlY, float endX, float endY) {
                UnityEngine.Vector2 start = points[points.Count - 1];
                UnityEngine.Vector2 control = new(controlX, controlY);
                UnityEngine.Vector2 end = new(endX, endY);

                for (int index = 1; index <= 12; index++) {
                    float t = index / 12.0f;
                    float oneMinusT = 1.0f - t;
                    UnityEngine.Vector2 point = oneMinusT * oneMinusT * start
                        + 2.0f * oneMinusT * t * control
                        + t * t * end;
                    points.Add(point);
                }
            }

            private bool IsInsidePolygon(float px, float py, List<UnityEngine.Vector2> points) {
                bool inside = false;
                int previous = points.Count - 1;
                for (int current = 0; current < points.Count; current++) {
                    UnityEngine.Vector2 currentPoint = points[current];
                    UnityEngine.Vector2 previousPoint = points[previous];
                    bool intersects = currentPoint.y > py != previousPoint.y > py
                        && px < (previousPoint.x - currentPoint.x) * (py - currentPoint.y) / (previousPoint.y - currentPoint.y) + currentPoint.x;
                    if (intersects) {
                        inside = !inside;
                    }
                    previous = current;
                }
                return inside;
            }

            private void OnDisable() {
                Clear();
            }

            private void OnDestroy() {
                Clear();
            }

        }

    }

}
