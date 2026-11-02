using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
                // TargetRect pivot is top-left (0, 1), so bottom-right corner is at (+width, -height)
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

    public class UIStarmapLogisticsPanel : MonoBehaviour
    {
        private class ItemRowView
        {
            public GameObject RootGo;
            public RectTransform RootRect;
            public Image BgImage;
            public RectTransform FillBarRect;
            public Image FillBarImage;
            public Image StatusStripeImage;
            public Image IconImage;
            public Text NameText;
            public Text MetaText;
            public Text AmountText;
            public Text SubStatusText;
        }

        private class FilterToggleRow
        {
            public EItemCategoryFilter Category;
            public Button Btn;
            public Image CheckBoxBg;
            public Text CheckMarkText;
            public Text LabelText;
        }

        private class CustomPickerCell
        {
            public int ItemId;
            public string ItemName;
            public EItemCategoryFilter Category;
            public int TabIndex;
            public GameObject CellGo;
            public Image BgImage;
            public Image IconImage;
        }

        private StellogisticsConfig _config;
        private UIGame _uiGame;
        private Font _gameFont;

        private RectTransform _panelRect;
        private RectTransform _headerRect;
        private Image _panelBg;
        private PointerHoverTracker _panelHoverTracker;
        private PointerHoverTracker _optionsHoverTracker;
        private PointerHoverTracker _customPickerHoverTracker;

        private Button _panelCollapseBtn;
        private Text _panelCollapseBtnText;
        private Text _titleText;
        private Text _subtitleText;
        private Text _fleetInfoText;

        private Button _filterModeBtn;
        private Text _filterModeBtnText;
        private Button _sortModeBtn;
        private Text _sortModeBtnText;
        private Button _optionsBtn;
        private Text _optionsBtnText;

        private RectTransform _scrollViewportRect;
        private RectTransform _scrollContentRect;
        private ScrollRect _scrollRect;
        private GameObject _scrollbarGo;
        private Scrollbar _verticalScrollbar;

        private GameObject _resizeGripGo;

        private GameObject _emptyStateGo;
        private Text _emptyStateText;

        private GameObject _exportsHeaderGo;
        private RectTransform _exportsHeaderRect;
        private Text _exportsHeaderText;
        private Text _exportsAlertText;

        private GameObject _importsHeaderGo;
        private RectTransform _importsHeaderRect;
        private Text _importsHeaderText;
        private Text _importsAlertText;

        private GameObject _optionsMenuGo;
        private RectTransform _optionsMenuRect;
        private Text _optionsTargetLabel;
        private Text _excludeRowLabelText;
        private Text _excludeThresholdBtnText;
        private readonly List<FilterToggleRow> _filterToggles = new List<FilterToggleRow>();

        private GameObject _customPickerGo;
        private RectTransform _customPickerRect;
        private Text _customPickerHoveredLabel;
        private Image _customTabItemsBg;
        private Image _customTabBuildingsBg;
        private int _customPickerActiveTab = 1;
        private bool _customPickerOpen = false;
        private readonly List<CustomPickerCell> _customPickerCells = new List<CustomPickerCell>();
        private readonly HashSet<int> _storedItemsSet = new HashSet<int>();

        private readonly List<ItemRowView> _rowPool = new List<ItemRowView>();

        private int _lastTargetAstroId = -1;
        private int _lastGalaxySeed = 0;
        private string _lastTargetDisplayName = "";
        private PlanetData _lastResolvedPlanet = null;
        private StarData _lastResolvedStar = null;
        private float _nextRefreshTime = 0f;
        private bool _forceRefresh = true;
        private float _activeContentHeight = 160f;

        private bool _panelCollapsed = false;
        private bool _exportsCollapsed = false;
        private bool _importsCollapsed = false;
        private bool _optionsMenuOpen = false;
        private bool _isResizing = false;

        private static readonly Color PanelBgColor = new Color(0.055f, 0.085f, 0.13f, 0.94f);
        private static readonly Color HeaderBgColor = new Color(0.09f, 0.145f, 0.22f, 0.98f);
        private static readonly Color SectionHeaderBgColor = new Color(0.12f, 0.19f, 0.28f, 0.95f);
        private static readonly Color SectionHeaderHoverColor = new Color(0.17f, 0.27f, 0.39f, 0.98f);
        private static readonly Color RowEvenBgColor = new Color(0.08f, 0.12f, 0.18f, 0.78f);
        private static readonly Color RowOddBgColor = new Color(0.065f, 0.10f, 0.15f, 0.78f);

        private static readonly Color CriticalColor = new Color(1.0f, 0.34f, 0.34f, 1f);
        private static readonly Color CriticalFillColor = new Color(0.85f, 0.18f, 0.18f, 0.24f);
        private static readonly Color WarningColor = new Color(1.0f, 0.70f, 0.24f, 1f);
        private static readonly Color WarningFillColor = new Color(0.85f, 0.52f, 0.12f, 0.22f);
        private static readonly Color HealthyColor = new Color(0.30f, 0.90f, 0.58f, 1f);
        private static readonly Color HealthyFillColor = new Color(0.18f, 0.68f, 0.42f, 0.18f);

        private const float HeaderHeight = 74f;
        private const float CollapsedTabWidth = 148f;
        private const float CollapsedTabHeight = 32f;
        private const float SectionHeaderHeight = 26f;
        private const float RowHeight = 38f;
        private const float RowSpacing = 2f;
        private const float MinScrollHeight = 56f;
        private const float ScrollbarWidth = 7f;

        public void Initialize(UIGame uiGame, StellogisticsConfig config)
        {
            _uiGame = uiGame;
            _config = config;

            if (_uiGame.planetDetail != null && _uiGame.planetDetail.typeText != null)
            {
                _gameFont = _uiGame.planetDetail.typeText.font;
            }
            else if (_uiGame.starmap != null && _uiGame.starmap.cursorViewText != null)
            {
                _gameFont = _uiGame.starmap.cursorViewText.font;
            }
            if (_gameFont == null)
            {
                _gameFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            BuildUI();
            BuildOptionsMenu();
            BuildCustomItemPicker();
        }

        public bool IsPointerOverUI()
        {
            if (!gameObject.activeInHierarchy)
            {
                return false;
            }
            if (_panelHoverTracker != null && _panelHoverTracker.IsHovered)
            {
                return true;
            }
            if (_optionsMenuOpen && _optionsHoverTracker != null && _optionsHoverTracker.IsHovered)
            {
                return true;
            }
            if (_customPickerOpen && _customPickerHoverTracker != null && _customPickerHoverTracker.IsHovered)
            {
                return true;
            }
            return false;
        }

        private void BuildUI()
        {
            _panelRect = gameObject.GetComponent<RectTransform>();
            if (_panelRect == null)
            {
                _panelRect = gameObject.AddComponent<RectTransform>();
            }

            // Anchor & pivot at top-left (0, 1) so collapsing to a taskbar tab and lower-right resizing keep top-left fixed
            _panelRect.anchorMin = new Vector2(0f, 1f);
            _panelRect.anchorMax = new Vector2(0f, 1f);
            _panelRect.pivot = new Vector2(0f, 1f);
            _panelRect.sizeDelta = new Vector2(_config.PanelWidth.Value, 240f);

            if (_config.UseCustomPosition.Value)
            {
                _panelRect.anchoredPosition = new Vector2(_config.CustomPanelPosX.Value, _config.CustomPanelPosY.Value);
            }
            else
            {
                _panelRect.anchoredPosition = new Vector2(StellogisticsConfig.DefaultPosX, StellogisticsConfig.DefaultPosY);
            }

            _panelBg = gameObject.AddComponent<Image>();
            _panelBg.color = PanelBgColor;
            _panelBg.raycastTarget = true;

            _panelHoverTracker = gameObject.AddComponent<PointerHoverTracker>();

            Outline panelOutline = gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.25f, 0.55f, 0.80f, 0.45f);
            panelOutline.effectDistance = new Vector2(1f, -1f);

            // Header Container (Draggable!)
            GameObject headerGo = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(PanelDragHandler));
            headerGo.transform.SetParent(_panelRect, false);
            _headerRect = headerGo.GetComponent<RectTransform>();
            _headerRect.anchorMin = new Vector2(0f, 1f);
            _headerRect.anchorMax = new Vector2(1f, 1f);
            _headerRect.pivot = new Vector2(0.5f, 1f);
            _headerRect.anchoredPosition = Vector2.zero;
            _headerRect.sizeDelta = new Vector2(0f, HeaderHeight);
            Image headerImg = headerGo.GetComponent<Image>();
            headerImg.color = HeaderBgColor;
            headerImg.raycastTarget = true;

            PanelDragHandler dragHandler = headerGo.GetComponent<PanelDragHandler>();
            dragHandler.TargetRect = _panelRect;
            dragHandler.OnDragStarted = () => _config.UseCustomPosition.Value = true;
            dragHandler.OnDragFinished = pos =>
            {
                _config.SaveCustomPanelPosition(pos.x, pos.y);
                if (_customPickerOpen)
                {
                    PositionCustomPickerBesidePanel();
                }
            };

            // Panel Collapse Button (top-left)
            _panelCollapseBtn = CreateHeaderButton("CollapsePanelBtn", _headerRect, new Vector2(8f, -6f), new Vector2(22f, 20f), false, out _panelCollapseBtnText);
            _panelCollapseBtnText.text = "▼";
            _panelCollapseBtn.onClick.AddListener(OnTogglePanelCollapseClicked);

            // Title Text
            _titleText = CreateText("TitleText", _headerRect, 13, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.94f, 0.97f, 1f, 1f));
            _titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _titleText.rectTransform.pivot = new Vector2(0f, 1f);
            _titleText.rectTransform.anchoredPosition = new Vector2(34f, -6f);
            _titleText.rectTransform.sizeDelta = new Vector2(-190f, 20f);

            // Top-right Header Buttons: [Ext Only] [Sort: Alert] [Filter ▾]
            _filterModeBtn = CreateHeaderButton("FilterModeBtn", _headerRect, new Vector2(-114f, -6f), new Vector2(52f, 20f), true, out _filterModeBtnText);
            _filterModeBtn.onClick.AddListener(OnFilterModeButtonClicked);

            _sortModeBtn = CreateHeaderButton("SortModeBtn", _headerRect, new Vector2(-58f, -6f), new Vector2(52f, 20f), true, out _sortModeBtnText);
            _sortModeBtn.onClick.AddListener(OnSortModeButtonClicked);

            _optionsBtn = CreateHeaderButton("OptionsBtn", _headerRect, new Vector2(-6f, -6f), new Vector2(48f, 20f), true, out _optionsBtnText);
            _optionsBtnText.text = "Filter ▾";
            _optionsBtn.onClick.AddListener(OnOptionsButtonClicked);

            // Subtitle Text
            _subtitleText = CreateText("SubtitleText", _headerRect, 11, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.68f, 0.82f, 0.94f, 1f));
            _subtitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _subtitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _subtitleText.rectTransform.pivot = new Vector2(0f, 1f);
            _subtitleText.rectTransform.anchoredPosition = new Vector2(10f, -31f);
            _subtitleText.rectTransform.sizeDelta = new Vector2(-20f, 18f);

            // Fleet / Warper Info
            _fleetInfoText = CreateText("FleetInfoText", _headerRect, 11, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.56f, 0.72f, 0.85f, 0.95f));
            _fleetInfoText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _fleetInfoText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _fleetInfoText.rectTransform.pivot = new Vector2(0f, 1f);
            _fleetInfoText.rectTransform.anchoredPosition = new Vector2(10f, -50f);
            _fleetInfoText.rectTransform.sizeDelta = new Vector2(-20f, 18f);

            // Scroll View
            GameObject scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGo.transform.SetParent(_panelRect, false);
            _scrollViewportRect = scrollGo.GetComponent<RectTransform>();
            _scrollViewportRect.anchorMin = new Vector2(0f, 1f);
            _scrollViewportRect.anchorMax = new Vector2(1f, 1f);
            _scrollViewportRect.pivot = new Vector2(0.5f, 1f);
            _scrollViewportRect.anchoredPosition = new Vector2(0f, -HeaderHeight);
            _scrollViewportRect.sizeDelta = new Vector2(0f, 160f);

            Image maskImg = scrollGo.GetComponent<Image>();
            maskImg.color = new Color(0f, 0f, 0f, 0.01f);
            maskImg.raycastTarget = true;
            Mask mask = scrollGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(_scrollViewportRect, false);
            _scrollContentRect = contentGo.GetComponent<RectTransform>();
            _scrollContentRect.anchorMin = new Vector2(0f, 1f);
            _scrollContentRect.anchorMax = new Vector2(1f, 1f);
            _scrollContentRect.pivot = new Vector2(0f, 1f);
            _scrollContentRect.anchoredPosition = Vector2.zero;
            _scrollContentRect.sizeDelta = new Vector2(0f, 160f);

            // Vertical Scrollbar (only shown when list exceeds viewport height)
            BuildScrollbar();

            _scrollRect = scrollGo.GetComponent<ScrollRect>();
            _scrollRect.viewport = _scrollViewportRect;
            _scrollRect.content = _scrollContentRect;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 32f;
            _scrollRect.verticalScrollbar = _verticalScrollbar;

            // Empty State
            _emptyStateGo = new GameObject("EmptyState", typeof(RectTransform));
            _emptyStateGo.transform.SetParent(_scrollContentRect, false);
            RectTransform emptyRect = _emptyStateGo.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0f, 1f);
            emptyRect.anchorMax = new Vector2(1f, 1f);
            emptyRect.pivot = new Vector2(0.5f, 1f);
            emptyRect.anchoredPosition = new Vector2(0f, -8f);
            emptyRect.sizeDelta = new Vector2(-20f, 40f);
            _emptyStateText = CreateText("EmptyText", emptyRect, 12, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.60f, 0.72f, 0.82f, 0.95f));
            _emptyStateText.rectTransform.anchorMin = Vector2.zero;
            _emptyStateText.rectTransform.anchorMax = Vector2.one;
            _emptyStateText.rectTransform.sizeDelta = Vector2.zero;
            _emptyStateText.text = "No active Interplanetary Logistics slots.";

            // Clickable Collapsible Section Headers (Exports & Imports)
            CreateSectionHeader(
                "ExportsHeader",
                OnToggleExportsCollapseClicked,
                out _exportsHeaderGo,
                out _exportsHeaderRect,
                out _exportsHeaderText,
                out _exportsAlertText
            );
            CreateSectionHeader(
                "ImportsHeader",
                OnToggleImportsCollapseClicked,
                out _importsHeaderGo,
                out _importsHeaderRect,
                out _importsHeaderText,
                out _importsAlertText
            );

            // Lower-Right Corner Resize Grip
            BuildResizeGrip();
        }

        private void BuildScrollbar()
        {
            _scrollbarGo = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            _scrollbarGo.transform.SetParent(_scrollViewportRect, false);
            RectTransform sbRect = _scrollbarGo.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1f, 0f);
            sbRect.anchorMax = new Vector2(1f, 1f);
            sbRect.pivot = new Vector2(1f, 1f);
            sbRect.anchoredPosition = new Vector2(-1f, -2f);
            sbRect.sizeDelta = new Vector2(ScrollbarWidth, -14f);

            Image trackImg = _scrollbarGo.GetComponent<Image>();
            trackImg.color = new Color(0.03f, 0.05f, 0.09f, 0.85f);
            trackImg.raycastTarget = true;

            GameObject slidingAreaGo = new GameObject("SlidingArea", typeof(RectTransform));
            slidingAreaGo.transform.SetParent(sbRect, false);
            RectTransform slidingRect = slidingAreaGo.GetComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(1f, 1f);
            slidingRect.offsetMax = new Vector2(-1f, -1f);

            GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(slidingRect, false);
            RectTransform handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImg = handleGo.GetComponent<Image>();
            handleImg.color = new Color(0.28f, 0.54f, 0.78f, 0.90f);
            handleImg.raycastTarget = true;

            _verticalScrollbar = _scrollbarGo.GetComponent<Scrollbar>();
            _verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
            _verticalScrollbar.handleRect = handleRect;
            _verticalScrollbar.targetGraphic = handleImg;

            ColorBlock cb = _verticalScrollbar.colors;
            cb.normalColor = new Color(0.28f, 0.54f, 0.78f, 0.90f);
            cb.highlightedColor = new Color(0.40f, 0.70f, 0.95f, 1f);
            cb.pressedColor = new Color(0.20f, 0.42f, 0.64f, 1f);
            _verticalScrollbar.colors = cb;

            _scrollbarGo.SetActive(false);
        }

        private void BuildResizeGrip()
        {
            _resizeGripGo = new GameObject("ResizeGrip", typeof(RectTransform), typeof(Image), typeof(PanelResizeHandler));
            _resizeGripGo.transform.SetParent(_panelRect, false);
            RectTransform gripRect = _resizeGripGo.GetComponent<RectTransform>();
            gripRect.anchorMin = new Vector2(1f, 0f);
            gripRect.anchorMax = new Vector2(1f, 0f);
            gripRect.pivot = new Vector2(1f, 0f);
            gripRect.anchoredPosition = Vector2.zero;
            gripRect.sizeDelta = new Vector2(16f, 16f);

            Image gripBg = _resizeGripGo.GetComponent<Image>();
            gripBg.color = new Color(0f, 0f, 0f, 0.01f);
            gripBg.raycastTarget = true;

            Text gripIcon = CreateText("GripIcon", gripRect, 11, FontStyle.Bold, TextAnchor.LowerRight, new Color(0.55f, 0.82f, 1f, 0.85f));
            gripIcon.rectTransform.anchorMin = Vector2.zero;
            gripIcon.rectTransform.anchorMax = Vector2.one;
            gripIcon.rectTransform.offsetMin = Vector2.zero;
            gripIcon.rectTransform.offsetMax = new Vector2(-1f, 0f);
            gripIcon.text = "◢";

            PanelResizeHandler resizeHandler = _resizeGripGo.GetComponent<PanelResizeHandler>();
            resizeHandler.TargetRect = _panelRect;
            resizeHandler.OnResizeStarted = () =>
            {
                _isResizing = true;
            };
            resizeHandler.OnResizing = (newW, newTotalH) =>
            {
                float desiredViewportH = newTotalH - HeaderHeight;

                // Stage 1: Dragged high up into the header -> collapse whole panel into Tabbed Mode
                if (desiredViewportH <= 10f)
                {
                    if (!_panelCollapsed)
                    {
                        _panelCollapsed = true;
                        _panelCollapseBtnText.text = "▶";
                        if (_optionsMenuOpen)
                        {
                            _optionsMenuOpen = false;
                            _optionsMenuGo.SetActive(false);
                            _optionsBtnText.text = "Filter ▾";
                        }
                        if (_customPickerOpen && _customPickerGo != null)
                        {
                            _customPickerOpen = false;
                            _customPickerGo.SetActive(false);
                        }
                        _forceRefresh = true;
                    }
                    return;
                }

                // Stage 2: Dragged into the section-header band -> uncollapse panel, collapse Exports & Imports
                if (desiredViewportH <= 68f)
                {
                    if (desiredViewportH >= 18f && _panelCollapsed)
                    {
                        _panelCollapsed = false;
                        _panelCollapseBtnText.text = "▼";
                        _forceRefresh = true;
                    }
                    if (!_panelCollapsed && (!_exportsCollapsed || !_importsCollapsed))
                    {
                        _exportsCollapsed = true;
                        _importsCollapsed = true;
                        _forceRefresh = true;
                    }
                    _config.PanelWidth.Value = newW;
                    if (!_panelCollapsed)
                    {
                        ApplyCurrentLayoutSizes(newW, _config.PanelMaxHeight.Value);
                    }
                    return;
                }

                // Stage 3: Dragged down into the list area (with hysteresis at 82px) -> expand menus & resize freely
                if (desiredViewportH >= 82f)
                {
                    if (_panelCollapsed)
                    {
                        _panelCollapsed = false;
                        _panelCollapseBtnText.text = "▼";
                        _forceRefresh = true;
                    }
                    if (_exportsCollapsed && _importsCollapsed)
                    {
                        _exportsCollapsed = false;
                        _importsCollapsed = false;
                        _forceRefresh = true;
                    }
                }

                _config.PanelWidth.Value = newW;
                float clampedViewportH = Mathf.Clamp(desiredViewportH, 82f, 950f);
                _config.PanelMaxHeight.Value = clampedViewportH;
                ApplyCurrentLayoutSizes(newW, clampedViewportH);
            };
            resizeHandler.OnResizeFinished = (newW, newTotalH) =>
            {
                _isResizing = false;
                float savedMaxH = Mathf.Max(120f, _config.PanelMaxHeight.Value);
                _config.SavePanelSize(_config.PanelWidth.Value, savedMaxH);
                _forceRefresh = true;
            };
        }

        private void ApplyCurrentLayoutSizes(float panelWidth, float maxScrollHeight)
        {
            if (_panelCollapsed)
            {
                return;
            }

            bool bothMenusCollapsed = _exportsCollapsed && _importsCollapsed;
            float viewportHeight = (_isResizing && !bothMenusCollapsed)
                ? Mathf.Max(MinScrollHeight, maxScrollHeight)
                : Mathf.Min(maxScrollHeight, _activeContentHeight);
            bool needsScrollbar = _activeContentHeight > viewportHeight + 0.5f;
            if (_scrollbarGo.activeSelf != needsScrollbar)
            {
                _scrollbarGo.SetActive(needsScrollbar);
            }

            float rightInset = needsScrollbar ? -(ScrollbarWidth + 2f) : 0f;
            _scrollContentRect.sizeDelta = new Vector2(rightInset, _activeContentHeight);
            _scrollViewportRect.sizeDelta = new Vector2(0f, viewportHeight);
            _panelRect.sizeDelta = new Vector2(panelWidth, HeaderHeight + viewportHeight + 6f);

            // Resize row fill bars to match the new panel width
            float rowAvailableWidth = panelWidth + rightInset;
            for (int i = 0; i < _rowPool.Count; i++)
            {
                ItemRowView row = _rowPool[i];
                if (row.RootGo.activeSelf && row.FillBarRect != null)
                {
                    // Re-scale proportionally
                    float prevRatio = row.RootRect.rect.width > 10f
                        ? Mathf.Clamp01(row.FillBarRect.sizeDelta.x / row.RootRect.rect.width)
                        : 0f;
                    row.FillBarRect.sizeDelta = new Vector2(prevRatio * rowAvailableWidth, 0f);
                }
            }
        }

        private void BuildOptionsMenu()
        {
            _optionsMenuGo = new GameObject("OptionsMenu", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(PointerHoverTracker));
            _optionsMenuGo.transform.SetParent(_panelRect, false);
            _optionsMenuRect = _optionsMenuGo.GetComponent<RectTransform>();
            _optionsMenuRect.anchorMin = new Vector2(1f, 1f);
            _optionsMenuRect.anchorMax = new Vector2(1f, 1f);
            _optionsMenuRect.pivot = new Vector2(1f, 1f);
            _optionsMenuRect.anchoredPosition = new Vector2(-4f, -30f);
            _optionsMenuRect.sizeDelta = new Vector2(236f, 278f);

            _optionsHoverTracker = _optionsMenuGo.GetComponent<PointerHoverTracker>();

            Image menuBg = _optionsMenuGo.GetComponent<Image>();
            menuBg.color = new Color(0.06f, 0.10f, 0.16f, 0.98f);
            menuBg.raycastTarget = true;

            Outline outline = _optionsMenuGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.32f, 0.68f, 0.95f, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);

            Text headerTitle = CreateText("MenuTitle", _optionsMenuRect, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.97f, 1f, 1f));
            headerTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            headerTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            headerTitle.rectTransform.pivot = new Vector2(0f, 1f);
            headerTitle.rectTransform.anchoredPosition = new Vector2(10f, -8f);
            headerTitle.rectTransform.sizeDelta = new Vector2(-20f, 18f);
            headerTitle.text = "OPTIONS — ITEM FILTER";

            _optionsTargetLabel = CreateText("TargetScopeLabel", _optionsMenuRect, 10, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.60f, 0.78f, 0.92f, 1f));
            _optionsTargetLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _optionsTargetLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _optionsTargetLabel.rectTransform.pivot = new Vector2(0f, 1f);
            _optionsTargetLabel.rectTransform.anchoredPosition = new Vector2(10f, -26f);
            _optionsTargetLabel.rectTransform.sizeDelta = new Vector2(-20f, 16f);
            _optionsTargetLabel.text = "Global Filter (All Stars & Planets)";

            (EItemCategoryFilter cat, string label)[] categories = new[]
            {
                (EItemCategoryFilter.RawResources, "Raw Resources (Ores, Gas)"),
                (EItemCategoryFilter.Materials, "Materials & Ingots"),
                (EItemCategoryFilter.Components, "Components & Parts"),
                (EItemCategoryFilter.Buildings, "Buildings & Structures"),
                (EItemCategoryFilter.ConsumablesAndPods, "Fuel, Warpers & Vessels"),
                (EItemCategoryFilter.Matrices, "Science Matrices")
            };

            float y = -48f;
            for (int i = 0; i < categories.Length; i++)
            {
                EItemCategoryFilter cat = categories[i].cat;
                FilterToggleRow toggleRow = CreateFilterToggleRow(_optionsMenuRect, cat, categories[i].label, y);
                _filterToggles.Add(toggleRow);
                y -= 26f;
            }

            // Thin divider before status/fill filter
            GameObject divGo = new GameObject("StatusFilterDivider", typeof(RectTransform), typeof(Image));
            divGo.transform.SetParent(_optionsMenuRect, false);
            RectTransform divRect = divGo.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0f, 1f);
            divRect.anchorMax = new Vector2(1f, 1f);
            divRect.pivot = new Vector2(0.5f, 1f);
            divRect.anchoredPosition = new Vector2(0f, y - 2f);
            divRect.sizeDelta = new Vector2(-16f, 1f);
            Image divImg = divGo.GetComponent<Image>();
            divImg.color = new Color(0.28f, 0.50f, 0.72f, 0.45f);
            divImg.raycastTarget = false;
            y -= 6f;

            FilterToggleRow exclude100Row = CreateFilterToggleRow(_optionsMenuRect, EItemCategoryFilter.Exclude100Percent, "Exclude ≥90% (Full/Tapped)", y);
            _excludeRowLabelText = exclude100Row.LabelText;
            _excludeRowLabelText.rectTransform.offsetMax = new Vector2(-50f, 0f);
            _filterToggles.Add(exclude100Row);

            RectTransform excludeRowRect = exclude100Row.Btn.GetComponent<RectTransform>();
            Button threshBtn = CreateHeaderButton("BtnExcludeThreshold", excludeRowRect, new Vector2(-3f, -2f), new Vector2(44f, 20f), true, out _excludeThresholdBtnText);
            _excludeThresholdBtnText.text = "≥90%";
            threshBtn.onClick.AddListener(() =>
            {
                int cur = _config.ExcludeFullThresholdPercent.Value;
                int next = cur >= 100 ? 41 : (cur >= 90 ? 100 : 90);
                _config.ExcludeFullThresholdPercent.Value = next;
                if (!_config.GetExclude100PercentForAstro(_lastGalaxySeed, _lastTargetAstroId))
                {
                    _config.ToggleExclude100PercentForAstro(_lastGalaxySeed, _lastTargetAstroId);
                }
                RefreshOptionsMenuState();
                _forceRefresh = true;
            });
            y -= 26f;

            Button resetFiltersBtn = CreateHeaderButton("BtnResetFilters", _optionsMenuRect, new Vector2(-150f, y - 6f), new Vector2(78f, 22f), true, out Text resetFiltersTxt);
            resetFiltersTxt.text = "Reset Filters";
            resetFiltersBtn.onClick.AddListener(() =>
            {
                _config.ResetFilterForAstro(_lastGalaxySeed, _lastTargetAstroId);
                RefreshOptionsMenuState();
                RefreshCustomPickerCells();
                _forceRefresh = true;
            });

            Button customBtn = CreateHeaderButton("BtnCustom", _optionsMenuRect, new Vector2(-78f, y - 6f), new Vector2(66f, 22f), true, out Text customTxt);
            customTxt.text = "Custom";
            customBtn.onClick.AddListener(ToggleCustomItemPicker);

            Button resetPosBtn = CreateHeaderButton("BtnResetPos", _optionsMenuRect, new Vector2(-6f, y - 6f), new Vector2(66f, 22f), true, out Text resetPosTxt);
            resetPosTxt.text = "Reset UI";
            resetPosBtn.onClick.AddListener(() =>
            {
                _config.ResetPanelLayout();
                _panelRect.anchoredPosition = new Vector2(StellogisticsConfig.DefaultPosX, StellogisticsConfig.DefaultPosY);
                PositionCustomPickerBesidePanel();
                _forceRefresh = true;
            });

            _optionsMenuGo.SetActive(false);
        }

        private void BuildCustomItemPicker()
        {
            const int cols = 14;
            const int rows = 8;
            const float cellSize = 38f;
            const float cellGap = 2f;
            const float padX = 10f;
            const float topHeight = 68f;
            const float bottomPad = 10f;

            float gridWidth = cols * cellSize + (cols - 1) * cellGap;
            float gridHeight = rows * cellSize + (rows - 1) * cellGap;
            float winWidth = gridWidth + padX * 2f;
            float winHeight = topHeight + gridHeight + bottomPad;

            _customPickerGo = new GameObject("CustomItemPicker", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(PointerHoverTracker));
            _customPickerGo.transform.SetParent(_panelRect, false);
            _customPickerRect = _customPickerGo.GetComponent<RectTransform>();
            _customPickerRect.anchorMin = new Vector2(1f, 1f);
            _customPickerRect.anchorMax = new Vector2(1f, 1f);
            _customPickerRect.pivot = new Vector2(0f, 1f);
            _customPickerRect.anchoredPosition = new Vector2(8f, 0f);
            _customPickerRect.sizeDelta = new Vector2(winWidth, winHeight);

            _customPickerHoverTracker = _customPickerGo.GetComponent<PointerHoverTracker>();

            Image winBg = _customPickerGo.GetComponent<Image>();
            winBg.color = new Color(0.06f, 0.10f, 0.15f, 0.98f);
            winBg.raycastTarget = true;

            Outline outline = _customPickerGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.32f, 0.68f, 0.95f, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Draggable header bar for the custom picker window
            GameObject headerGo = new GameObject("PickerHeader", typeof(RectTransform), typeof(Image), typeof(PanelDragHandler));
            headerGo.transform.SetParent(_customPickerRect, false);
            RectTransform headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 28f);

            Image headerBg = headerGo.GetComponent<Image>();
            headerBg.color = HeaderBgColor;
            headerBg.raycastTarget = true;

            PanelDragHandler pickerDrag = headerGo.GetComponent<PanelDragHandler>();
            pickerDrag.TargetRect = _customPickerRect;

            Text titleTxt = CreateText("PickerTitle", headerRect, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.97f, 1f, 1f));
            titleTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
            titleTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleTxt.rectTransform.offsetMin = new Vector2(10f, 0f);
            titleTxt.rectTransform.offsetMax = new Vector2(-34f, 0f);
            titleTxt.text = "CUSTOM ITEM & BUILDING FILTER (Click to toggle)";

            Button closeBtn = CreateHeaderButton("BtnClosePicker", headerRect, new Vector2(-4f, -3f), new Vector2(22f, 22f), true, out Text closeTxt);
            closeTxt.text = "✕";
            closeBtn.onClick.AddListener(() =>
            {
                _customPickerOpen = false;
                _customPickerGo.SetActive(false);
            });

            // Tabs row: Items (tab 1) and Buildings (tab 2)
            Button tabItemsBtn = CreateHeaderButton("TabItems", _customPickerRect, new Vector2(10f, -34f), new Vector2(86f, 28f), false, out Text tabItemsTxt);
            tabItemsTxt.text = "Items";
            tabItemsTxt.fontSize = 11;
            _customTabItemsBg = tabItemsBtn.GetComponent<Image>();
            tabItemsBtn.onClick.AddListener(() => SetCustomPickerTab(1));

            Button tabBldgsBtn = CreateHeaderButton("TabBuildings", _customPickerRect, new Vector2(102f, -34f), new Vector2(92f, 28f), false, out Text tabBldgsTxt);
            tabBldgsTxt.text = "Buildings";
            tabBldgsTxt.fontSize = 11;
            _customTabBuildingsBg = tabBldgsBtn.GetComponent<Image>();
            tabBldgsBtn.onClick.AddListener(() => SetCustomPickerTab(2));

            // Vertical separator between category tabs and bulk action buttons
            GameObject sepGo = new GameObject("ToolbarDivider", typeof(RectTransform), typeof(Image));
            sepGo.transform.SetParent(_customPickerRect, false);
            RectTransform sepRect = sepGo.GetComponent<RectTransform>();
            sepRect.anchorMin = new Vector2(0f, 1f);
            sepRect.anchorMax = new Vector2(0f, 1f);
            sepRect.pivot = new Vector2(0.5f, 1f);
            sepRect.anchoredPosition = new Vector2(204f, -37f);
            sepRect.sizeDelta = new Vector2(1f, 22f);
            Image sepImg = sepGo.GetComponent<Image>();
            sepImg.color = new Color(0.32f, 0.56f, 0.78f, 0.55f);
            sepImg.raycastTarget = false;

            // Visually distinct bulk action buttons (smaller height, outlined, green/rose color coding)
            Button pickerCheckAllBtn = CreateHeaderButton("PickerCheckAll", _customPickerRect, new Vector2(215f, -37f), new Vector2(80f, 22f), false, out Text pickerCheckAllTxt);
            pickerCheckAllTxt.text = "✓ Check All";
            pickerCheckAllTxt.color = new Color(0.84f, 1f, 0.92f, 1f);
            Image checkAllBg = pickerCheckAllBtn.GetComponent<Image>();
            checkAllBg.color = new Color(0.10f, 0.28f, 0.22f, 0.95f);
            ColorBlock checkCb = pickerCheckAllBtn.colors;
            checkCb.normalColor = new Color(0.10f, 0.28f, 0.22f, 0.95f);
            checkCb.highlightedColor = new Color(0.16f, 0.42f, 0.34f, 1f);
            checkCb.pressedColor = new Color(0.08f, 0.20f, 0.16f, 1f);
            pickerCheckAllBtn.colors = checkCb;
            Outline checkOutline = pickerCheckAllBtn.gameObject.AddComponent<Outline>();
            checkOutline.effectColor = new Color(0.26f, 0.72f, 0.54f, 0.65f);
            checkOutline.effectDistance = new Vector2(1f, -1f);
            pickerCheckAllBtn.onClick.AddListener(() =>
            {
                _config.SetCustomItemsAllVisible(_lastGalaxySeed, _lastTargetAstroId, true);
                RefreshCustomPickerCells();
                RefreshOptionsMenuState();
                _forceRefresh = true;
            });

            Button pickerUncheckAllBtn = CreateHeaderButton("PickerUncheckAll", _customPickerRect, new Vector2(301f, -37f), new Vector2(90f, 22f), false, out Text pickerUncheckAllTxt);
            pickerUncheckAllTxt.text = "✕ Uncheck All";
            pickerUncheckAllTxt.color = new Color(1f, 0.86f, 0.88f, 1f);
            Image uncheckAllBg = pickerUncheckAllBtn.GetComponent<Image>();
            uncheckAllBg.color = new Color(0.28f, 0.13f, 0.17f, 0.95f);
            ColorBlock uncheckCb = pickerUncheckAllBtn.colors;
            uncheckCb.normalColor = new Color(0.28f, 0.13f, 0.17f, 0.95f);
            uncheckCb.highlightedColor = new Color(0.42f, 0.19f, 0.24f, 1f);
            uncheckCb.pressedColor = new Color(0.20f, 0.10f, 0.12f, 1f);
            pickerUncheckAllBtn.colors = uncheckCb;
            Outline uncheckOutline = pickerUncheckAllBtn.gameObject.AddComponent<Outline>();
            uncheckOutline.effectColor = new Color(0.76f, 0.34f, 0.40f, 0.65f);
            uncheckOutline.effectDistance = new Vector2(1f, -1f);
            pickerUncheckAllBtn.onClick.AddListener(() =>
            {
                _config.SetCustomItemsAllVisible(_lastGalaxySeed, _lastTargetAstroId, false);
                RefreshCustomPickerCells();
                RefreshOptionsMenuState();
                _forceRefresh = true;
            });

            _customPickerHoveredLabel = CreateText("HoveredItemLabel", _customPickerRect, 11, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.80f, 0.92f, 1f, 1f));
            _customPickerHoveredLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _customPickerHoveredLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _customPickerHoveredLabel.rectTransform.pivot = new Vector2(1f, 1f);
            _customPickerHoveredLabel.rectTransform.anchoredPosition = new Vector2(-12f, -34f);
            _customPickerHoveredLabel.rectTransform.sizeDelta = new Vector2(-406f, 28f);
            _customPickerHoveredLabel.text = "Bright = Shown • Dimmed = Hidden";

            // Grid container
            GameObject gridGo = new GameObject("GridContainer", typeof(RectTransform), typeof(Image));
            gridGo.transform.SetParent(_customPickerRect, false);
            RectTransform gridRect = gridGo.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(0f, 1f);
            gridRect.pivot = new Vector2(0f, 1f);
            gridRect.anchoredPosition = new Vector2(padX, -topHeight);
            gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);

            Image gridBg = gridGo.GetComponent<Image>();
            gridBg.color = new Color(0.04f, 0.06f, 0.09f, 0.90f);
            gridBg.raycastTarget = false;

            // Background empty slot cells (14 x 8)
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    GameObject slotBgGo = new GameObject($"Slot_{r}_{c}", typeof(RectTransform), typeof(Image));
                    slotBgGo.transform.SetParent(gridRect, false);
                    RectTransform slotRect = slotBgGo.GetComponent<RectTransform>();
                    slotRect.anchorMin = new Vector2(0f, 1f);
                    slotRect.anchorMax = new Vector2(0f, 1f);
                    slotRect.pivot = new Vector2(0f, 1f);
                    slotRect.anchoredPosition = new Vector2(c * (cellSize + cellGap), -r * (cellSize + cellGap));
                    slotRect.sizeDelta = new Vector2(cellSize, cellSize);

                    Image slotImg = slotBgGo.GetComponent<Image>();
                    slotImg.color = new Color(0.10f, 0.14f, 0.20f, 0.55f);
                    slotImg.raycastTarget = false;
                }
            }

            // Populate all items & buildings from LDB.items.dataArray (independent of tech unlock state)
            _customPickerCells.Clear();
            ItemProto[] allProtos = LDB.items != null ? LDB.items.dataArray : null;
            if (allProtos != null)
            {
                for (int i = 0; i < allProtos.Length; i++)
                {
                    ItemProto proto = allProtos[i];
                    if (proto == null || proto.ID <= 0 || proto.GridIndex <= 0)
                    {
                        continue;
                    }

                    int rawTab = proto.GridIndex / 1000;
                    int row = ((proto.GridIndex % 1000) / 100) - 1;
                    int col = (proto.GridIndex % 100) - 1;
                    if (rawTab < 1 || row < 0 || row >= rows || col < 0 || col >= cols)
                    {
                        continue;
                    }

                    int tab = rawTab >= 2 ? 2 : 1;
                    int itemId = proto.ID;
                    string itemName = !string.IsNullOrEmpty(proto.name) ? proto.name : $"Item #{itemId}";
                    EItemCategoryFilter itemCategory = CombinedLogisticsItem.ClassifyItem(proto);

                    GameObject cellGo = new GameObject($"ItemCell_{itemId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(ItemCellHoverHandler));
                    cellGo.transform.SetParent(gridRect, false);
                    RectTransform cellRect = cellGo.GetComponent<RectTransform>();
                    cellRect.anchorMin = new Vector2(0f, 1f);
                    cellRect.anchorMax = new Vector2(0f, 1f);
                    cellRect.pivot = new Vector2(0f, 1f);
                    cellRect.anchoredPosition = new Vector2(col * (cellSize + cellGap), -row * (cellSize + cellGap));
                    cellRect.sizeDelta = new Vector2(cellSize, cellSize);

                    Image cellBg = cellGo.GetComponent<Image>();
                    cellBg.color = new Color(0.14f, 0.24f, 0.36f, 0.90f);

                    GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconGo.transform.SetParent(cellRect, false);
                    RectTransform iconRect = iconGo.GetComponent<RectTransform>();
                    iconRect.anchorMin = Vector2.zero;
                    iconRect.anchorMax = Vector2.one;
                    iconRect.offsetMin = new Vector2(3f, 3f);
                    iconRect.offsetMax = new Vector2(-3f, -3f);

                    Image iconImg = iconGo.GetComponent<Image>();
                    iconImg.sprite = proto.iconSprite;
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;

                    Button btn = cellGo.GetComponent<Button>();
                    btn.onClick.AddListener(() => OnCustomPickerItemClicked(itemId, itemName, itemCategory));

                    ItemCellHoverHandler hover = cellGo.GetComponent<ItemCellHoverHandler>();
                    hover.OnHoverEnter = () =>
                    {
                        if (_customPickerHoveredLabel == null)
                        {
                            return;
                        }
                        bool vis = _config.IsItemVisibleForAstro(_lastGalaxySeed, _lastTargetAstroId, itemId, itemCategory);
                        _customPickerHoveredLabel.text = vis ? $"{itemName} (Shown)" : $"{itemName} (Hidden)";
                        _customPickerHoveredLabel.color = vis ? HealthyColor : WarningColor;
                    };
                    hover.OnHoverExit = () =>
                    {
                        if (_customPickerHoveredLabel == null)
                        {
                            return;
                        }
                        _customPickerHoveredLabel.text = "Bright = Shown • Dimmed = Hidden";
                        _customPickerHoveredLabel.color = new Color(0.80f, 0.92f, 1f, 1f);
                    };

                    _customPickerCells.Add(new CustomPickerCell
                    {
                        ItemId = itemId,
                        TabIndex = tab,
                        ItemName = itemName,
                        Category = itemCategory,
                        CellGo = cellGo,
                        BgImage = cellBg,
                        IconImage = iconImg
                    });
                }
            }

            _customPickerGo.SetActive(false);
        }

        private void ToggleCustomItemPicker()
        {
            if (_customPickerGo == null)
            {
                return;
            }
            _customPickerOpen = !_customPickerOpen;
            _customPickerGo.SetActive(_customPickerOpen);
            if (_customPickerOpen)
            {
                PositionCustomPickerBesidePanel();
                _customPickerRect.SetAsLastSibling();
                SetCustomPickerTab(_customPickerActiveTab);
            }
        }

        private void PositionCustomPickerBesidePanel()
        {
            if (_customPickerRect == null || _panelRect == null)
            {
                return;
            }

            RectTransform parentRect = _panelRect.parent as RectTransform;
            float screenWidth = (parentRect != null && parentRect.rect.width > 100f)
                ? parentRect.rect.width
                : Screen.width;

            float panelRightEdge = _panelRect.anchoredPosition.x + _panelRect.rect.width;
            float pickerWidth = _customPickerRect.rect.width > 10f ? _customPickerRect.rect.width : 578f;
            float spaceOnRight = screenWidth - panelRightEdge;

            if (spaceOnRight < pickerWidth + 12f)
            {
                // Not enough space on the right side of the panel -> open on the left side
                _customPickerRect.anchorMin = new Vector2(0f, 1f);
                _customPickerRect.anchorMax = new Vector2(0f, 1f);
                _customPickerRect.pivot = new Vector2(1f, 1f);
                _customPickerRect.anchoredPosition = new Vector2(-8f, 0f);
            }
            else
            {
                // Enough space on the right side -> open on the right side
                _customPickerRect.anchorMin = new Vector2(1f, 1f);
                _customPickerRect.anchorMax = new Vector2(1f, 1f);
                _customPickerRect.pivot = new Vector2(0f, 1f);
                _customPickerRect.anchoredPosition = new Vector2(8f, 0f);
            }
        }

        private void SetCustomPickerTab(int tab)
        {
            _customPickerActiveTab = tab;
            if (_customTabItemsBg != null)
            {
                _customTabItemsBg.color = (tab == 1)
                    ? new Color(0.24f, 0.44f, 0.66f, 1f)
                    : new Color(0.12f, 0.18f, 0.28f, 0.85f);
            }
            if (_customTabBuildingsBg != null)
            {
                _customTabBuildingsBg.color = (tab == 2)
                    ? new Color(0.24f, 0.44f, 0.66f, 1f)
                    : new Color(0.12f, 0.18f, 0.28f, 0.85f);
            }
            RefreshCustomPickerCells();
        }

        private void OnCustomPickerItemClicked(int itemId, string itemName, EItemCategoryFilter itemCategory)
        {
            _config.ToggleItemOverrideForAstro(_lastGalaxySeed, _lastTargetAstroId, itemId);
            bool nowVisible = _config.IsItemVisibleForAstro(_lastGalaxySeed, _lastTargetAstroId, itemId, itemCategory);
            if (_customPickerHoveredLabel != null)
            {
                _customPickerHoveredLabel.text = nowVisible ? $"{itemName} (Shown)" : $"{itemName} (Hidden)";
                _customPickerHoveredLabel.color = nowVisible ? HealthyColor : WarningColor;
            }
            RefreshCustomPickerCells();
            RefreshOptionsMenuState();
            _forceRefresh = true;
        }

        private void RefreshCustomPickerCells()
        {
            if (!_customPickerOpen || _customPickerGo == null)
            {
                return;
            }

            LogisticsAggregator.CollectStoredItemIds(_storedItemsSet);

            for (int i = 0; i < _customPickerCells.Count; i++)
            {
                CustomPickerCell cell = _customPickerCells[i];
                bool shouldShowCell = cell.TabIndex == _customPickerActiveTab && _storedItemsSet.Contains(cell.ItemId);
                if (cell.CellGo.activeSelf != shouldShowCell)
                {
                    cell.CellGo.SetActive(shouldShowCell);
                }
                if (!shouldShowCell)
                {
                    continue;
                }

                bool visible = _config.IsItemVisibleForAstro(_lastGalaxySeed, _lastTargetAstroId, cell.ItemId, cell.Category);
                cell.BgImage.color = visible
                    ? new Color(0.14f, 0.26f, 0.38f, 0.92f)
                    : new Color(0.06f, 0.08f, 0.11f, 0.70f);
                cell.IconImage.color = visible
                    ? Color.white
                    : new Color(0.32f, 0.35f, 0.40f, 0.28f);
            }
        }

        private FilterToggleRow CreateFilterToggleRow(RectTransform parent, EItemCategoryFilter category, string label, float posY)
        {
            GameObject rowGo = new GameObject($"Toggle_{category}", typeof(RectTransform), typeof(Image), typeof(Button));
            rowGo.transform.SetParent(parent, false);
            RectTransform rect = rowGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, posY);
            rect.sizeDelta = new Vector2(-12f, 24f);

            Image rowBg = rowGo.GetComponent<Image>();
            rowBg.color = new Color(0.10f, 0.16f, 0.24f, 0.65f);

            Button btn = rowGo.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.10f, 0.16f, 0.24f, 0.65f);
            cb.highlightedColor = new Color(0.18f, 0.28f, 0.42f, 0.95f);
            cb.pressedColor = new Color(0.08f, 0.13f, 0.20f, 1f);
            btn.colors = cb;

            GameObject boxGo = new GameObject("CheckBox", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(rect, false);
            RectTransform boxRect = boxGo.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0f, 0.5f);
            boxRect.anchoredPosition = new Vector2(6f, 0f);
            boxRect.sizeDelta = new Vector2(16f, 16f);
            Image boxImg = boxGo.GetComponent<Image>();
            boxImg.color = new Color(0.18f, 0.34f, 0.52f, 1f);
            boxImg.raycastTarget = false;

            Text checkTxt = CreateText("CheckMark", boxRect, 11, FontStyle.Bold, TextAnchor.MiddleCenter, HealthyColor);
            checkTxt.rectTransform.anchorMin = Vector2.zero;
            checkTxt.rectTransform.anchorMax = Vector2.one;
            checkTxt.rectTransform.sizeDelta = Vector2.zero;
            checkTxt.text = "✓";

            Text lblTxt = CreateText("Label", rect, 11, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.90f, 0.95f, 1f, 1f));
            lblTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
            lblTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            lblTxt.rectTransform.offsetMin = new Vector2(28f, 0f);
            lblTxt.rectTransform.offsetMax = new Vector2(-4f, 0f);
            lblTxt.text = label;

            btn.onClick.AddListener(() => OnCategoryToggleClicked(category));

            return new FilterToggleRow
            {
                Category = category,
                Btn = btn,
                CheckBoxBg = boxImg,
                CheckMarkText = checkTxt,
                LabelText = lblTxt
            };
        }

        private Button CreateHeaderButton(
            string name,
            RectTransform parent,
            Vector2 anchoredPos,
            Vector2 size,
            bool anchorRight,
            out Text btnText)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            RectTransform rect = btnGo.GetComponent<RectTransform>();
            float ax = anchorRight ? 1f : 0f;
            rect.anchorMin = new Vector2(ax, 1f);
            rect.anchorMax = new Vector2(ax, 1f);
            rect.pivot = new Vector2(ax, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image bg = btnGo.GetComponent<Image>();
            bg.color = new Color(0.16f, 0.28f, 0.42f, 0.95f);

            Button btn = btnGo.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.16f, 0.28f, 0.42f, 0.95f);
            cb.highlightedColor = new Color(0.24f, 0.42f, 0.62f, 1f);
            cb.pressedColor = new Color(0.12f, 0.20f, 0.30f, 1f);
            btn.colors = cb;

            btnText = CreateText("Text", rect, 10, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.88f, 0.96f, 1f, 1f));
            btnText.rectTransform.anchorMin = Vector2.zero;
            btnText.rectTransform.anchorMax = Vector2.one;
            btnText.rectTransform.sizeDelta = Vector2.zero;

            return btn;
        }

        private void CreateSectionHeader(
            string name,
            UnityEngine.Events.UnityAction onClickAction,
            out GameObject headerGo,
            out RectTransform headerRect,
            out Text titleText,
            out Text alertText)
        {
            headerGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            headerGo.transform.SetParent(_scrollContentRect, false);
            headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, SectionHeaderHeight);

            Image bg = headerGo.GetComponent<Image>();
            bg.color = SectionHeaderBgColor;
            bg.raycastTarget = true;

            Button btn = headerGo.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = SectionHeaderBgColor;
            cb.highlightedColor = SectionHeaderHoverColor;
            cb.pressedColor = new Color(0.09f, 0.14f, 0.22f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(onClickAction);

            titleText = CreateText("Title", headerRect, 11, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.82f, 0.92f, 1f, 1f));
            titleText.rectTransform.anchorMin = new Vector2(0f, 0f);
            titleText.rectTransform.anchorMax = new Vector2(0.65f, 1f);
            titleText.rectTransform.offsetMin = new Vector2(10f, 0f);
            titleText.rectTransform.offsetMax = Vector2.zero;

            alertText = CreateText("Alert", headerRect, 10, FontStyle.Bold, TextAnchor.MiddleRight, CriticalColor);
            alertText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            alertText.rectTransform.anchorMax = new Vector2(1f, 1f);
            alertText.rectTransform.offsetMin = Vector2.zero;
            alertText.rectTransform.offsetMax = new Vector2(-10f, 0f);
        }

        private ItemRowView GetOrCreateRow(int index)
        {
            while (_rowPool.Count <= index)
            {
                _rowPool.Add(CreateRowWidget(_rowPool.Count));
            }
            ItemRowView row = _rowPool[index];
            if (!row.RootGo.activeSelf)
            {
                row.RootGo.SetActive(true);
            }
            return row;
        }

        private ItemRowView CreateRowWidget(int index)
        {
            GameObject rowGo = new GameObject($"ItemRow_{index}", typeof(RectTransform), typeof(Image));
            rowGo.transform.SetParent(_scrollContentRect, false);
            RectTransform rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(0f, RowHeight);

            Image bgImg = rowGo.GetComponent<Image>();
            bgImg.color = (index % 2 == 0) ? RowEvenBgColor : RowOddBgColor;

            GameObject fillGo = new GameObject("FillBar", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(rowRect, false);
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(0f, 0f);
            Image fillImg = fillGo.GetComponent<Image>();
            fillImg.color = HealthyFillColor;
            fillImg.raycastTarget = false;

            GameObject stripeGo = new GameObject("StatusStripe", typeof(RectTransform), typeof(Image));
            stripeGo.transform.SetParent(rowRect, false);
            RectTransform stripeRect = stripeGo.GetComponent<RectTransform>();
            stripeRect.anchorMin = new Vector2(0f, 0f);
            stripeRect.anchorMax = new Vector2(0f, 1f);
            stripeRect.pivot = new Vector2(0f, 0.5f);
            stripeRect.anchoredPosition = Vector2.zero;
            stripeRect.sizeDelta = new Vector2(4f, 0f);
            Image stripeImg = stripeGo.GetComponent<Image>();
            stripeImg.color = HealthyColor;
            stripeImg.raycastTarget = false;

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rowRect, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(10f, 0f);
            iconRect.sizeDelta = new Vector2(28f, 28f);
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            Text nameText = CreateText("NameText", rowRect, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.95f, 0.98f, 1f, 1f));
            nameText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            nameText.rectTransform.anchorMax = new Vector2(0.64f, 1f);
            nameText.rectTransform.offsetMin = new Vector2(44f, 0f);
            nameText.rectTransform.offsetMax = new Vector2(-4f, -1f);

            Text metaText = CreateText("MetaText", rowRect, 10, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.62f, 0.76f, 0.88f, 0.95f));
            metaText.rectTransform.anchorMin = new Vector2(0f, 0f);
            metaText.rectTransform.anchorMax = new Vector2(0.64f, 0.5f);
            metaText.rectTransform.offsetMin = new Vector2(44f, 1f);
            metaText.rectTransform.offsetMax = new Vector2(-4f, 0f);

            Text amountText = CreateText("AmountText", rowRect, 12, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);
            amountText.rectTransform.anchorMin = new Vector2(0.55f, 0.5f);
            amountText.rectTransform.anchorMax = new Vector2(1f, 1f);
            amountText.rectTransform.offsetMin = new Vector2(0f, 0f);
            amountText.rectTransform.offsetMax = new Vector2(-10f, -1f);

            Text subStatusText = CreateText("SubStatusText", rowRect, 10, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.7f, 0.82f, 0.92f, 1f));
            subStatusText.rectTransform.anchorMin = new Vector2(0.55f, 0f);
            subStatusText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            subStatusText.rectTransform.offsetMin = new Vector2(0f, 1f);
            subStatusText.rectTransform.offsetMax = new Vector2(-10f, 0f);

            return new ItemRowView
            {
                RootGo = rowGo,
                RootRect = rowRect,
                BgImage = bgImg,
                FillBarRect = fillRect,
                FillBarImage = fillImg,
                StatusStripeImage = stripeImg,
                IconImage = iconImg,
                NameText = nameText,
                MetaText = metaText,
                AmountText = amountText,
                SubStatusText = subStatusText
            };
        }

        private Text CreateText(string name, RectTransform parent, int fontSize, FontStyle style, TextAnchor align, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text txt = go.GetComponent<Text>();
            txt.font = _gameFont;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        private void OnTogglePanelCollapseClicked()
        {
            _panelCollapsed = !_panelCollapsed;
            _panelCollapseBtnText.text = _panelCollapsed ? "▶" : "▼";
            if (_panelCollapsed)
            {
                if (_optionsMenuOpen)
                {
                    _optionsMenuOpen = false;
                    _optionsMenuGo.SetActive(false);
                    _optionsBtnText.text = "Filter ▾";
                }
                if (_customPickerOpen && _customPickerGo != null)
                {
                    _customPickerOpen = false;
                    _customPickerGo.SetActive(false);
                }
            }
            _forceRefresh = true;
        }

        private void OnToggleExportsCollapseClicked()
        {
            _exportsCollapsed = !_exportsCollapsed;
            _forceRefresh = true;
        }

        private void OnToggleImportsCollapseClicked()
        {
            _importsCollapsed = !_importsCollapsed;
            _forceRefresh = true;
        }

        private void OnFilterModeButtonClicked()
        {
            int next = ((int)_config.StarSystemFilterMode.Value + 1) % 3;
            _config.StarSystemFilterMode.Value = (EStarFilterMode)next;
            _forceRefresh = true;
        }

        private void OnSortModeButtonClicked()
        {
            int next = ((int)_config.SortOrder.Value + 1) % 3;
            _config.SortOrder.Value = (ESortOrder)next;
            _forceRefresh = true;
        }

        private void OnOptionsButtonClicked()
        {
            _optionsMenuOpen = !_optionsMenuOpen;
            if (_optionsMenuOpen && _panelCollapsed)
            {
                _panelCollapsed = false;
                _panelCollapseBtnText.text = "▼";
            }
            _optionsMenuGo.SetActive(_optionsMenuOpen);
            _optionsBtnText.text = _optionsMenuOpen ? "Filter ▴" : "Filter ▾";
            if (_optionsMenuOpen)
            {
                _optionsMenuRect.SetAsLastSibling();
                RefreshOptionsMenuState();
            }
        }

        private void OnCategoryToggleClicked(EItemCategoryFilter category)
        {
            if (category == EItemCategoryFilter.Exclude100Percent)
            {
                _config.ToggleExclude100PercentForAstro(_lastGalaxySeed, _lastTargetAstroId);
                RefreshOptionsMenuState();
                _forceRefresh = true;
                return;
            }

            EItemCategoryFilter current = _config.GetFilterForAstro(_lastGalaxySeed, _lastTargetAstroId);
            EItemCategoryFilter updated = (current & category) != 0
                ? (current & ~category)
                : (current | category);

            SetCurrentTargetCategoryFilter(updated, category);
        }

        private void SetCurrentTargetCategoryFilter(EItemCategoryFilter mask, EItemCategoryFilter clearedOverrideCategories = EItemCategoryFilter.None)
        {
            _config.SetFilterForAstro(_lastGalaxySeed, _lastTargetAstroId, mask, clearedOverrideCategories);
            RefreshOptionsMenuState();
            RefreshCustomPickerCells();
            _forceRefresh = true;
        }

        private void RefreshOptionsMenuState()
        {
            if (!_optionsMenuOpen)
            {
                return;
            }

            EItemCategoryFilter mask = _config.GetFilterForAstro(_lastGalaxySeed, _lastTargetAstroId);
            bool hasCustom = _config.HasCustomFilterForAstro(_lastGalaxySeed, _lastTargetAstroId);
            _optionsTargetLabel.text = hasCustom
                ? "Global Filter (Active across Starmap)"
                : "Global Filter (All Shown)";

            int thresh = _config.ExcludeFullThresholdPercent.Value;
            if (_excludeRowLabelText != null && _excludeThresholdBtnText != null)
            {
                if (thresh >= 100)
                {
                    _excludeRowLabelText.text = "Exclude 100% Entries";
                    _excludeThresholdBtnText.text = "100%";
                }
                else if (thresh >= 90)
                {
                    _excludeRowLabelText.text = "Exclude ≥90% (Tapped)";
                    _excludeThresholdBtnText.text = "≥90%";
                }
                else
                {
                    _excludeRowLabelText.text = "Exclude >40% (Healthy)";
                    _excludeThresholdBtnText.text = ">40%";
                }
            }

            for (int i = 0; i < _filterToggles.Count; i++)
            {
                FilterToggleRow row = _filterToggles[i];
                bool enabled = (mask & row.Category) != 0;
                row.CheckMarkText.text = enabled ? "✓" : "";
                row.CheckBoxBg.color = enabled
                    ? new Color(0.18f, 0.42f, 0.62f, 1f)
                    : new Color(0.12f, 0.16f, 0.22f, 1f);
                row.LabelText.color = enabled
                    ? new Color(0.92f, 0.96f, 1f, 1f)
                    : new Color(0.50f, 0.58f, 0.68f, 0.85f);
            }
        }

        public void RequestImmediateRefresh()
        {
            _forceRefresh = true;
        }

        public void OnStarmapUpdate(UIStarmap starmap)
        {
            if (_config == null || !_config.EnableMod.Value || starmap == null || !starmap.active)
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            ResolveTarget(starmap, out PlanetData targetPlanet, out StarData targetStar);

            if (targetPlanet == null && targetStar == null)
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
                _lastTargetAstroId = -1;
                _lastResolvedPlanet = null;
                _lastResolvedStar = null;
                return;
            }

            _lastResolvedPlanet = targetPlanet;
            _lastResolvedStar = targetStar;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            UpdatePanelPosition();

            if (IsPointerOverUI())
            {
                VFInput.inScrollView = true;
            }

            int currentAstroId = targetPlanet != null ? targetPlanet.astroId : targetStar.astroId;
            if (currentAstroId != _lastTargetAstroId)
            {
                _lastTargetAstroId = currentAstroId;
                _lastGalaxySeed = GameMain.galaxy != null ? GameMain.galaxy.seed : 0;
                _lastTargetDisplayName = targetPlanet != null ? targetPlanet.displayName : $"{targetStar.displayName} System";
                _forceRefresh = true;
                if (_scrollRect != null)
                {
                    _scrollRect.verticalNormalizedPosition = 1f;
                }
                if (_optionsMenuOpen)
                {
                    RefreshOptionsMenuState();
                }
                if (_customPickerOpen)
                {
                    RefreshCustomPickerCells();
                }
            }

            float now = Time.unscaledTime;
            if (_forceRefresh || now >= _nextRefreshTime)
            {
                _forceRefresh = false;
                _nextRefreshTime = now + 0.25f;

                LogisticsSummary summary = targetPlanet != null
                    ? LogisticsAggregator.AggregatePlanet(targetPlanet, _config)
                    : LogisticsAggregator.AggregateStar(targetStar, _config);

                RenderSummary(summary);
            }
        }

        private void ResolveTarget(UIStarmap starmap, out PlanetData targetPlanet, out StarData targetStar)
        {
            targetPlanet = null;
            targetStar = null;

            if (_uiGame != null)
            {
                if (_uiGame.planetDetail != null && _uiGame.planetDetail.active && _uiGame.planetDetail.planet != null)
                {
                    targetPlanet = _uiGame.planetDetail.planet;
                    return;
                }
                if (_uiGame.starDetail != null && _uiGame.starDetail.active && _uiGame.starDetail.star != null)
                {
                    targetStar = _uiGame.starDetail.star;
                    return;
                }
            }

            if (starmap.focusPlanet != null && starmap.focusPlanet.planet != null)
            {
                targetPlanet = starmap.focusPlanet.planet;
                return;
            }
            if (starmap.focusStar != null && starmap.focusStar.star != null)
            {
                targetStar = starmap.focusStar.star;
                return;
            }

            // Do not switch target based on background 3D hover raycasts while the cursor is interacting with our UI
            if (IsPointerOverUI() && (_lastResolvedPlanet != null || _lastResolvedStar != null))
            {
                targetPlanet = _lastResolvedPlanet;
                targetStar = _lastResolvedStar;
                return;
            }

            if (_config.ShowOnHover.Value)
            {
                if (starmap.mouseHoverPlanet != null && starmap.mouseHoverPlanet.planet != null)
                {
                    targetPlanet = starmap.mouseHoverPlanet.planet;
                    return;
                }
                if (starmap.mouseHoverStar != null && starmap.mouseHoverStar.star != null)
                {
                    targetStar = starmap.mouseHoverStar.star;
                    return;
                }
            }

            if (starmap.viewPlanet != null)
            {
                targetPlanet = starmap.viewPlanet;
                return;
            }
            if (starmap.viewStarSystem != null)
            {
                targetStar = starmap.viewStarSystem;
            }
        }

        private void UpdatePanelPosition()
        {
            if (_config.UseCustomPosition.Value)
            {
                return;
            }
            _panelRect.anchoredPosition = new Vector2(StellogisticsConfig.DefaultPosX, StellogisticsConfig.DefaultPosY);
        }

        private void RenderSummary(LogisticsSummary summary)
        {
            // Closed Taskbar Tab State
            if (_panelCollapsed)
            {
                _titleText.text = "Stellogistics";
                _titleText.alignment = TextAnchor.MiddleCenter;
                _titleText.rectTransform.anchoredPosition = new Vector2(30f, -6f);
                _titleText.rectTransform.sizeDelta = new Vector2(-36f, 20f);
                _subtitleText.gameObject.SetActive(false);
                _fleetInfoText.gameObject.SetActive(false);
                _filterModeBtn.gameObject.SetActive(false);
                _sortModeBtn.gameObject.SetActive(false);
                _optionsBtn.gameObject.SetActive(false);
                _scrollRect.gameObject.SetActive(false);
                _resizeGripGo.SetActive(_isResizing);

                _headerRect.sizeDelta = new Vector2(0f, CollapsedTabHeight);
                _panelRect.sizeDelta = new Vector2(CollapsedTabWidth, CollapsedTabHeight);
                return;
            }

            // Expanded State
            _headerRect.sizeDelta = new Vector2(0f, HeaderHeight);
            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.rectTransform.anchoredPosition = new Vector2(34f, -6f);
            _titleText.rectTransform.sizeDelta = new Vector2(-190f, 20f);
            _subtitleText.gameObject.SetActive(true);
            _fleetInfoText.gameObject.SetActive(true);
            _sortModeBtn.gameObject.SetActive(true);
            _optionsBtn.gameObject.SetActive(true);
            _resizeGripGo.SetActive(true);

            float panelWidth = _config.PanelWidth.Value;
            float maxScrollHeight = _config.PanelMaxHeight.Value;

            _titleText.text = summary.TargetName;
            _subtitleText.text = summary.TargetSubtitle;

            int totalShips = summary.TotalIdleShips + summary.TotalWorkShips;
            string filterNote = summary.FilteredOutItemCount > 0 ? $"  •  {summary.FilteredOutItemCount} filtered" : "";
            _fleetInfoText.text = $"Vessels: {summary.TotalIdleShips}/{totalShips} idle  •  Warpers: {CombinedLogisticsItem.FormatAmount(summary.TotalWarperCount)}{filterNote}";

            _filterModeBtn.gameObject.SetActive(summary.IsStarSystem);
            if (summary.IsStarSystem)
            {
                _filterModeBtnText.text = _config.StarSystemFilterMode.Value switch
                {
                    EStarFilterMode.InterstellarSmart => "Ext Only",
                    EStarFilterMode.NetOutIntraSystem => "Netted",
                    _ => "All Slots"
                };
            }

            _sortModeBtnText.text = _config.SortOrder.Value switch
            {
                ESortOrder.ShortageFirst => "Sort: Alert",
                ESortOrder.ItemId => "Sort: ID",
                _ => "Sort: Pct ↓"
            };

            if (!_scrollRect.gameObject.activeSelf)
            {
                _scrollRect.gameObject.SetActive(true);
            }

            if (!summary.HasAnySlots)
            {
                _exportsHeaderGo.SetActive(false);
                _importsHeaderGo.SetActive(false);
                HideRowsFrom(0);

                _emptyStateGo.SetActive(true);
                if (summary.FilteredOutItemCount > 0)
                {
                    _emptyStateText.text = $"{summary.FilteredOutItemCount} item(s) hidden by Filter.";
                }
                else
                {
                    _emptyStateText.text = summary.HasAnyStations
                        ? "No matching remote supply/demand slots."
                        : "No Interplanetary Logistics Stations built.";
                }

                _activeContentHeight = MinScrollHeight;
                _scrollbarGo.SetActive(false);
                _scrollContentRect.sizeDelta = new Vector2(0f, _activeContentHeight);
                _scrollViewportRect.sizeDelta = new Vector2(0f, _activeContentHeight);
                _panelRect.sizeDelta = new Vector2(panelWidth, HeaderHeight + _activeContentHeight + 8f);
                return;
            }

            _emptyStateGo.SetActive(false);

            // First pass to compute total content height so we know whether the scrollbar is visible
            float estimatedHeight = 0f;
            if (summary.Exports.Count > 0)
            {
                estimatedHeight += SectionHeaderHeight + RowSpacing;
                if (!_exportsCollapsed)
                {
                    estimatedHeight += summary.Exports.Count * (RowHeight + RowSpacing);
                }
            }
            if (summary.Imports.Count > 0)
            {
                if (estimatedHeight > 0f) estimatedHeight += 4f;
                estimatedHeight += SectionHeaderHeight + RowSpacing;
                if (!_importsCollapsed)
                {
                    estimatedHeight += summary.Imports.Count * (RowHeight + RowSpacing);
                }
            }
            _activeContentHeight = Mathf.Max(MinScrollHeight, estimatedHeight + 4f);
            bool bothMenusCollapsed = _exportsCollapsed && _importsCollapsed;
            float viewportHeight = (_isResizing && !bothMenusCollapsed)
                ? Mathf.Max(MinScrollHeight, maxScrollHeight)
                : Mathf.Min(maxScrollHeight, _activeContentHeight);
            bool needsScrollbar = _activeContentHeight > viewportHeight + 0.5f;
            float rightInset = needsScrollbar ? -(ScrollbarWidth + 2f) : 0f;
            float rowAvailableWidth = panelWidth + rightInset;

            float cursorY = 0f;
            int rowIndex = 0;
            int critPct = _config.CriticalThresholdPercent.Value;
            int warnPct = _config.WarningThresholdPercent.Value;

            if (summary.Exports.Count > 0)
            {
                _exportsHeaderGo.SetActive(true);
                _exportsHeaderRect.anchoredPosition = new Vector2(0f, -cursorY);
                string expArrow = _exportsCollapsed ? "▶" : "▼";
                _exportsHeaderText.text = $"{expArrow} EXPORTS ({summary.Exports.Count})";
                _exportsAlertText.text = FormatAlertSummary(summary.CriticalExportCount, summary.WarningExportCount, "EMPTY", "LOW");
                _exportsAlertText.color = summary.CriticalExportCount > 0 ? CriticalColor : WarningColor;
                cursorY += SectionHeaderHeight + RowSpacing;

                if (!_exportsCollapsed)
                {
                    for (int i = 0; i < summary.Exports.Count; i++)
                    {
                        ItemRowView row = GetOrCreateRow(rowIndex++);
                        row.RootRect.anchoredPosition = new Vector2(0f, -cursorY);
                        BindRow(row, summary.Exports[i], summary.IsStarSystem, rowAvailableWidth, critPct, warnPct);
                        cursorY += RowHeight + RowSpacing;
                    }
                }
            }
            else
            {
                _exportsHeaderGo.SetActive(false);
            }

            if (summary.Imports.Count > 0)
            {
                if (cursorY > 0f)
                {
                    cursorY += 4f;
                }
                _importsHeaderGo.SetActive(true);
                _importsHeaderRect.anchoredPosition = new Vector2(0f, -cursorY);
                string impArrow = _importsCollapsed ? "▶" : "▼";
                _importsHeaderText.text = $"{impArrow} IMPORTS ({summary.Imports.Count})";
                _importsAlertText.text = FormatAlertSummary(summary.CriticalImportCount, summary.WarningImportCount, "STARVED", "LOW");
                _importsAlertText.color = summary.CriticalImportCount > 0 ? CriticalColor : WarningColor;
                cursorY += SectionHeaderHeight + RowSpacing;

                if (!_importsCollapsed)
                {
                    for (int i = 0; i < summary.Imports.Count; i++)
                    {
                        ItemRowView row = GetOrCreateRow(rowIndex++);
                        row.RootRect.anchoredPosition = new Vector2(0f, -cursorY);
                        BindRow(row, summary.Imports[i], summary.IsStarSystem, rowAvailableWidth, critPct, warnPct);
                        cursorY += RowHeight + RowSpacing;
                    }
                }
            }
            else
            {
                _importsHeaderGo.SetActive(false);
            }

            HideRowsFrom(rowIndex);

            _scrollbarGo.SetActive(needsScrollbar);
            _scrollContentRect.sizeDelta = new Vector2(rightInset, _activeContentHeight);
            _scrollViewportRect.sizeDelta = new Vector2(0f, viewportHeight);
            _panelRect.sizeDelta = new Vector2(panelWidth, HeaderHeight + viewportHeight + 6f);
            _resizeGripGo.transform.SetAsLastSibling();
        }

        private static string FormatAlertSummary(int criticalCount, int warningCount, string critLabel, string warnLabel)
        {
            if (criticalCount > 0 && warningCount > 0)
            {
                return $"{criticalCount} {critLabel} • {warningCount} {warnLabel}";
            }
            if (criticalCount > 0)
            {
                return $"! {criticalCount} {critLabel}";
            }
            if (warningCount > 0)
            {
                return $"{warningCount} {warnLabel}";
            }
            return "";
        }

        private void BindRow(
            ItemRowView row,
            CombinedLogisticsItem item,
            bool isStarSystem,
            float rowWidth,
            int critPct,
            int warnPct)
        {
            EHealthStatus status = item.GetHealthStatus(critPct, warnPct);

            Color accentColor;
            Color fillColor;
            switch (status)
            {
                case EHealthStatus.Critical:
                    accentColor = CriticalColor;
                    fillColor = CriticalFillColor;
                    break;
                case EHealthStatus.Warning:
                    accentColor = WarningColor;
                    fillColor = WarningFillColor;
                    break;
                default:
                    accentColor = HealthyColor;
                    fillColor = HealthyFillColor;
                    break;
            }

            row.StatusStripeImage.color = accentColor;
            row.FillBarImage.color = fillColor;
            float fillWidth = Mathf.Clamp01(item.FillRatio) * rowWidth;
            row.FillBarRect.sizeDelta = new Vector2(fillWidth, 0f);

            row.IconImage.sprite = item.Icon;
            row.IconImage.enabled = item.Icon != null;

            string displayName = item.ItemName ?? "";
            int maxChars = Mathf.Max(18, Mathf.RoundToInt((rowWidth - 135f) / 6.8f));
            if (displayName.Length > maxChars)
            {
                displayName = displayName.Substring(0, maxChars - 2) + "..";
            }
            row.NameText.text = displayName;

            int ilsSlots = item.SlotCount - item.CollectorSlotCount;
            string slotDesc;
            if (item.CollectorSlotCount > 0 && ilsSlots > 0)
            {
                slotDesc = $"{ilsSlots} ILS +{item.CollectorSlotCount} OC";
            }
            else if (item.CollectorSlotCount > 0)
            {
                slotDesc = $"{item.CollectorSlotCount} OC";
            }
            else
            {
                slotDesc = $"{item.SlotCount} ILS";
            }

            if (isStarSystem && item.PlanetCount > 1)
            {
                slotDesc = $"{item.PlanetCount} Planets • {slotDesc}";
            }
            row.MetaText.text = slotDesc;

            string storedStr = CombinedLogisticsItem.FormatAmount(item.StoredCount);
            string maxStr = CombinedLogisticsItem.FormatAmount(item.MaxCount);
            row.AmountText.text = $"{storedStr} / {maxStr}";
            row.AmountText.color = status == EHealthStatus.Healthy
                ? new Color(0.92f, 0.99f, 0.94f, 1f)
                : accentColor;

            int pct = item.DisplayPercent;
            string statusTag = "";
            if (status == EHealthStatus.Critical)
            {
                statusTag = item.Logic == ELogisticStorage.Supply
                    ? (item.StoredCount == 0 ? "EMPTY " : "LOW ")
                    : (item.StoredCount == 0 ? "STARVED " : "LOW ");
            }
            else if (status == EHealthStatus.Warning)
            {
                statusTag = "LOW ";
            }

            string transitStr = "";
            if (item.InTransitOrder != 0)
            {
                string sign = item.InTransitOrder > 0 ? "+" : "";
                transitStr = $" ({sign}{CombinedLogisticsItem.FormatAmount(item.InTransitOrder)})";
            }

            row.SubStatusText.text = $"{statusTag}{pct}%{transitStr}";
            row.SubStatusText.color = status == EHealthStatus.Healthy
                ? new Color(0.62f, 0.82f, 0.72f, 0.95f)
                : accentColor;
        }

        private void HideRowsFrom(int startIndex)
        {
            for (int i = startIndex; i < _rowPool.Count; i++)
            {
                if (_rowPool[i].RootGo.activeSelf)
                {
                    _rowPool[i].RootGo.SetActive(false);
                }
            }
        }
    }
}
