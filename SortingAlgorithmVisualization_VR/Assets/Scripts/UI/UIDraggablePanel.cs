// Copyright (c) 2021 Dylan Cheng (https://github.com/newlooper). All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class UIDraggablePanel : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform titleBar;
        [SerializeField] private Button closeButton;

        [Header("Configuration")]
        [SerializeField] private bool startHidden = false;

        public bool StartHidden
        {
            get => startHidden;
            set => startHidden = value;
        }

        private RectTransform _panelRectTransform;
        private RectTransform _parentRectTransform;
        private Canvas _canvas;
        private bool _isDragging;

        private void Awake()
        {
            _panelRectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            if (_panelRectTransform != null && _panelRectTransform.parent != null)
            {
                _parentRectTransform = _panelRectTransform.parent as RectTransform;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }
        }

        private void Start()
        {
            // Start with panel hidden if configured, keeping the 3D visualization unobstructed.
            // Awake has already completed across all GameObjects, so controllers have cached their references.
            if (startHidden)
            {
                gameObject.SetActive(false);
            }
        }

        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (titleBar == null)
            {
                _isDragging = true;
                return;
            }

            var cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? eventData.pressEventCamera : null;
            _isDragging = RectTransformUtility.RectangleContainsScreenPoint(titleBar, eventData.pressPosition, cam);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _panelRectTransform == null) return;

            var scaleFactor = (_canvas != null && _canvas.scaleFactor > 0.0001f) ? _canvas.scaleFactor : 1f;
            _panelRectTransform.anchoredPosition += eventData.delta / scaleFactor;

            ClampToScreen();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            ClampToScreen();
        }

        private void LateUpdate()
        {
            // Ensure panel stays inside canvas if window is resized while open
            if (gameObject.activeInHierarchy && !_isDragging)
            {
                ClampToScreen();
            }
        }

        public void ClampToScreen()
        {
            if (_panelRectTransform == null || _parentRectTransform == null) return;

            var parentRect = _parentRectTransform.rect;
            var corners = new Vector3[4];
            _panelRectTransform.GetWorldCorners(corners);

            for (var i = 0; i < 4; i++)
            {
                corners[i] = _parentRectTransform.InverseTransformPoint(corners[i]);
            }

            var minX = corners[0].x;
            var maxX = corners[2].x;
            var minY = corners[0].y;
            var maxY = corners[2].y;

            var deltaX = 0f;
            var deltaY = 0f;
            var margin = 4f;

            var panelWidth = maxX - minX;
            var panelHeight = maxY - minY;

            if (panelWidth <= parentRect.width)
            {
                if (minX < parentRect.xMin + margin)
                    deltaX = (parentRect.xMin + margin) - minX;
                else if (maxX > parentRect.xMax - margin)
                    deltaX = (parentRect.xMax - margin) - maxX;
            }
            else
            {
                deltaX = (parentRect.xMin + margin) - minX;
            }

            if (panelHeight <= parentRect.height)
            {
                if (minY < parentRect.yMin + margin)
                    deltaY = (parentRect.yMin + margin) - minY;
                else if (maxY > parentRect.yMax - margin)
                    deltaY = (parentRect.yMax - margin) - maxY;
            }
            else
            {
                deltaY = (parentRect.yMax - margin) - maxY;
            }

            if (Mathf.Abs(deltaX) > 0.01f || Mathf.Abs(deltaY) > 0.01f)
            {
                _panelRectTransform.anchoredPosition += new Vector2(deltaX, deltaY);
            }
        }
    }
}
