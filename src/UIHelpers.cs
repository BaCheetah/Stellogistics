using UnityEngine;
using UnityEngine.UI;

namespace Stellogistics
{
    public static class UIHelpers
    {
        public static RectTransform SetRect(
            this RectTransform rt,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        public static RectTransform SetTopLeft(this RectTransform rt, Vector2 pos, Vector2 size)
        {
            return rt.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);
        }

        public static RectTransform SetTopStretch(this RectTransform rt, Vector2 pos, Vector2 size, float pivotX = 0.5f)
        {
            return rt.SetRect(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(pivotX, 1f), pos, size);
        }

        public static RectTransform Stretch(this RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform StretchFull(this RectTransform rt)
        {
            return rt.Stretch(Vector2.zero, Vector2.zero);
        }

        public static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget, out RectTransform rect)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            rect = go.GetComponent<RectTransform>();
            Image img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = raycastTarget;
            return img;
        }

        public static void ApplyButtonTint(Button btn, Color highlightedColor, Color pressedColor)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = highlightedColor;
            cb.pressedColor = pressedColor;
            btn.colors = cb;
        }
    }
}
