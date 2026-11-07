using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Stellogistics
{
    public class PointerHoverTracker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool IsHovered { get; private set; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
        }

        private void OnDisable()
        {
            IsHovered = false;
        }
    }

    public class ItemCellHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action OnHoverEnter;
        public Action OnHoverExit;

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHoverEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverExit?.Invoke();
        }
    }

    public class PanelDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform TargetRect;
        public Action OnDragStarted;
        public Action<Vector2> OnDragFinished;

        private Canvas _canvas;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (TargetRect == null)
            {
                return;
            }
            if (_canvas == null)
            {
                _canvas = TargetRect.GetComponentInParent<Canvas>();
            }
            OnDragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (TargetRect == null)
            {
                return;
            }

            float scaleFactor = (_canvas != null && _canvas.scaleFactor > 0.01f) ? _canvas.scaleFactor : 1f;
            TargetRect.anchoredPosition += eventData.delta / scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (TargetRect != null)
            {
                OnDragFinished?.Invoke(TargetRect.anchoredPosition);
            }
        }
    }

    public class PanelResizeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform TargetRect;
        public Action OnResizeStarted;
        public Action<float, float> OnResizing;
        public Action<float, float> OnResizeFinished;

        private Vector2 _grabOffset;
        private float _lastWidth = 310f;
        private float _lastTotalHeight = 300f;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (TargetRect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                TargetRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localMouse))
            {
                float currentW = TargetRect.rect.width;
                float currentH = TargetRect.rect.height;
                _grabOffset = new Vector2(currentW - localMouse.x, currentH - (-localMouse.y));
            }
            else
            {
                _grabOffset = Vector2.zero;
            }

            _lastWidth = TargetRect.rect.width;
            _lastTotalHeight = TargetRect.rect.height;
            OnResizeStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (TargetRect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                TargetRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localMouse))
            {
                float rawWidth = localMouse.x + _grabOffset.x;
                float rawTotalHeight = (-localMouse.y) + _grabOffset.y;

                _lastWidth = Mathf.Clamp(rawWidth, 240f, 700f);
                _lastTotalHeight = Mathf.Clamp(rawTotalHeight, 10f, 1020f);

                OnResizing?.Invoke(_lastWidth, _lastTotalHeight);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnResizeFinished?.Invoke(_lastWidth, _lastTotalHeight);
        }
    }
}
