using System;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmArchive
{
    /// <summary>uGUI 요소를 코드로 만드는 헬퍼. 기준 해상도는 1080x1920(세로).</summary>
    public static class UIFactory
    {
        public static readonly Color Accent = new Color(0.35f, 0.85f, 1f);
        public static readonly Color Accent2 = new Color(1f, 0.45f, 0.75f);
        public static readonly Color TextMain = new Color(0.94f, 0.96f, 1f);
        public static readonly Color TextDim = new Color(0.62f, 0.66f, 0.76f);
        public static readonly Color ButtonBg = new Color(1f, 1f, 1f, 0.09f);
        public static readonly Color PanelBg = new Color(0.03f, 0.035f, 0.07f, 1f);

        static Font font;

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                foreach (var name in new[] { "LegacyRuntime.ttf", "Arial.ttf" })
                {
                    try { font = Resources.GetBuiltinResource<Font>(name); }
                    catch (Exception) { font = null; }
                    if (font != null) break;
                }
                return font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(this RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>anchor 지점을 기준으로 pos만큼 떨어진 곳에 size 크기로 배치.</summary>
        public static RectTransform At(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static readonly Vector2 Top = new Vector2(0.5f, 1f);
        public static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);

        public static Image Panel(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent).Stretch();
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Image RoundedImage(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = RuntimeSprites.Rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.45f;
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, int fontSize, Action onClick, Color? bg = null)
        {
            var rt = Rect("Button", parent);
            var img = RoundedImage(rt, bg ?? ButtonBg);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.fadeDuration = 0.05f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var text = Label(rt, label, fontSize, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.Stretch();
            return btn;
        }

        public static void SetLabel(this Button b, string text) => b.GetComponentInChildren<Text>().text = text;

        public static void SetBg(this Button b, Color c) => ((Image)b.targetGraphic).color = c;
    }
}
