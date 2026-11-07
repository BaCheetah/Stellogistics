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
            public Button RowButton;
            public PointerHoverTracker RowHoverTracker;
            public Action OnRowClick;
            public GameObject MuteBtnGo;
            public RectTransform MuteBtnRect;
            public Image MuteBtnBg;
            public Text MuteBtnText;
            public PointerHoverTracker MuteHoverTracker;
            public bool ShowMuteOnHover;
            public float MuteSlideProgress;
            public Action OnMuteClick;
            public RectTransform FillBarRect;
            public Image FillBarImage;
            public float FillRatioCached;
            public RectTransform TransitBarRect;
            public Image TransitBarImage;
            public float TransitRatioCached;
            public Image StatusStripeImage;
            public Image IconImage;
            public Text NameText;
            public Text MetaText;
            public Text AmountText;
            public Text SubStatusText;
        }

        private class PlanetFilterRowView
        {
            public GameObject RootGo;
            public RectTransform RootRect;
            public Image BgImage;
            public Button Btn;
            public Image CheckBoxBg;
            public Text CheckMarkText;
            public Text NameText;
            public Text MetaText;
            public Action OnClick;
        }

        private class GeneralToggleRow
        {
            public Func<bool> Getter;
            public Action Toggler;
            public Button Btn;
            public Image CheckBoxBg;
            public Text CheckMarkText;
            public Text LabelText;
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
            public Outline CellOutline;
        }

        private StellogisticsConfig _config;
        private UIGame _uiGame;
        private Font _gameFont;

        private RectTransform _panelRect;
        private RectTransform _headerRect;
        private Image _panelBg;
        private PointerHoverTracker _panelHoverTracker;
        private PointerHoverTracker _optionsHoverTracker;
        private PointerHoverTracker _generalOptionsHoverTracker;
        private PointerHoverTracker _customPickerHoverTracker;
        private Sprite _hatchedTransitSprite;

        private Button _panelCollapseBtn;
        private Text _panelCollapseBtnText;
        private Text _titleText;
        private Text _subtitleText;
        private Text _fleetInfoText;

        private Button _filterModeBtn;
        private Text _filterModeBtnText;
        private Button _finderModeBtn;
        private Text _finderModeBtnText;
        private Button _sortModeBtn;
        private Text _sortModeBtnText;
        private Button _optionsBtn;
        private Text _optionsBtnText;
        private Button _generalOptionsBtn;
        private Text _generalOptionsBtnText;

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
        private GeneralToggleRow _ignoreTargetToggleRow;
        private readonly List<FilterToggleRow> _filterToggles = new List<FilterToggleRow>();

        private GameObject _generalOptionsMenuGo;
        private RectTransform _generalOptionsMenuRect;
        private readonly List<GeneralToggleRow> _generalOptionToggles = new List<GeneralToggleRow>();
        private Image _refreshFastBtnBg;
        private Image _refreshMedBtnBg;
        private Image _refreshSlowBtnBg;
        private Image _refreshPausedBtnBg;
        private Text _refreshFastBtnTxt;
        private Text _refreshMedBtnTxt;
        private Text _refreshSlowBtnTxt;
        private Text _refreshPausedBtnTxt;

        private GameObject _customPickerGo;
        private RectTransform _customPickerRect;
        private GameObject _customPickerGridGo;
        private GameObject _customPickerPlanetsScrollGo;
        private RectTransform _customPickerPlanetsContentRect;
        private GameObject _customPickerPlanetsEmptyGo;
        private Text _customPickerTitleText;
        private Text _customPickerHoveredLabel;
        private Image _customTabItemsBg;
        private Image _customTabBuildingsBg;
        private Image _customTabPlanetsBg;
        private Text _customTabPlanetsText;
        private Button _pickerCheckAllBtn;
        private Button _pickerUncheckAllBtn;
        private Button _pickerShowAllPlanetsBtn;
        private int _customPickerActiveTab = 1;
        private bool _customPickerOpen = false;
        private readonly List<CustomPickerCell> _customPickerCells = new List<CustomPickerCell>();
        private readonly List<PlanetFilterRowView> _planetFilterRowPool = new List<PlanetFilterRowView>();
        private readonly List<PlanetLogisticsFilterEntry> _planetFilterEntries = new List<PlanetLogisticsFilterEntry>();
        private readonly HashSet<int> _storedItemsSet = new HashSet<int>();
        private readonly HashSet<int> _savedFinderHiddenSnapshot = new HashSet<int>();
        private bool _hasSavedFinderSnapshot = false;
        private int _lastFinderCheckedCount = 0;

        private readonly List<ItemRowView> _rowPool = new List<ItemRowView>();

        private UIStarmap _currentStarmap;
        private bool _finderModeActive = false;

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
        private bool _generalOptionsMenuOpen = false;
        private bool _isResizing = false;

        private static readonly Color PanelBgColor = new Color(0.055f, 0.085f, 0.13f, 0.94f);
        private static readonly Color HeaderBgColor = new Color(0.09f, 0.145f, 0.22f, 0.98f);
        private static readonly Color SectionHeaderBgColor = new Color(0.12f, 0.19f, 0.28f, 0.95f);
        private static readonly Color SectionHeaderHoverColor = new Color(0.17f, 0.27f, 0.39f, 0.98f);
        private static readonly Color RowEvenBgColor = new Color(0.08f, 0.12f, 0.18f, 0.78f);
        private static readonly Color RowOddBgColor = new Color(0.065f, 0.10f, 0.15f, 0.78f);

        private static readonly Color CriticalColor = new Color(1.0f, 0.34f, 0.34f, 1f);
        private static readonly Color CriticalFillColor = new Color(0.85f, 0.18f, 0.18f, 0.24f);
        private static readonly Color CriticalTransitColor = new Color(1.0f, 0.42f, 0.42f, 0.38f);
        private static readonly Color WarningColor = new Color(1.0f, 0.70f, 0.24f, 1f);
        private static readonly Color WarningFillColor = new Color(0.85f, 0.52f, 0.12f, 0.22f);
        private static readonly Color WarningTransitColor = new Color(1.0f, 0.72f, 0.28f, 0.36f);
        private static readonly Color HealthyColor = new Color(0.30f, 0.90f, 0.58f, 1f);
        private static readonly Color HealthyFillColor = new Color(0.18f, 0.68f, 0.42f, 0.18f);
        private static readonly Color HealthyTransitColor = new Color(0.32f, 0.92f, 0.62f, 0.34f);

        private const float HeaderHeight = 76f;
        private const float StackedHeaderHeight = 100f;
        private const float StackedWidthPlanet = 305f;
        private const float StackedWidthFiveBtns = 355f;
        private const float CollapsedTabWidth = 148f;
        private const float CollapsedTabHeight = 36f;
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
            BuildGeneralOptionsMenu();
            BuildCustomItemPicker();
        }

        private Sprite GetOrCreateHatchedTransitSprite()
        {
            if (_hatchedTransitSprite != null)
            {
                return _hatchedTransitSprite;
            }

            const int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Diagonal 45-degree stripes (period 8px: 4px bright stripe, 4px soft gap)
                    int diag = (x + y) & 7;
                    float alpha = diag < 4 ? 1.0f : 0.22f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _hatchedTransitSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f
            );
            return _hatchedTransitSprite;
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
            if (_generalOptionsMenuOpen && _generalOptionsHoverTracker != null && _generalOptionsHoverTracker.IsHovered)
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
            dragHandler.OnDragStarted = () =>
            {
                _config.UseCustomPosition.Value = true;
                CloseDropdownMenus();
            };
            dragHandler.OnDragFinished = pos =>
            {
                _config.SaveCustomPanelPosition(pos.x, pos.y);
                if (_customPickerOpen)
                {
                    PositionCustomPickerBesidePanel();
                }
            };

            // Panel Collapse Button (top-left)
            _panelCollapseBtn = CreateHeaderButton("CollapsePanelBtn", _headerRect, new Vector2(8f, -8f), new Vector2(22f, 20f), false, out _panelCollapseBtnText);
            _panelCollapseBtnText.text = "▼";
            _panelCollapseBtn.onClick.AddListener(OnTogglePanelCollapseClicked);

            // Title Text
            _titleText = CreateText("TitleText", _headerRect, 13, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.94f, 0.97f, 1f, 1f));
            _titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _titleText.rectTransform.pivot = new Vector2(0f, 1f);
            _titleText.rectTransform.anchoredPosition = new Vector2(36f, -8f);
            _titleText.rectTransform.sizeDelta = new Vector2(-314f, 20f);
            _titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _titleText.verticalOverflow = VerticalWrapMode.Truncate;
            _titleText.resizeTextForBestFit = true;
            _titleText.resizeTextMinSize = 10;
            _titleText.resizeTextMaxSize = 13;

            // Top-right Header Buttons with uniform 4px spacing:
            // [-272..-222: Ext Only / Pick Item] [-218..-172: Finder / ← Back] [-168..-112: Sort] [-108..-62: Filter ▾] [-58..-6: Options ▾]
            _filterModeBtn = CreateHeaderButton("FilterModeBtn", _headerRect, new Vector2(-222f, -8f), new Vector2(50f, 20f), true, out _filterModeBtnText);
            _filterModeBtn.onClick.AddListener(OnFilterModeButtonClicked);

            _finderModeBtn = CreateHeaderButton("FinderModeBtn", _headerRect, new Vector2(-172f, -8f), new Vector2(46f, 20f), true, out _finderModeBtnText);
            _finderModeBtnText.text = "Finder";
            _finderModeBtn.onClick.AddListener(OnFinderModeButtonClicked);

            _sortModeBtn = CreateHeaderButton("SortModeBtn", _headerRect, new Vector2(-112f, -8f), new Vector2(56f, 20f), true, out _sortModeBtnText);
            _sortModeBtn.onClick.AddListener(OnSortModeButtonClicked);

            _optionsBtn = CreateHeaderButton("OptionsBtn", _headerRect, new Vector2(-62f, -8f), new Vector2(46f, 20f), true, out _optionsBtnText);
            _optionsBtnText.text = "Filter ▾";
            _optionsBtn.onClick.AddListener(OnOptionsButtonClicked);

            _generalOptionsBtn = CreateHeaderButton("GeneralOptionsBtn", _headerRect, new Vector2(-6f, -8f), new Vector2(52f, 20f), true, out _generalOptionsBtnText);
            _generalOptionsBtnText.text = "Options ▾";
            _generalOptionsBtn.onClick.AddListener(OnGeneralOptionsButtonClicked);

            // Subtitle Text
            _subtitleText = CreateText("SubtitleText", _headerRect, 11, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.68f, 0.82f, 0.94f, 1f));
            _subtitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _subtitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _subtitleText.rectTransform.pivot = new Vector2(0f, 1f);
            _subtitleText.rectTransform.anchoredPosition = new Vector2(10f, -33f);
            _subtitleText.rectTransform.sizeDelta = new Vector2(-20f, 18f);

            // Fleet / Warper Info
            _fleetInfoText = CreateText("FleetInfoText", _headerRect, 11, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.56f, 0.72f, 0.85f, 0.95f));
            _fleetInfoText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _fleetInfoText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _fleetInfoText.rectTransform.pivot = new Vector2(0f, 1f);
            _fleetInfoText.rectTransform.anchoredPosition = new Vector2(10f, -52f);
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
            _scrollRect.verticalScrollbar = null;

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
            sbRect.anchoredPosition = Vector2.zero;
            sbRect.sizeDelta = new Vector2(ScrollbarWidth, -14f);

            Image trackImg = _scrollbarGo.GetComponent<Image>();
            trackImg.color = new Color(0.03f, 0.05f, 0.09f, 0.96f);
            trackImg.raycastTarget = true;

            GameObject slidingAreaGo = new GameObject("SlidingArea", typeof(RectTransform));
            slidingAreaGo.transform.SetParent(sbRect, false);
            RectTransform slidingRect = slidingAreaGo.GetComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(1f, 1f);
            slidingRect.offsetMax = new Vector2(0f, -1f);

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
            gripIcon.rectTransform.offsetMin = new Vector2(0f, -3f);
            gripIcon.rectTransform.offsetMax = new Vector2(-1f, -3f);
            gripIcon.text = "◢";

            PanelResizeHandler resizeHandler = _resizeGripGo.GetComponent<PanelResizeHandler>();
            resizeHandler.TargetRect = _panelRect;
            resizeHandler.OnResizeStarted = () =>
            {
                _isResizing = true;
                CloseDropdownMenus();
            };
            resizeHandler.OnResizing = (newW, newTotalH) =>
            {
                // Use stable base HeaderHeight (74f) for vertical collapse thresholds so crossing the horizontal
                // width threshold (385px) never causes a 22px vertical hysteresis oscillation!
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
                        if (_generalOptionsMenuOpen && _generalOptionsMenuGo != null)
                        {
                            _generalOptionsMenuOpen = false;
                            _generalOptionsMenuGo.SetActive(false);
                            _generalOptionsBtnText.text = "Options ▾";
                        }
                        if (_customPickerOpen && _customPickerGo != null)
                        {
                            _customPickerOpen = false;
                            _customPickerGo.SetActive(false);
                        }
                        _forceRefresh = true;
                    }
                    // Immediately apply collapsed tab geometry so it never flashes a half-collapsed state between frames
                    _titleText.text = "Stellogistics";
                    _titleText.alignment = TextAnchor.MiddleCenter;
                    _titleText.rectTransform.anchoredPosition = new Vector2(32f, -8f);
                    _titleText.rectTransform.sizeDelta = new Vector2(-38f, 20f);
                    _subtitleText.gameObject.SetActive(false);
                    _fleetInfoText.gameObject.SetActive(false);
                    _filterModeBtn.gameObject.SetActive(false);
                    _finderModeBtn.gameObject.SetActive(false);
                    _sortModeBtn.gameObject.SetActive(false);
                    _optionsBtn.gameObject.SetActive(false);
                    _generalOptionsBtn.gameObject.SetActive(false);
                    _scrollRect.gameObject.SetActive(false);
                    _headerRect.sizeDelta = new Vector2(0f, CollapsedTabHeight);
                    _panelRect.sizeDelta = new Vector2(CollapsedTabWidth, CollapsedTabHeight);
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
                bool showFifthBtn = _finderModeActive || (_lastResolvedStar != null && _lastResolvedPlanet == null);
                float activeHeaderH = ApplyHeaderLayout(newW, showFifthBtn);
                float clampedViewportH = Mathf.Clamp(newTotalH - activeHeaderH, 82f, 950f);
                _config.PanelMaxHeight.Value = clampedViewportH;
                ApplyCurrentLayoutSizes(newW, clampedViewportH);
            };
            resizeHandler.OnResizeFinished = (newW, newTotalH) =>
            {
                _isResizing = false;
                float savedMaxH = Mathf.Max(82f, _config.PanelMaxHeight.Value);
                _config.SavePanelSize(_config.PanelWidth.Value, savedMaxH);
                _forceRefresh = true;
            };
        }

        private float ApplyHeaderLayout(float panelWidth, bool showFifthButton)
        {
            if (_panelCollapsed)
            {
                return CollapsedTabHeight;
            }
            float threshold = showFifthButton ? StackedWidthFiveBtns : StackedWidthPlanet;
            bool stacked = panelWidth < threshold;
            float currentHeaderH = stacked ? StackedHeaderHeight : HeaderHeight;

            _headerRect.sizeDelta = new Vector2(0f, currentHeaderH);
            _scrollViewportRect.anchoredPosition = new Vector2(0f, -currentHeaderH);

            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.rectTransform.anchoredPosition = new Vector2(36f, -8f);

            RectTransform filterBtnRect = _filterModeBtn.GetComponent<RectTransform>();
            RectTransform finderBtnRect = _finderModeBtn.GetComponent<RectTransform>();
            RectTransform sortBtnRect = _sortModeBtn.GetComponent<RectTransform>();
            RectTransform optBtnRect = _optionsBtn.GetComponent<RectTransform>();
            RectTransform genOptBtnRect = _generalOptionsBtn.GetComponent<RectTransform>();

            if (stacked)
            {
                // Row 1: [▼] + Title + Rightmost buttons ([Filter ▾] [Options ▾])
                _titleText.rectTransform.sizeDelta = new Vector2(-148f, 20f);
                optBtnRect.anchoredPosition = new Vector2(-62f, -8f);
                genOptBtnRect.anchoredPosition = new Vector2(-6f, -8f);

                // Row 2: Action buttons ([Netted / Pick Item] [Finder / ← Back] [Sort]) stacked with a matching 4px vertical gap at y = -32f
                filterBtnRect.anchoredPosition = new Vector2(-116f, -32f);
                finderBtnRect.anchoredPosition = new Vector2(-66f, -32f);
                sortBtnRect.anchoredPosition = new Vector2(-6f, -32f);

                _subtitleText.rectTransform.anchoredPosition = new Vector2(10f, -57f);
                _fleetInfoText.rectTransform.anchoredPosition = new Vector2(10f, -76f);

                if (_optionsMenuRect != null) _optionsMenuRect.anchoredPosition = new Vector2(-4f, -32f);
                if (_generalOptionsMenuRect != null) _generalOptionsMenuRect.anchoredPosition = new Vector2(-4f, -32f);
            }
            else
            {
                // Single top row: [▼] + Title on left, Buttons on right
                _titleText.rectTransform.sizeDelta = new Vector2(showFifthButton ? -314f : -260f, 20f);

                filterBtnRect.anchoredPosition = new Vector2(-222f, -8f);
                finderBtnRect.anchoredPosition = new Vector2(-172f, -8f);
                sortBtnRect.anchoredPosition = new Vector2(-112f, -8f);
                optBtnRect.anchoredPosition = new Vector2(-62f, -8f);
                genOptBtnRect.anchoredPosition = new Vector2(-6f, -8f);

                _subtitleText.rectTransform.anchoredPosition = new Vector2(10f, -33f);
                _fleetInfoText.rectTransform.anchoredPosition = new Vector2(10f, -52f);

                if (_optionsMenuRect != null) _optionsMenuRect.anchoredPosition = new Vector2(-4f, -32f);
                if (_generalOptionsMenuRect != null) _generalOptionsMenuRect.anchoredPosition = new Vector2(-4f, -32f);
            }

            return currentHeaderH;
        }

        private void ApplyCurrentLayoutSizes(float panelWidth, float maxScrollHeight)
        {
            if (_panelCollapsed)
            {
                return;
            }

            bool showFifthBtn = _finderModeActive || (_lastResolvedStar != null && _lastResolvedPlanet == null);
            float activeHeaderH = ApplyHeaderLayout(panelWidth, showFifthBtn);

            bool bothMenusCollapsed = _exportsCollapsed && _importsCollapsed;
            float viewportHeight = bothMenusCollapsed
                ? Mathf.Min(maxScrollHeight, _activeContentHeight)
                : Mathf.Max(MinScrollHeight, maxScrollHeight);
            bool needsScrollbar = _activeContentHeight > viewportHeight + 0.5f;
            SetScrollbarActive(needsScrollbar);

            float rightInset = needsScrollbar ? -(ScrollbarWidth + 1f) : 0f;
            _scrollContentRect.anchoredPosition = new Vector2(0f, _scrollContentRect.anchoredPosition.y);
            _scrollContentRect.sizeDelta = new Vector2(rightInset, _activeContentHeight);
            _scrollViewportRect.sizeDelta = new Vector2(0f, viewportHeight);
            _panelRect.sizeDelta = new Vector2(panelWidth, activeHeaderH + viewportHeight);
        }

        private void SetScrollbarActive(bool active)
        {
            if (_scrollRect != null)
            {
                Scrollbar targetSb = active ? _verticalScrollbar : null;
                if (_scrollRect.verticalScrollbar != targetSb)
                {
                    _scrollRect.verticalScrollbar = targetSb;
                }
            }
            if (_scrollbarGo != null && _scrollbarGo.activeSelf != active)
            {
                _scrollbarGo.SetActive(active);
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
            _optionsMenuRect.sizeDelta = new Vector2(236f, 304f);

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
            headerTitle.text = "ITEM FILTER";

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

            _ignoreTargetToggleRow = CreateGeneralOptionToggleRow(
                _optionsMenuRect,
                "Ignore Target in Finder",
                y,
                () => _lastTargetAstroId > 0 && _config.IsAstroIgnored(_lastGalaxySeed, _lastTargetAstroId),
                () =>
                {
                    if (_lastTargetAstroId > 0)
                    {
                        _config.ToggleAstroIgnored(_lastGalaxySeed, _lastTargetAstroId);
                        RefreshOptionsMenuState();
                        RefreshCustomPickerPlanetsList();
                        _forceRefresh = true;
                    }
                }
            );
            y -= 26f;

            Button resetFiltersBtn = CreateHeaderButton("BtnResetFilters", _optionsMenuRect, new Vector2(-120f, y - 6f), new Vector2(108f, 22f), true, out Text resetFiltersTxt);
            resetFiltersTxt.text = "Reset Filters";
            resetFiltersBtn.onClick.AddListener(() =>
            {
                _config.ResetFilterForAstro(_lastGalaxySeed, _lastTargetAstroId);
                RefreshOptionsMenuState();
                RefreshCustomPickerCells();
                _forceRefresh = true;
            });

            Button customBtn = CreateHeaderButton("BtnCustom", _optionsMenuRect, new Vector2(-6f, y - 6f), new Vector2(108f, 22f), true, out Text customTxt);
            customTxt.text = "Custom Picker";
            customBtn.onClick.AddListener(ToggleCustomItemPicker);

            _optionsMenuGo.SetActive(false);
        }

        private void BuildGeneralOptionsMenu()
        {
            _generalOptionsMenuGo = new GameObject("GeneralOptionsMenu", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(PointerHoverTracker));
            _generalOptionsMenuGo.transform.SetParent(_panelRect, false);
            _generalOptionsMenuRect = _generalOptionsMenuGo.GetComponent<RectTransform>();
            _generalOptionsMenuRect.anchorMin = new Vector2(1f, 1f);
            _generalOptionsMenuRect.anchorMax = new Vector2(1f, 1f);
            _generalOptionsMenuRect.pivot = new Vector2(1f, 1f);
            _generalOptionsMenuRect.anchoredPosition = new Vector2(-4f, -32f);
            _generalOptionsMenuRect.sizeDelta = new Vector2(236f, 198f);

            _generalOptionsHoverTracker = _generalOptionsMenuGo.GetComponent<PointerHoverTracker>();

            Image menuBg = _generalOptionsMenuGo.GetComponent<Image>();
            menuBg.color = new Color(0.06f, 0.10f, 0.16f, 0.98f);
            menuBg.raycastTarget = true;

            Outline outline = _generalOptionsMenuGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.32f, 0.68f, 0.95f, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);

            Text headerTitle = CreateText("GeneralMenuTitle", _generalOptionsMenuRect, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.97f, 1f, 1f));
            headerTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            headerTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            headerTitle.rectTransform.pivot = new Vector2(0f, 1f);
            headerTitle.rectTransform.anchoredPosition = new Vector2(10f, -8f);
            headerTitle.rectTransform.sizeDelta = new Vector2(-20f, 18f);
            headerTitle.text = "OPTIONS — DISPLAY & UI";

            _generalOptionToggles.Clear();
            float y = -32f;

            _generalOptionToggles.Add(CreateGeneralOptionToggleRow(
                _generalOptionsMenuRect,
                "Show +In-Transit Bar (Hatched)",
                y,
                () => _config.ShowInTransitBar.Value,
                () => _config.ShowInTransitBar.Value = !_config.ShowInTransitBar.Value
            ));
            y -= 26f;

            _generalOptionToggles.Add(CreateGeneralOptionToggleRow(
                _generalOptionsMenuRect,
                "Include Orbital Collectors (OC)",
                y,
                () => _config.IncludeOrbitalCollectors.Value,
                () => _config.IncludeOrbitalCollectors.Value = !_config.IncludeOrbitalCollectors.Value
            ));
            y -= 26f;

            _generalOptionToggles.Add(CreateGeneralOptionToggleRow(
                _generalOptionsMenuRect,
                "Show Panel on Starmap Hover",
                y,
                () => _config.ShowOnHover.Value,
                () => _config.ShowOnHover.Value = !_config.ShowOnHover.Value
            ));
            y -= 28f;

            Text refreshTitle = CreateText("RefreshRateLabel", _generalOptionsMenuRect, 10, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.70f, 0.85f, 0.96f, 1f));
            refreshTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            refreshTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            refreshTitle.rectTransform.pivot = new Vector2(0f, 1f);
            refreshTitle.rectTransform.anchoredPosition = new Vector2(8f, y);
            refreshTitle.rectTransform.sizeDelta = new Vector2(-16f, 15f);
            refreshTitle.text = "REFRESH RATE";
            y -= 17f;

            Button fastBtn = CreateHeaderButton("BtnRefreshFast", _generalOptionsMenuRect, new Vector2(6f, y), new Vector2(53f, 20f), false, out _refreshFastBtnTxt);
            _refreshFastBtnTxt.text = "Fast";
            _refreshFastBtnBg = fastBtn.GetComponent<Image>();
            fastBtn.onClick.AddListener(() => SetRefreshRate(ERefreshRate.Fast));

            Button medBtn = CreateHeaderButton("BtnRefreshMed", _generalOptionsMenuRect, new Vector2(63f, y), new Vector2(53f, 20f), false, out _refreshMedBtnTxt);
            _refreshMedBtnTxt.text = "Medium";
            _refreshMedBtnBg = medBtn.GetComponent<Image>();
            medBtn.onClick.AddListener(() => SetRefreshRate(ERefreshRate.Medium));

            Button slowBtn = CreateHeaderButton("BtnRefreshSlow", _generalOptionsMenuRect, new Vector2(120f, y), new Vector2(53f, 20f), false, out _refreshSlowBtnTxt);
            _refreshSlowBtnTxt.text = "Slow";
            _refreshSlowBtnBg = slowBtn.GetComponent<Image>();
            slowBtn.onClick.AddListener(() => SetRefreshRate(ERefreshRate.Slow));

            Button pausedBtn = CreateHeaderButton("BtnRefreshPaused", _generalOptionsMenuRect, new Vector2(177f, y), new Vector2(53f, 20f), false, out _refreshPausedBtnTxt);
            _refreshPausedBtnTxt.text = "Paused";
            _refreshPausedBtnBg = pausedBtn.GetComponent<Image>();
            pausedBtn.onClick.AddListener(() => SetRefreshRate(ERefreshRate.Paused));
            y -= 26f;

            Button resetPosBtn = CreateHeaderButton("BtnResetPos", _generalOptionsMenuRect, new Vector2(-6f, y - 4f), new Vector2(224f, 22f), true, out Text resetPosTxt);
            resetPosTxt.text = "Reset UI Position & Size";
            resetPosBtn.onClick.AddListener(() =>
            {
                _config.ResetPanelLayout();
                _panelRect.anchoredPosition = new Vector2(StellogisticsConfig.DefaultPosX, StellogisticsConfig.DefaultPosY);
                PositionCustomPickerBesidePanel();
                _forceRefresh = true;
            });

            _generalOptionsMenuGo.SetActive(false);
        }

        private GeneralToggleRow CreateGeneralOptionToggleRow(
            RectTransform parent,
            string label,
            float posY,
            Func<bool> getter,
            Action toggler)
        {
            GameObject rowGo = new GameObject($"OptToggle_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            rowGo.transform.SetParent(parent, false);
            RectTransform rect = rowGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, posY);
            rect.sizeDelta = new Vector2(-12f, 24f);

            Image rowBg = rowGo.GetComponent<Image>();
            rowBg.color = new Color(0.10f, 0.16f, 0.24f, 0.78f);

            Button btn = rowGo.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.30f, 1.36f, 1.44f, 1f);
            cb.pressedColor = new Color(0.80f, 0.85f, 0.92f, 1f);
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

            btn.onClick.AddListener(() =>
            {
                toggler();
                RefreshGeneralOptionsMenuState();
                _forceRefresh = true;
            });

            return new GeneralToggleRow
            {
                Getter = getter,
                Toggler = toggler,
                Btn = btn,
                CheckBoxBg = boxImg,
                CheckMarkText = checkTxt,
                LabelText = lblTxt
            };
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

            _customPickerTitleText = CreateText("PickerTitle", headerRect, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.97f, 1f, 1f));
            _customPickerTitleText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _customPickerTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _customPickerTitleText.rectTransform.offsetMin = new Vector2(10f, 0f);
            _customPickerTitleText.rectTransform.offsetMax = new Vector2(-34f, 0f);
            _customPickerTitleText.text = "ITEM, BUILDING & PLANET PICKER (Click to toggle)";

            Button closeBtn = CreateHeaderButton("BtnClosePicker", headerRect, new Vector2(-4f, -3f), new Vector2(22f, 22f), true, out Text closeTxt);
            closeTxt.text = "✕";
            closeBtn.onClick.AddListener(() =>
            {
                _customPickerOpen = false;
                _customPickerGo.SetActive(false);
            });

            // Tabs row: Items (tab 1), Buildings (tab 2), and Planets (tab 3) — uniform 24px height at y = -36
            Button tabItemsBtn = CreateHeaderButton("TabItems", _customPickerRect, new Vector2(10f, -36f), new Vector2(68f, 24f), false, out Text tabItemsTxt);
            tabItemsTxt.text = "Items";
            tabItemsTxt.fontSize = 11;
            _customTabItemsBg = tabItemsBtn.GetComponent<Image>();
            tabItemsBtn.onClick.AddListener(() => SetCustomPickerTab(1));

            Button tabBldgsBtn = CreateHeaderButton("TabBuildings", _customPickerRect, new Vector2(82f, -36f), new Vector2(76f, 24f), false, out Text tabBldgsTxt);
            tabBldgsTxt.text = "Buildings";
            tabBldgsTxt.fontSize = 11;
            _customTabBuildingsBg = tabBldgsBtn.GetComponent<Image>();
            tabBldgsBtn.onClick.AddListener(() => SetCustomPickerTab(2));

            Button tabPlanetsBtn = CreateHeaderButton("TabPlanets", _customPickerRect, new Vector2(162f, -36f), new Vector2(78f, 24f), false, out _customTabPlanetsText);
            _customTabPlanetsText.text = "Planets";
            _customTabPlanetsText.fontSize = 11;
            _customTabPlanetsBg = tabPlanetsBtn.GetComponent<Image>();
            tabPlanetsBtn.onClick.AddListener(() => SetCustomPickerTab(3));

            // Vertical separator between category tabs and action buttons
            GameObject sepGo = new GameObject("ToolbarDivider", typeof(RectTransform), typeof(Image));
            sepGo.transform.SetParent(_customPickerRect, false);
            RectTransform sepRect = sepGo.GetComponent<RectTransform>();
            sepRect.anchorMin = new Vector2(0f, 1f);
            sepRect.anchorMax = new Vector2(0f, 1f);
            sepRect.pivot = new Vector2(0.5f, 1f);
            sepRect.anchoredPosition = new Vector2(248f, -36f);
            sepRect.sizeDelta = new Vector2(1f, 24f);
            Image sepImg = sepGo.GetComponent<Image>();
            sepImg.color = new Color(0.32f, 0.56f, 0.78f, 0.55f);
            sepImg.raycastTarget = false;

            // Visually distinct bulk action buttons (24px height, outlined, green/rose color coding)
            _pickerCheckAllBtn = CreateHeaderButton("PickerCheckAll", _customPickerRect, new Vector2(254f, -36f), new Vector2(74f, 24f), false, out Text pickerCheckAllTxt);
            pickerCheckAllTxt.text = "✓ Check All";
            pickerCheckAllTxt.color = new Color(0.86f, 1f, 0.92f, 1f);
            Image checkAllBg = _pickerCheckAllBtn.GetComponent<Image>();
            checkAllBg.color = new Color(0.11f, 0.30f, 0.23f, 0.96f);
            Outline checkOutline = _pickerCheckAllBtn.GetComponent<Outline>();
            if (checkOutline != null) checkOutline.effectColor = new Color(0.26f, 0.72f, 0.54f, 0.70f);
            _pickerCheckAllBtn.onClick.AddListener(() =>
            {
                _hasSavedFinderSnapshot = false;
                _config.SetCustomItemsAllVisible(_lastGalaxySeed, _lastTargetAstroId, true);
                RefreshCustomPickerCells();
                RefreshOptionsMenuState();
                _forceRefresh = true;
            });

            _pickerUncheckAllBtn = CreateHeaderButton("PickerUncheckAll", _customPickerRect, new Vector2(332f, -36f), new Vector2(82f, 24f), false, out Text pickerUncheckAllTxt);
            pickerUncheckAllTxt.text = "✕ Uncheck All";
            pickerUncheckAllTxt.color = new Color(1f, 0.88f, 0.90f, 1f);
            Image uncheckAllBg = _pickerUncheckAllBtn.GetComponent<Image>();
            uncheckAllBg.color = new Color(0.30f, 0.14f, 0.18f, 0.96f);
            Outline uncheckOutline = _pickerUncheckAllBtn.GetComponent<Outline>();
            if (uncheckOutline != null) uncheckOutline.effectColor = new Color(0.78f, 0.34f, 0.40f, 0.70f);
            _pickerUncheckAllBtn.onClick.AddListener(() =>
            {
                _hasSavedFinderSnapshot = false;
                _config.SetCustomItemsAllVisible(_lastGalaxySeed, _lastTargetAstroId, false);
                RefreshCustomPickerCells();
                RefreshOptionsMenuState();
                _forceRefresh = true;
            });

            // Planets tab button: Show all planets / clear all ignored planets & systems
            _pickerShowAllPlanetsBtn = CreateHeaderButton("PickerShowAllPlanets", _customPickerRect, new Vector2(256f, -36f), new Vector2(124f, 24f), false, out Text pickerShowAllPlanetsTxt);
            pickerShowAllPlanetsTxt.text = "✓ Show All Planets";
            pickerShowAllPlanetsTxt.color = new Color(0.86f, 1f, 0.92f, 1f);
            Image showAllPlanetsBg = _pickerShowAllPlanetsBtn.GetComponent<Image>();
            showAllPlanetsBg.color = new Color(0.11f, 0.30f, 0.23f, 0.96f);
            Outline showAllPlanetsOutline = _pickerShowAllPlanetsBtn.GetComponent<Outline>();
            if (showAllPlanetsOutline != null) showAllPlanetsOutline.effectColor = new Color(0.26f, 0.72f, 0.54f, 0.70f);
            _pickerShowAllPlanetsBtn.onClick.AddListener(() =>
            {
                _config.ClearAllIgnoredAstros(_lastGalaxySeed);
                RefreshCustomPickerPlanetsList();
                RefreshOptionsMenuState();
                _forceRefresh = true;
            });
            _pickerShowAllPlanetsBtn.gameObject.SetActive(false);

            _customPickerHoveredLabel = CreateText("HoveredItemLabel", _customPickerRect, 11, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.80f, 0.92f, 1f, 1f));
            _customPickerHoveredLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _customPickerHoveredLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _customPickerHoveredLabel.rectTransform.pivot = new Vector2(1f, 1f);
            _customPickerHoveredLabel.rectTransform.anchoredPosition = new Vector2(-12f, -36f);
            _customPickerHoveredLabel.rectTransform.sizeDelta = new Vector2(-440f, 24f);
            _customPickerHoveredLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _customPickerHoveredLabel.verticalOverflow = VerticalWrapMode.Truncate;
            _customPickerHoveredLabel.resizeTextForBestFit = true;
            _customPickerHoveredLabel.resizeTextMinSize = 9;
            _customPickerHoveredLabel.resizeTextMaxSize = 11;
            _customPickerHoveredLabel.text = "Bright = Shown • Dimmed = Hidden";

            // Grid container
            GameObject gridGo = new GameObject("GridContainer", typeof(RectTransform), typeof(Image));
            _customPickerGridGo = gridGo;
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

                    Outline cellOutline = cellGo.AddComponent<Outline>();
                    cellOutline.effectColor = new Color(0.28f, 0.50f, 0.74f, 0.40f);
                    cellOutline.effectDistance = new Vector2(1f, -1f);

                    Button btn = cellGo.GetComponent<Button>();
                    ColorBlock cellCb = btn.colors;
                    cellCb.normalColor = Color.white;
                    cellCb.highlightedColor = new Color(1.32f, 1.38f, 1.48f, 1f);
                    cellCb.pressedColor = new Color(0.80f, 0.86f, 0.94f, 1f);
                    btn.colors = cellCb;
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
                        IconImage = iconImg,
                        CellOutline = cellOutline
                    });
                }
            }

            BuildCustomPickerPlanetsTab(padX, -topHeight, gridWidth, gridHeight);

            _customPickerGo.SetActive(false);
        }

        private void BuildCustomPickerPlanetsTab(float posX, float posY, float width, float height)
        {
            _customPickerPlanetsScrollGo = new GameObject("PlanetsContainer", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            _customPickerPlanetsScrollGo.transform.SetParent(_customPickerRect, false);
            RectTransform scrollRectTransform = _customPickerPlanetsScrollGo.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 1f);
            scrollRectTransform.anchorMax = new Vector2(0f, 1f);
            scrollRectTransform.pivot = new Vector2(0f, 1f);
            scrollRectTransform.anchoredPosition = new Vector2(posX, posY);
            scrollRectTransform.sizeDelta = new Vector2(width, height);

            Image bgImg = _customPickerPlanetsScrollGo.GetComponent<Image>();
            bgImg.color = new Color(0.04f, 0.06f, 0.09f, 0.92f);
            bgImg.raycastTarget = true;
            Mask mask = _customPickerPlanetsScrollGo.GetComponent<Mask>();
            mask.showMaskGraphic = true;

            GameObject contentGo = new GameObject("PlanetsContent", typeof(RectTransform));
            contentGo.transform.SetParent(scrollRectTransform, false);
            _customPickerPlanetsContentRect = contentGo.GetComponent<RectTransform>();
            _customPickerPlanetsContentRect.anchorMin = new Vector2(0f, 1f);
            _customPickerPlanetsContentRect.anchorMax = new Vector2(1f, 1f);
            _customPickerPlanetsContentRect.pivot = new Vector2(0f, 1f);
            _customPickerPlanetsContentRect.anchoredPosition = Vector2.zero;
            _customPickerPlanetsContentRect.sizeDelta = new Vector2(0f, height);

            ScrollRect sr = _customPickerPlanetsScrollGo.GetComponent<ScrollRect>();
            sr.viewport = scrollRectTransform;
            sr.content = _customPickerPlanetsContentRect;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 28f;

            _customPickerPlanetsEmptyGo = new GameObject("PlanetsEmpty", typeof(RectTransform));
            _customPickerPlanetsEmptyGo.transform.SetParent(_customPickerPlanetsContentRect, false);
            RectTransform emptyRect = _customPickerPlanetsEmptyGo.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0f, 1f);
            emptyRect.anchorMax = new Vector2(1f, 1f);
            emptyRect.pivot = new Vector2(0.5f, 1f);
            emptyRect.anchoredPosition = new Vector2(0f, -20f);
            emptyRect.sizeDelta = new Vector2(-20f, 40f);
            Text emptyTxt = CreateText("EmptyText", emptyRect, 12, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.60f, 0.72f, 0.82f, 0.95f));
            emptyTxt.rectTransform.anchorMin = Vector2.zero;
            emptyTxt.rectTransform.anchorMax = Vector2.one;
            emptyTxt.rectTransform.sizeDelta = Vector2.zero;
            emptyTxt.text = "No planets with Interplanetary Logistics Stations found.";
            _customPickerPlanetsEmptyGo.SetActive(false);

            _customPickerPlanetsScrollGo.SetActive(false);
        }

        private PlanetFilterRowView GetOrCreatePlanetFilterRow(int index)
        {
            while (_planetFilterRowPool.Count <= index)
            {
                int idx = _planetFilterRowPool.Count;
                GameObject rowGo = new GameObject($"PlanetFilterRow_{idx}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
                rowGo.transform.SetParent(_customPickerPlanetsContentRect, false);
                RectTransform rect = rowGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(272f, 26f);

                Image rowBg = rowGo.GetComponent<Image>();
                rowBg.color = new Color(0.10f, 0.16f, 0.24f, 0.85f);

                Outline outline = rowGo.GetComponent<Outline>();
                outline.effectColor = new Color(0.26f, 0.48f, 0.72f, 0.35f);
                outline.effectDistance = new Vector2(1f, -1f);

                Button btn = rowGo.GetComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.28f, 1.34f, 1.42f, 1f);
                cb.pressedColor = new Color(0.80f, 0.85f, 0.92f, 1f);
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

                Text nameTxt = CreateText("Name", rect, 11, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.97f, 1f, 1f));
                nameTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
                nameTxt.rectTransform.anchorMax = new Vector2(0.62f, 1f);
                nameTxt.rectTransform.offsetMin = new Vector2(28f, 0f);
                nameTxt.rectTransform.offsetMax = new Vector2(-2f, 0f);

                Text metaTxt = CreateText("Meta", rect, 10, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.64f, 0.80f, 0.92f, 0.95f));
                metaTxt.rectTransform.anchorMin = new Vector2(0.58f, 0f);
                metaTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                metaTxt.rectTransform.offsetMin = new Vector2(0f, 0f);
                metaTxt.rectTransform.offsetMax = new Vector2(-6f, 0f);

                var view = new PlanetFilterRowView
                {
                    RootGo = rowGo,
                    RootRect = rect,
                    BgImage = rowBg,
                    Btn = btn,
                    CheckBoxBg = boxImg,
                    CheckMarkText = checkTxt,
                    NameText = nameTxt,
                    MetaText = metaTxt
                };
                btn.onClick.AddListener(() => view.OnClick?.Invoke());
                _planetFilterRowPool.Add(view);
            }

            PlanetFilterRowView row = _planetFilterRowPool[index];
            if (!row.RootGo.activeSelf)
            {
                row.RootGo.SetActive(true);
            }
            return row;
        }

        private void RefreshCustomPickerPlanetsList()
        {
            if (_customPickerGo == null)
            {
                return;
            }

            int ignoredCount = _config.GetIgnoredAstroCount(_lastGalaxySeed);
            if (_customTabPlanetsText != null)
            {
                _customTabPlanetsText.text = ignoredCount > 0 ? $"Planets (-{ignoredCount})" : "Planets";
            }

            if (!_customPickerOpen || _customPickerActiveTab != 3 || _customPickerPlanetsContentRect == null)
            {
                return;
            }

            LogisticsAggregator.CollectLogisticsPlanetsAndStars(_config, _planetFilterEntries);
            if (_planetFilterEntries.Count == 0)
            {
                _customPickerPlanetsEmptyGo.SetActive(true);
                for (int i = 0; i < _planetFilterRowPool.Count; i++)
                {
                    _planetFilterRowPool[i].RootGo.SetActive(false);
                }
                _customPickerPlanetsContentRect.sizeDelta = new Vector2(0f, 120f);
                return;
            }

            _customPickerPlanetsEmptyGo.SetActive(false);

            const float colWidth = 272f;
            const float colGap = 6f;
            const float rowH = 26f;
            const float rowGap = 3f;
            const float padLeft = 4f;
            const float padTop = 4f;

            for (int i = 0; i < _planetFilterEntries.Count; i++)
            {
                PlanetLogisticsFilterEntry entry = _planetFilterEntries[i];
                PlanetFilterRowView row = GetOrCreatePlanetFilterRow(i);

                int col = i % 2;
                int r = i / 2;
                row.RootRect.anchoredPosition = new Vector2(padLeft + col * (colWidth + colGap), -(padTop + r * (rowH + rowGap)));

                bool isSelfIgnored = _config.IsAstroIgnored(_lastGalaxySeed, entry.AstroId);
                bool isParentStarIgnored = !entry.IsStarSystem && _config.IsAstroIgnored(_lastGalaxySeed, entry.ParentStarAstroId);
                bool effectivelyIgnored = isSelfIgnored || isParentStarIgnored;

                if (entry.IsStarSystem)
                {
                    row.BgImage.color = effectivelyIgnored
                        ? new Color(0.20f, 0.10f, 0.12f, 0.78f)
                        : new Color(0.14f, 0.24f, 0.36f, 0.95f);
                }
                else
                {
                    row.BgImage.color = effectivelyIgnored
                        ? new Color(0.15f, 0.08f, 0.10f, 0.68f)
                        : new Color(0.09f, 0.15f, 0.23f, 0.88f);
                }

                row.CheckMarkText.text = effectivelyIgnored ? "✕" : "✓";
                row.CheckMarkText.color = effectivelyIgnored ? CriticalColor : HealthyColor;
                row.CheckBoxBg.color = effectivelyIgnored
                    ? new Color(0.32f, 0.14f, 0.16f, 1f)
                    : new Color(0.18f, 0.42f, 0.62f, 1f);

                string dispName = entry.IsStarSystem ? entry.DisplayName : $"  ▸ {entry.DisplayName}";
                if (dispName.Length > 22)
                {
                    dispName = dispName.Substring(0, 20) + "..";
                }
                row.NameText.text = dispName;
                row.NameText.fontStyle = entry.IsStarSystem ? FontStyle.Bold : FontStyle.Normal;
                row.NameText.color = effectivelyIgnored
                    ? new Color(0.65f, 0.55f, 0.58f, 0.85f)
                    : (entry.IsStarSystem ? new Color(0.96f, 0.98f, 1f, 1f) : new Color(0.88f, 0.94f, 1f, 1f));

                row.MetaText.text = effectivelyIgnored ? "Ignored" : entry.MetaInfo;
                row.MetaText.color = effectivelyIgnored
                    ? CriticalColor
                    : new Color(0.62f, 0.78f, 0.92f, 0.95f);

                int capturedAstroId = entry.AstroId;
                int capturedParentStarAstroId = entry.ParentStarAstroId;
                bool capturedIsStar = entry.IsStarSystem;
                row.OnClick = () =>
                {
                    if (!capturedIsStar && _config.IsAstroIgnored(_lastGalaxySeed, capturedParentStarAstroId))
                    {
                        // If parent star system was ignored and user clicks a planet inside it, un-ignore the star system
                        _config.SetAstroIgnored(_lastGalaxySeed, capturedParentStarAstroId, false);
                    }
                    else
                    {
                        _config.ToggleAstroIgnored(_lastGalaxySeed, capturedAstroId);
                    }
                    RefreshCustomPickerPlanetsList();
                    RefreshOptionsMenuState();
                    _forceRefresh = true;
                };
            }

            for (int i = _planetFilterEntries.Count; i < _planetFilterRowPool.Count; i++)
            {
                if (_planetFilterRowPool[i].RootGo.activeSelf)
                {
                    _planetFilterRowPool[i].RootGo.SetActive(false);
                }
            }

            int totalRows = (_planetFilterEntries.Count + 1) / 2;
            float contentH = Mathf.Max(318f, padTop * 2f + totalRows * (rowH + rowGap));
            _customPickerPlanetsContentRect.sizeDelta = new Vector2(0f, contentH);
        }

        private void ToggleCustomItemPicker()
        {
            if (_customPickerGo == null)
            {
                return;
            }

            CloseDropdownMenus();

            if (_customPickerOpen)
            {
                _customPickerOpen = false;
                _customPickerGo.SetActive(false);
                return;
            }

            _customPickerOpen = true;
            _customPickerGo.SetActive(true);

            PositionCustomPickerBesidePanel();
            _customPickerRect.SetAsLastSibling();
            SetCustomPickerTab(_customPickerActiveTab);
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
            if (_customTabPlanetsBg != null)
            {
                _customTabPlanetsBg.color = (tab == 3)
                    ? new Color(0.24f, 0.44f, 0.66f, 1f)
                    : new Color(0.12f, 0.18f, 0.28f, 0.85f);
            }

            bool isPlanetsTab = (tab == 3);
            if (_customPickerGridGo != null) _customPickerGridGo.SetActive(!isPlanetsTab);
            if (_customPickerPlanetsScrollGo != null) _customPickerPlanetsScrollGo.SetActive(isPlanetsTab);

            if (_pickerCheckAllBtn != null) _pickerCheckAllBtn.gameObject.SetActive(!isPlanetsTab);
            if (_pickerUncheckAllBtn != null) _pickerUncheckAllBtn.gameObject.SetActive(!isPlanetsTab);
            if (_pickerShowAllPlanetsBtn != null) _pickerShowAllPlanetsBtn.gameObject.SetActive(isPlanetsTab);

            if (_customPickerHoveredLabel != null)
            {
                _customPickerHoveredLabel.rectTransform.sizeDelta = new Vector2(-430f, 24f);
                _customPickerHoveredLabel.text = isPlanetsTab
                    ? "Toggle Star / Planet in Finder"
                    : "Bright = Shown • Dimmed = Hidden";
                _customPickerHoveredLabel.color = new Color(0.80f, 0.92f, 1f, 1f);
            }

            RefreshCustomPickerPlanetsList();
            if (!isPlanetsTab)
            {
                RefreshCustomPickerCells();
            }
        }

        private void OnCustomPickerItemClicked(int itemId, string itemName, EItemCategoryFilter itemCategory)
        {
            _hasSavedFinderSnapshot = false;
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
                    ? new Color(0.16f, 0.32f, 0.48f, 0.96f)
                    : new Color(0.05f, 0.08f, 0.12f, 0.82f);
                cell.IconImage.color = visible
                    ? Color.white
                    : new Color(0.75f, 0.80f, 0.86f, 0.52f);
                if (cell.CellOutline != null)
                {
                    cell.CellOutline.effectColor = visible
                        ? new Color(0.36f, 0.68f, 0.96f, 0.68f)
                        : new Color(0.18f, 0.28f, 0.40f, 0.28f);
                }
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
            rowBg.color = new Color(0.10f, 0.16f, 0.24f, 0.78f);

            Button btn = rowGo.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.30f, 1.36f, 1.44f, 1f);
            cb.pressedColor = new Color(0.80f, 0.85f, 0.92f, 1f);
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
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            btnGo.transform.SetParent(parent, false);
            RectTransform rect = btnGo.GetComponent<RectTransform>();
            float ax = anchorRight ? 1f : 0f;
            rect.anchorMin = new Vector2(ax, 1f);
            rect.anchorMax = new Vector2(ax, 1f);
            rect.pivot = new Vector2(ax, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image bg = btnGo.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.26f, 0.39f, 0.96f);

            Outline outline = btnGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.30f, 0.54f, 0.78f, 0.45f);
            outline.effectDistance = new Vector2(1f, -1f);

            Button btn = btnGo.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.30f, 1.35f, 1.42f, 1f);
            cb.pressedColor = new Color(0.80f, 0.85f, 0.92f, 1f);
            btn.colors = cb;

            btnText = CreateText("Text", rect, 10, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.90f, 0.97f, 1f, 1f));
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
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.30f, 1.38f, 1f);
            cb.pressedColor = new Color(0.82f, 0.87f, 0.94f, 1f);
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
                row.MuteSlideProgress = 0f;
                row.RootGo.SetActive(true);
            }
            return row;
        }

        private ItemRowView CreateRowWidget(int index)
        {
            GameObject rowGo = new GameObject($"ItemRow_{index}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PointerHoverTracker));
            rowGo.transform.SetParent(_scrollContentRect, false);
            RectTransform rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(0f, RowHeight);

            PointerHoverTracker rowHoverTracker = rowGo.GetComponent<PointerHoverTracker>();

            Image bgImg = rowGo.GetComponent<Image>();
            Color baseBg = (index % 2 == 0) ? RowEvenBgColor : RowOddBgColor;
            bgImg.color = baseBg;

            Button rowBtn = rowGo.GetComponent<Button>();
            ColorBlock rowCb = rowBtn.colors;
            rowCb.normalColor = Color.white;
            rowCb.highlightedColor = new Color(1.22f, 1.28f, 1.38f, 1f);
            rowCb.pressedColor = new Color(0.85f, 0.90f, 0.98f, 1f);
            rowBtn.colors = rowCb;
            rowBtn.targetGraphic = bgImg;

            GameObject fillGo = new GameObject("FillBar", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(rowRect, false);
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImg = fillGo.GetComponent<Image>();
            fillImg.color = HealthyFillColor;
            fillImg.raycastTarget = false;

            GameObject transitGo = new GameObject("TransitBar", typeof(RectTransform), typeof(Image));
            transitGo.transform.SetParent(rowRect, false);
            RectTransform transitRect = transitGo.GetComponent<RectTransform>();
            transitRect.anchorMin = new Vector2(0f, 0f);
            transitRect.anchorMax = new Vector2(0f, 1f);
            transitRect.pivot = new Vector2(0f, 0.5f);
            transitRect.offsetMin = Vector2.zero;
            transitRect.offsetMax = Vector2.zero;
            Image transitImg = transitGo.GetComponent<Image>();
            transitImg.sprite = GetOrCreateHatchedTransitSprite();
            transitImg.type = Image.Type.Tiled;
            transitImg.color = HealthyTransitColor;
            transitImg.raycastTarget = false;
            transitGo.SetActive(false);

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

            // Hover-revealed [✕] button on Finder rows with slide-in animation
            GameObject muteBtnGo = new GameObject("MutePlanetBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PointerHoverTracker), typeof(ItemCellHoverHandler));
            muteBtnGo.transform.SetParent(rowRect, false);
            RectTransform muteRect = muteBtnGo.GetComponent<RectTransform>();
            muteRect.anchorMin = new Vector2(1f, 0.5f);
            muteRect.anchorMax = new Vector2(1f, 0.5f);
            muteRect.pivot = new Vector2(1f, 0.5f);
            muteRect.anchoredPosition = new Vector2(10f, 0f);
            muteRect.sizeDelta = new Vector2(15f, 15f);

            PointerHoverTracker muteHoverTracker = muteBtnGo.GetComponent<PointerHoverTracker>();

            Image muteBg = muteBtnGo.GetComponent<Image>();
            muteBg.color = new Color(0.14f, 0.22f, 0.32f, 0f);

            Button muteBtn = muteBtnGo.GetComponent<Button>();
            ColorBlock muteCb = muteBtn.colors;
            muteCb.normalColor = Color.white;
            muteCb.highlightedColor = Color.white;
            muteCb.pressedColor = new Color(0.82f, 0.72f, 0.72f, 1f);
            muteBtn.colors = muteCb;

            Text muteTxt = CreateText("Text", muteRect, 9, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.76f, 0.88f, 0.98f, 0f));
            muteTxt.rectTransform.anchorMin = Vector2.zero;
            muteTxt.rectTransform.anchorMax = Vector2.one;
            muteTxt.rectTransform.sizeDelta = Vector2.zero;
            muteTxt.text = "✕";

            muteBtnGo.SetActive(false);

            var view = new ItemRowView
            {
                RootGo = rowGo,
                RootRect = rowRect,
                BgImage = bgImg,
                RowButton = rowBtn,
                RowHoverTracker = rowHoverTracker,
                MuteBtnGo = muteBtnGo,
                MuteBtnRect = muteRect,
                MuteBtnBg = muteBg,
                MuteBtnText = muteTxt,
                MuteHoverTracker = muteHoverTracker,
                ShowMuteOnHover = false,
                MuteSlideProgress = 0f,
                FillBarRect = fillRect,
                FillBarImage = fillImg,
                TransitBarRect = transitRect,
                TransitBarImage = transitImg,
                StatusStripeImage = stripeImg,
                IconImage = iconImg,
                NameText = nameText,
                MetaText = metaText,
                AmountText = amountText,
                SubStatusText = subStatusText
            };

            ItemCellHoverHandler muteHover = muteBtnGo.GetComponent<ItemCellHoverHandler>();
            muteHover.OnHoverEnter = () => ApplyRowMuteAnimationState(view);
            muteHover.OnHoverExit = () => ApplyRowMuteAnimationState(view);

            rowBtn.onClick.AddListener(() => view.OnRowClick?.Invoke());
            muteBtn.onClick.AddListener(() => view.OnMuteClick?.Invoke());
            return view;
        }

        private void UpdateRowHoverAnimations()
        {
            if (_panelCollapsed || _rowPool.Count == 0)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            const float speed = 10f;

            for (int i = 0; i < _rowPool.Count; i++)
            {
                ItemRowView row = _rowPool[i];
                if (row == null || !row.RootGo.activeSelf)
                {
                    continue;
                }

                bool isHovered = row.ShowMuteOnHover &&
                    ((row.RowHoverTracker != null && row.RowHoverTracker.IsHovered) ||
                     (row.MuteHoverTracker != null && row.MuteHoverTracker.IsHovered));

                float target = isHovered ? 1f : 0f;
                if (Mathf.Approximately(row.MuteSlideProgress, target))
                {
                    row.MuteSlideProgress = target;
                    continue;
                }

                row.MuteSlideProgress = Mathf.MoveTowards(row.MuteSlideProgress, target, dt * speed);
                ApplyRowMuteAnimationState(row);
            }
        }

        private static void ApplyRowMuteAnimationState(ItemRowView row)
        {
            float t = row.ShowMuteOnHover ? Mathf.Clamp01(row.MuteSlideProgress) : 0f;
            float eased = 1f - (1f - t) * (1f - t);

            float rightTextOffset = Mathf.Lerp(-10f, -23f, eased);
            row.AmountText.rectTransform.offsetMax = new Vector2(rightTextOffset, -1f);
            row.SubStatusText.rectTransform.offsetMax = new Vector2(rightTextOffset, 0f);

            bool shouldBeActive = row.ShowMuteOnHover && t > 0.01f;
            if (row.MuteBtnGo.activeSelf != shouldBeActive)
            {
                row.MuteBtnGo.SetActive(shouldBeActive);
            }

            if (shouldBeActive)
            {
                float xPos = Mathf.Lerp(10f, -4f, eased);
                row.MuteBtnRect.anchoredPosition = new Vector2(xPos, 0f);

                bool muteDirectHover = row.MuteHoverTracker != null && row.MuteHoverTracker.IsHovered;
                Color bgCol = muteDirectHover
                    ? new Color(0.48f, 0.15f, 0.20f, 0.92f * eased)
                    : new Color(0.14f, 0.22f, 0.32f, 0.72f * eased);
                Color txtCol = muteDirectHover
                    ? new Color(1f, 0.84f, 0.86f, 1f * eased)
                    : new Color(0.76f, 0.88f, 0.98f, 0.90f * eased);

                row.MuteBtnBg.color = bgCol;
                row.MuteBtnText.color = txtCol;
            }
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
            txt.supportRichText = true;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        public void CloseDropdownMenus()
        {
            if (_optionsMenuOpen)
            {
                _optionsMenuOpen = false;
                if (_optionsMenuGo != null) _optionsMenuGo.SetActive(false);
                if (_optionsBtnText != null) _optionsBtnText.text = "Filter ▾";
            }
            if (_generalOptionsMenuOpen)
            {
                _generalOptionsMenuOpen = false;
                if (_generalOptionsMenuGo != null) _generalOptionsMenuGo.SetActive(false);
                if (_generalOptionsBtnText != null) _generalOptionsBtnText.text = "Options ▾";
            }
        }

        private void CheckAutoCloseDropdownMenus()
        {
            if (!_optionsMenuOpen && !_generalOptionsMenuOpen)
            {
                return;
            }

            EventSystem es = EventSystem.current;
            if (es == null)
            {
                return;
            }

            GameObject sel = es.currentSelectedGameObject;
            if (_optionsMenuOpen && _optionsMenuRect != null && _optionsBtn != null)
            {
                bool isMenuOrBtnSelected = sel != null &&
                    (sel == _optionsBtn.gameObject || sel.transform.IsChildOf(_optionsMenuRect));
                if (!isMenuOrBtnSelected)
                {
                    if (_optionsHoverTracker != null && _optionsHoverTracker.IsHovered)
                    {
                        es.SetSelectedGameObject(_optionsBtn.gameObject);
                    }
                    else
                    {
                        CloseDropdownMenus();
                        return;
                    }
                }
            }

            if (_generalOptionsMenuOpen && _generalOptionsMenuRect != null && _generalOptionsBtn != null)
            {
                bool isMenuOrBtnSelected = sel != null &&
                    (sel == _generalOptionsBtn.gameObject || sel.transform.IsChildOf(_generalOptionsMenuRect));
                if (!isMenuOrBtnSelected)
                {
                    if (_generalOptionsHoverTracker != null && _generalOptionsHoverTracker.IsHovered)
                    {
                        es.SetSelectedGameObject(_generalOptionsBtn.gameObject);
                    }
                    else
                    {
                        CloseDropdownMenus();
                    }
                }
            }
        }

        private void OnTogglePanelCollapseClicked()
        {
            CloseDropdownMenus();
            _panelCollapsed = !_panelCollapsed;
            _panelCollapseBtnText.text = _panelCollapsed ? "▶" : "▼";
            if (_panelCollapsed)
            {
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
            CloseDropdownMenus();
            _exportsCollapsed = !_exportsCollapsed;
            _forceRefresh = true;
        }

        private void OnToggleImportsCollapseClicked()
        {
            CloseDropdownMenus();
            _importsCollapsed = !_importsCollapsed;
            _forceRefresh = true;
        }

        private void OnFilterModeButtonClicked()
        {
            CloseDropdownMenus();
            if (_finderModeActive)
            {
                ToggleCustomItemPicker();
                return;
            }
            int next = ((int)_config.StarSystemFilterMode.Value + 1) % 3;
            _config.StarSystemFilterMode.Value = (EStarFilterMode)next;
            _forceRefresh = true;
        }

        private void ReturnToFinderOverview()
        {
            if (_hasSavedFinderSnapshot)
            {
                _hasSavedFinderSnapshot = false;
                _config.RestoreHiddenItemsSnapshot(_lastGalaxySeed, _lastTargetAstroId, _savedFinderHiddenSnapshot);
            }
            else
            {
                _config.SetCustomItemsAllVisible(_lastGalaxySeed, _lastTargetAstroId, true);
            }

            _finderModeActive = true;
            if (_customPickerOpen)
            {
                SetCustomPickerTab(_customPickerActiveTab);
            }
            RefreshOptionsMenuState();
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
            _forceRefresh = true;
        }

        private void OnFinderModeButtonClicked()
        {
            CloseDropdownMenus();
            if (_finderModeActive)
            {
                if (_hasSavedFinderSnapshot || _lastFinderCheckedCount == 1)
                {
                    ReturnToFinderOverview();
                    return;
                }

                // Exit Finder mode back to normal Planet/Star inspection
                _finderModeActive = false;
                if (_customPickerOpen)
                {
                    SetCustomPickerTab(_customPickerActiveTab);
                }
                if (_scrollRect != null)
                {
                    _scrollRect.verticalNormalizedPosition = 1f;
                }
                _forceRefresh = true;
                return;
            }

            if (_panelCollapsed)
            {
                _panelCollapsed = false;
                _panelCollapseBtnText.text = "▼";
            }

            // Activate Finder view without auto-opening the item picker
            _finderModeActive = true;
            if (_customPickerOpen)
            {
                SetCustomPickerTab(_customPickerActiveTab);
            }
            _forceRefresh = true;
        }

        private void OnSortModeButtonClicked()
        {
            CloseDropdownMenus();
            int next = ((int)_config.SortOrder.Value + 1) % 3;
            _config.SortOrder.Value = (ESortOrder)next;
            _forceRefresh = true;
        }

        private void OnOptionsButtonClicked()
        {
            _optionsMenuOpen = !_optionsMenuOpen;
            if (_optionsMenuOpen && _generalOptionsMenuOpen)
            {
                _generalOptionsMenuOpen = false;
                _generalOptionsMenuGo.SetActive(false);
                _generalOptionsBtnText.text = "Options ▾";
            }
            if (_optionsMenuOpen && _panelCollapsed)
            {
                _panelCollapsed = false;
                _panelCollapseBtnText.text = "▼";
            }
            _optionsMenuGo.SetActive(_optionsMenuOpen);
            _optionsBtnText.text = _optionsMenuOpen ? "Filter ▴" : "Filter ▾";
            if (_optionsMenuOpen)
            {
                EventSystem.current?.SetSelectedGameObject(_optionsBtn.gameObject);
                _optionsMenuRect.SetAsLastSibling();
                RefreshOptionsMenuState();
            }
        }

        private void OnGeneralOptionsButtonClicked()
        {
            _generalOptionsMenuOpen = !_generalOptionsMenuOpen;
            if (_generalOptionsMenuOpen && _optionsMenuOpen)
            {
                _optionsMenuOpen = false;
                _optionsMenuGo.SetActive(false);
                _optionsBtnText.text = "Filter ▾";
            }
            if (_generalOptionsMenuOpen && _panelCollapsed)
            {
                _panelCollapsed = false;
                _panelCollapseBtnText.text = "▼";
            }
            _generalOptionsMenuGo.SetActive(_generalOptionsMenuOpen);
            _generalOptionsBtnText.text = _generalOptionsMenuOpen ? "Options ▴" : "Options ▾";
            if (_generalOptionsMenuOpen)
            {
                EventSystem.current?.SetSelectedGameObject(_generalOptionsBtn.gameObject);
                _generalOptionsMenuRect.SetAsLastSibling();
                RefreshGeneralOptionsMenuState();
            }
        }

        private void SetRefreshRate(ERefreshRate rate)
        {
            _config.RefreshRate.Value = rate;
            _nextRefreshTime = 0f;
            _forceRefresh = true;
            RefreshGeneralOptionsMenuState();
        }

        private void RefreshGeneralOptionsMenuState()
        {
            if (!_generalOptionsMenuOpen)
            {
                return;
            }

            for (int i = 0; i < _generalOptionToggles.Count; i++)
            {
                GeneralToggleRow row = _generalOptionToggles[i];
                bool enabled = row.Getter();
                row.CheckMarkText.text = enabled ? "✓" : "";
                row.CheckBoxBg.color = enabled
                    ? new Color(0.18f, 0.42f, 0.62f, 1f)
                    : new Color(0.12f, 0.16f, 0.22f, 1f);
                row.LabelText.color = enabled
                    ? new Color(0.92f, 0.96f, 1f, 1f)
                    : new Color(0.50f, 0.58f, 0.68f, 0.85f);
            }

            ERefreshRate rate = _config.RefreshRate.Value;
            Color activeBg = new Color(0.22f, 0.44f, 0.68f, 0.98f);
            Color pausedActiveBg = new Color(0.52f, 0.34f, 0.12f, 0.98f);
            Color inactiveBg = new Color(0.11f, 0.17f, 0.26f, 0.85f);
            Color activeTxt = new Color(0.95f, 0.99f, 1f, 1f);
            Color inactiveTxt = new Color(0.58f, 0.70f, 0.82f, 0.85f);

            if (_refreshFastBtnBg != null) _refreshFastBtnBg.color = rate == ERefreshRate.Fast ? activeBg : inactiveBg;
            if (_refreshMedBtnBg != null) _refreshMedBtnBg.color = rate == ERefreshRate.Medium ? activeBg : inactiveBg;
            if (_refreshSlowBtnBg != null) _refreshSlowBtnBg.color = rate == ERefreshRate.Slow ? activeBg : inactiveBg;
            if (_refreshPausedBtnBg != null) _refreshPausedBtnBg.color = rate == ERefreshRate.Paused ? pausedActiveBg : inactiveBg;

            if (_refreshFastBtnTxt != null) _refreshFastBtnTxt.color = rate == ERefreshRate.Fast ? activeTxt : inactiveTxt;
            if (_refreshMedBtnTxt != null) _refreshMedBtnTxt.color = rate == ERefreshRate.Medium ? activeTxt : inactiveTxt;
            if (_refreshSlowBtnTxt != null) _refreshSlowBtnTxt.color = rate == ERefreshRate.Slow ? activeTxt : inactiveTxt;
            if (_refreshPausedBtnTxt != null) _refreshPausedBtnTxt.color = rate == ERefreshRate.Paused ? WarningColor : inactiveTxt;
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

            if (_ignoreTargetToggleRow != null)
            {
                bool ignored = _ignoreTargetToggleRow.Getter();
                int totalIgnored = _config.GetIgnoredAstroCount(_lastGalaxySeed);
                string targetKind = (_lastResolvedStar != null && _lastResolvedPlanet == null) ? "Star System" : "Planet";
                string countSuffix = totalIgnored > 0 ? $" ({totalIgnored} ignored)" : "";
                _ignoreTargetToggleRow.LabelText.text = $"Ignore {targetKind} in Finder{countSuffix}";
                _ignoreTargetToggleRow.CheckMarkText.text = ignored ? "✓" : "";
                _ignoreTargetToggleRow.CheckBoxBg.color = ignored
                    ? new Color(0.18f, 0.42f, 0.62f, 1f)
                    : new Color(0.12f, 0.16f, 0.22f, 1f);
                _ignoreTargetToggleRow.LabelText.color = ignored
                    ? WarningColor
                    : new Color(0.92f, 0.96f, 1f, 1f);
            }
        }

        public void RequestImmediateRefresh()
        {
            _forceRefresh = true;
        }

        public void OnStarmapUpdate(UIStarmap starmap)
        {
            _currentStarmap = starmap;
            if (_config == null || !_config.EnableMod.Value || starmap == null || !starmap.active)
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            ResolveTarget(starmap, out PlanetData targetPlanet, out StarData targetStar);

            if (!_finderModeActive && targetPlanet == null && targetStar == null)
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

            if (targetPlanet != null || targetStar != null)
            {
                _lastResolvedPlanet = targetPlanet;
                _lastResolvedStar = targetStar;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            UpdatePanelPosition();
            CheckAutoCloseDropdownMenus();
            UpdateRowHoverAnimations();

            if (IsPointerOverUI())
            {
                VFInput.inScrollView = true;
            }

            if (!_finderModeActive && (targetPlanet != null || targetStar != null))
            {
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
            }

            float now = Time.unscaledTime;
            ERefreshRate rate = _config.RefreshRate.Value;
            bool timerDue = rate != ERefreshRate.Paused && now >= _nextRefreshTime;
            if (_forceRefresh || timerDue)
            {
                _forceRefresh = false;
                float interval = rate switch
                {
                    ERefreshRate.Fast => 0.10f,
                    ERefreshRate.Slow => 1.00f,
                    _ => 0.25f
                };
                _nextRefreshTime = now + interval;

                if (_finderModeActive)
                {
                    GalaxyItemSearchSummary searchSummary = LogisticsAggregator.ScanGalaxyFinder(_config);
                    RenderGalaxySearchSummary(searchSummary);
                }
                else
                {
                    LogisticsSummary summary = targetPlanet != null
                        ? LogisticsAggregator.AggregatePlanet(targetPlanet, _config)
                        : LogisticsAggregator.AggregateStar(targetStar, _config);

                    RenderSummary(summary);
                }
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
                _titleText.rectTransform.anchoredPosition = new Vector2(32f, -8f);
                _titleText.rectTransform.sizeDelta = new Vector2(-38f, 20f);
                _subtitleText.gameObject.SetActive(false);
                _fleetInfoText.gameObject.SetActive(false);
                _filterModeBtn.gameObject.SetActive(false);
                _finderModeBtn.gameObject.SetActive(false);
                _sortModeBtn.gameObject.SetActive(false);
                _optionsBtn.gameObject.SetActive(false);
                _generalOptionsBtn.gameObject.SetActive(false);
                _scrollRect.gameObject.SetActive(false);
                _resizeGripGo.SetActive(_isResizing);

                _headerRect.sizeDelta = new Vector2(0f, CollapsedTabHeight);
                _panelRect.sizeDelta = new Vector2(CollapsedTabWidth, CollapsedTabHeight);
                return;
            }

            float panelWidth = _config.PanelWidth.Value;
            float maxScrollHeight = _config.PanelMaxHeight.Value;

            // Expanded State
            float activeHeaderH = ApplyHeaderLayout(panelWidth, summary.IsStarSystem);
            _subtitleText.gameObject.SetActive(true);
            _fleetInfoText.gameObject.SetActive(true);
            _finderModeBtn.gameObject.SetActive(true);
            _finderModeBtnText.text = "Finder";
            _finderModeBtn.GetComponent<Image>().color = new Color(0.15f, 0.26f, 0.39f, 0.96f);
            _filterModeBtn.GetComponent<Image>().color = new Color(0.15f, 0.26f, 0.39f, 0.96f);
            _sortModeBtn.gameObject.SetActive(true);
            _optionsBtn.gameObject.SetActive(true);
            _generalOptionsBtn.gameObject.SetActive(true);
            _resizeGripGo.SetActive(true);

            _titleText.text = summary.TargetName;
            string pausedSuffix = _config.RefreshRate.Value == ERefreshRate.Paused
                ? " • <color=#FFD24C>⏸ Paused</color>"
                : "";
            _subtitleText.text = $"{summary.TargetSubtitle}{pausedSuffix}";

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
                float emptyViewportHeight = Mathf.Max(MinScrollHeight, maxScrollHeight);
                SetScrollbarActive(false);
                _scrollContentRect.anchoredPosition = Vector2.zero;
                _scrollContentRect.sizeDelta = new Vector2(0f, _activeContentHeight);
                _scrollViewportRect.sizeDelta = new Vector2(0f, emptyViewportHeight);
                _panelRect.sizeDelta = new Vector2(panelWidth, activeHeaderH + emptyViewportHeight);
                _resizeGripGo.transform.SetAsLastSibling();
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
            _activeContentHeight = Mathf.Max(MinScrollHeight, Mathf.Max(0f, estimatedHeight - RowSpacing));
            bool bothMenusCollapsed = _exportsCollapsed && _importsCollapsed;
            float viewportHeight = bothMenusCollapsed
                ? Mathf.Min(maxScrollHeight, _activeContentHeight)
                : Mathf.Max(MinScrollHeight, maxScrollHeight);
            bool needsScrollbar = _activeContentHeight > viewportHeight + 0.5f;
            float rightInset = needsScrollbar ? -(ScrollbarWidth + 1f) : 0f;
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

            SetScrollbarActive(needsScrollbar);
            _scrollContentRect.anchoredPosition = new Vector2(0f, _scrollContentRect.anchoredPosition.y);
            _scrollContentRect.sizeDelta = new Vector2(rightInset, _activeContentHeight);
            _scrollViewportRect.sizeDelta = new Vector2(0f, viewportHeight);
            _panelRect.sizeDelta = new Vector2(panelWidth, activeHeaderH + viewportHeight);
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
            Color transitColor;
            switch (status)
            {
                case EHealthStatus.Critical:
                    accentColor = CriticalColor;
                    fillColor = CriticalFillColor;
                    transitColor = CriticalTransitColor;
                    break;
                case EHealthStatus.Warning:
                    accentColor = WarningColor;
                    fillColor = WarningFillColor;
                    transitColor = WarningTransitColor;
                    break;
                default:
                    accentColor = HealthyColor;
                    fillColor = HealthyFillColor;
                    transitColor = HealthyTransitColor;
                    break;
            }

            row.StatusStripeImage.color = accentColor;
            row.FillBarImage.color = fillColor;
            float fillRatio = Mathf.Clamp01(item.FillRatio);
            row.FillRatioCached = fillRatio;
            row.FillBarRect.anchorMin = new Vector2(0f, 0f);
            row.FillBarRect.anchorMax = new Vector2(fillRatio, 1f);
            row.FillBarRect.offsetMin = Vector2.zero;
            row.FillBarRect.offsetMax = Vector2.zero;

            float transitRatio = _config.ShowInTransitBar.Value ? item.InboundTransitRatio : 0f;
            row.TransitRatioCached = transitRatio;
            if (transitRatio > 0.001f)
            {
                if (!row.TransitBarRect.gameObject.activeSelf)
                {
                    row.TransitBarRect.gameObject.SetActive(true);
                }
                row.TransitBarImage.color = transitColor;
                row.TransitBarRect.anchorMin = new Vector2(fillRatio, 0f);
                row.TransitBarRect.anchorMax = new Vector2(Mathf.Clamp01(fillRatio + transitRatio), 1f);
                row.TransitBarRect.offsetMin = Vector2.zero;
                row.TransitBarRect.offsetMax = Vector2.zero;
            }
            else if (row.TransitBarRect.gameObject.activeSelf)
            {
                row.TransitBarRect.gameObject.SetActive(false);
            }

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
            if (item.IsHub)
            {
                string counterRole = item.Logic == ELogisticStorage.Supply ? "Imp" : "Exp";
                string counterMax = CombinedLogisticsItem.FormatAmount(item.HubCounterpartMax);
                slotDesc = $"{slotDesc} • <color=#5CE1FF>⇄ Hub ({counterRole} {counterMax})</color>";
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

            string bottleneckPrefix = item.HasLogisticsBottleneck
                ? "<color=#FFD24C>⚡+ILS</color> • "
                : "";

            string transitStr = "";
            if (item.InTransitOrder != 0)
            {
                string sign = item.InTransitOrder > 0 ? "+" : "";
                transitStr = $" ({sign}{CombinedLogisticsItem.FormatAmount(item.InTransitOrder)})";
            }

            row.SubStatusText.text = $"{bottleneckPrefix}{statusTag}{pct}%{transitStr}";
            row.SubStatusText.color = status == EHealthStatus.Healthy
                ? new Color(0.62f, 0.82f, 0.72f, 0.95f)
                : accentColor;

            int capturedItemId = item.ItemId;
            row.ShowMuteOnHover = false;
            row.MuteSlideProgress = 0f;
            ApplyRowMuteAnimationState(row);
            row.OnMuteClick = null;
            row.OnRowClick = () =>
            {
                CloseDropdownMenus();
                if (capturedItemId > 0)
                {
                    if (!_hasSavedFinderSnapshot)
                    {
                        _config.CopyHiddenItemsSnapshot(_lastGalaxySeed, _lastTargetAstroId, _savedFinderHiddenSnapshot);
                        _hasSavedFinderSnapshot = true;
                    }
                    _finderModeActive = true;
                    _config.SetCustomSingleItemOnly(_lastGalaxySeed, _lastTargetAstroId, capturedItemId);
                    if (_customPickerOpen)
                    {
                        SetCustomPickerTab(_customPickerActiveTab);
                    }
                    RefreshOptionsMenuState();
                    if (_scrollRect != null)
                    {
                        _scrollRect.verticalNormalizedPosition = 1f;
                    }
                    _forceRefresh = true;
                }
            };
        }

        private void RenderGalaxySearchSummary(GalaxyItemSearchSummary summary)
        {
            if (_panelCollapsed)
            {
                _titleText.text = "Stellogistics";
                _titleText.alignment = TextAnchor.MiddleCenter;
                _titleText.rectTransform.anchoredPosition = new Vector2(32f, -8f);
                _titleText.rectTransform.sizeDelta = new Vector2(-38f, 20f);
                _subtitleText.gameObject.SetActive(false);
                _fleetInfoText.gameObject.SetActive(false);
                _filterModeBtn.gameObject.SetActive(false);
                _finderModeBtn.gameObject.SetActive(false);
                _sortModeBtn.gameObject.SetActive(false);
                _optionsBtn.gameObject.SetActive(false);
                _generalOptionsBtn.gameObject.SetActive(false);
                _scrollRect.gameObject.SetActive(false);
                _resizeGripGo.SetActive(_isResizing);

                _headerRect.sizeDelta = new Vector2(0f, CollapsedTabHeight);
                _panelRect.sizeDelta = new Vector2(CollapsedTabWidth, CollapsedTabHeight);
                return;
            }

            float panelWidth = _config.PanelWidth.Value;
            float maxScrollHeight = _config.PanelMaxHeight.Value;

            float activeHeaderH = ApplyHeaderLayout(panelWidth, true);
            _subtitleText.gameObject.SetActive(true);
            _fleetInfoText.gameObject.SetActive(true);

            // In Finder mode, FilterModeBtn becomes "Pick Item" so you can easily pick another item or scan bottlenecks
            _filterModeBtn.gameObject.SetActive(true);
            _filterModeBtnText.text = "Pick Item";
            _filterModeBtn.GetComponent<Image>().color = _customPickerOpen
                ? new Color(0.24f, 0.46f, 0.70f, 0.98f)
                : new Color(0.15f, 0.26f, 0.39f, 0.96f);

            _finderModeBtn.gameObject.SetActive(true);
            _lastFinderCheckedCount = summary.CheckedItemCount;
            bool isSingleItemFocused = summary.CheckedItemCount == 1;
            if (isSingleItemFocused)
            {
                _finderModeBtnText.text = "← All";
                _finderModeBtn.GetComponent<Image>().color = new Color(0.26f, 0.50f, 0.74f, 0.98f);
            }
            else
            {
                _hasSavedFinderSnapshot = false;
                _finderModeBtnText.text = "← Back";
                _finderModeBtn.GetComponent<Image>().color = new Color(0.22f, 0.40f, 0.62f, 0.98f);
            }

            _sortModeBtn.gameObject.SetActive(true);
            _optionsBtn.gameObject.SetActive(true);
            _generalOptionsBtn.gameObject.SetActive(true);
            _resizeGripGo.SetActive(true);

            int totalIgnoredAstros = _config.GetIgnoredAstroCount(_lastGalaxySeed);
            string ignoredSuffix = summary.IgnoredPlanetMatchCount > 0
                ? $" • {summary.IgnoredPlanetMatchCount} Ignored"
                : (totalIgnoredAstros > 0 ? $" • {totalIgnoredAstros} Filtered" : "");

            bool onlyBottlenecks = _config.GetExclude100PercentForAstro(_lastGalaxySeed, 0) &&
                _config.ExcludeFullThresholdPercent.Value <= _config.WarningThresholdPercent.Value + 1;
            string pausedSuffix = _config.RefreshRate.Value == ERefreshRate.Paused
                ? " • <color=#FFD24C>⏸ Paused</color>"
                : "";
            _titleText.text = $"Finder: {summary.ItemName}";
            _subtitleText.text = (onlyBottlenecks && summary.CheckedItemCount != 1)
                ? $"Galaxy Scan • {summary.ExportingPlanets.Count} Export / {summary.ImportingPlanets.Count} Import Alerts{ignoredSuffix}{pausedSuffix}"
                : $"Galaxy Scan • {summary.ExportingPlanets.Count} Exporting • {summary.ImportingPlanets.Count} Importing{ignoredSuffix}{pausedSuffix}";
            _fleetInfoText.text = "Click row to focus & pin • [✕] to hide item";

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

            if (!summary.HasAnyLocations)
            {
                _exportsHeaderGo.SetActive(false);
                _importsHeaderGo.SetActive(false);
                HideRowsFrom(0);

                _emptyStateGo.SetActive(true);
                if (summary.CheckedItemCount == 0)
                {
                    _emptyStateText.text = "No items checked — click any item(s) in the Picker to locate.";
                }
                else if (summary.IgnoredPlanetMatchCount > 0)
                {
                    _emptyStateText.text = $"{summary.IgnoredPlanetMatchCount} matching planet(s) hidden by Planet Filter (Pick Item → Planets).";
                }
                else
                {
                    _emptyStateText.text = onlyBottlenecks
                        ? "All checked remote slots are Healthy (>40%)!"
                        : $"No planet exports or imports {summary.ItemName}.";
                }

                _activeContentHeight = MinScrollHeight;
                float emptyViewportHeight = Mathf.Max(MinScrollHeight, maxScrollHeight);
                SetScrollbarActive(false);
                _scrollContentRect.anchoredPosition = Vector2.zero;
                _scrollContentRect.sizeDelta = new Vector2(0f, _activeContentHeight);
                _scrollViewportRect.sizeDelta = new Vector2(0f, emptyViewportHeight);
                _panelRect.sizeDelta = new Vector2(panelWidth, activeHeaderH + emptyViewportHeight);
                _resizeGripGo.transform.SetAsLastSibling();
                return;
            }

            _emptyStateGo.SetActive(false);

            float estimatedHeight = 0f;
            if (summary.ExportingPlanets.Count > 0)
            {
                estimatedHeight += SectionHeaderHeight + RowSpacing;
                if (!_exportsCollapsed)
                {
                    estimatedHeight += summary.ExportingPlanets.Count * (RowHeight + RowSpacing);
                }
            }
            if (summary.ImportingPlanets.Count > 0)
            {
                if (estimatedHeight > 0f) estimatedHeight += 4f;
                estimatedHeight += SectionHeaderHeight + RowSpacing;
                if (!_importsCollapsed)
                {
                    estimatedHeight += summary.ImportingPlanets.Count * (RowHeight + RowSpacing);
                }
            }
            _activeContentHeight = Mathf.Max(MinScrollHeight, Mathf.Max(0f, estimatedHeight - RowSpacing));
            bool bothMenusCollapsed = _exportsCollapsed && _importsCollapsed;
            float viewportHeight = bothMenusCollapsed
                ? Mathf.Min(maxScrollHeight, _activeContentHeight)
                : Mathf.Max(MinScrollHeight, maxScrollHeight);
            bool needsScrollbar = _activeContentHeight > viewportHeight + 0.5f;
            float rightInset = needsScrollbar ? -(ScrollbarWidth + 1f) : 0f;
            float rowAvailableWidth = panelWidth + rightInset;

            float cursorY = 0f;
            int rowIndex = 0;
            int critPct = _config.CriticalThresholdPercent.Value;
            int warnPct = _config.WarningThresholdPercent.Value;

            if (summary.ExportingPlanets.Count > 0)
            {
                _exportsHeaderGo.SetActive(true);
                _exportsHeaderRect.anchoredPosition = new Vector2(0f, -cursorY);
                string expArrow = _exportsCollapsed ? "▶" : "▼";
                _exportsHeaderText.text = $"{expArrow} EXPORTING PLANETS ({summary.ExportingPlanets.Count})";
                _exportsAlertText.text = FormatAlertSummary(summary.CriticalExportPlanetCount, summary.WarningExportPlanetCount, "EMPTY", "LOW");
                _exportsAlertText.color = summary.CriticalExportPlanetCount > 0 ? CriticalColor : WarningColor;
                cursorY += SectionHeaderHeight + RowSpacing;

                if (!_exportsCollapsed)
                {
                    for (int i = 0; i < summary.ExportingPlanets.Count; i++)
                    {
                        ItemRowView row = GetOrCreateRow(rowIndex++);
                        row.RootRect.anchoredPosition = new Vector2(0f, -cursorY);
                        BindPlanetLocationRow(row, summary.ExportingPlanets[i], summary.IsBottleneckScan, rowAvailableWidth, critPct, warnPct);
                        cursorY += RowHeight + RowSpacing;
                    }
                }
            }
            else
            {
                _exportsHeaderGo.SetActive(false);
            }

            if (summary.ImportingPlanets.Count > 0)
            {
                if (cursorY > 0f)
                {
                    cursorY += 4f;
                }
                _importsHeaderGo.SetActive(true);
                _importsHeaderRect.anchoredPosition = new Vector2(0f, -cursorY);
                string impArrow = _importsCollapsed ? "▶" : "▼";
                _importsHeaderText.text = $"{impArrow} IMPORTING PLANETS ({summary.ImportingPlanets.Count})";
                _importsAlertText.text = FormatAlertSummary(summary.CriticalImportPlanetCount, summary.WarningImportPlanetCount, "STARVED", "LOW");
                _importsAlertText.color = summary.CriticalImportPlanetCount > 0 ? CriticalColor : WarningColor;
                cursorY += SectionHeaderHeight + RowSpacing;

                if (!_importsCollapsed)
                {
                    for (int i = 0; i < summary.ImportingPlanets.Count; i++)
                    {
                        ItemRowView row = GetOrCreateRow(rowIndex++);
                        row.RootRect.anchoredPosition = new Vector2(0f, -cursorY);
                        BindPlanetLocationRow(row, summary.ImportingPlanets[i], summary.IsBottleneckScan, rowAvailableWidth, critPct, warnPct);
                        cursorY += RowHeight + RowSpacing;
                    }
                }
            }
            else
            {
                _importsHeaderGo.SetActive(false);
            }

            HideRowsFrom(rowIndex);

            SetScrollbarActive(needsScrollbar);
            _scrollContentRect.anchoredPosition = new Vector2(0f, _scrollContentRect.anchoredPosition.y);
            _scrollContentRect.sizeDelta = new Vector2(rightInset, _activeContentHeight);
            _scrollViewportRect.sizeDelta = new Vector2(0f, viewportHeight);
            _panelRect.sizeDelta = new Vector2(panelWidth, activeHeaderH + viewportHeight);
            _resizeGripGo.transform.SetAsLastSibling();
        }

        private void BindPlanetLocationRow(
            ItemRowView row,
            PlanetItemLocationEntry entry,
            bool isBottleneckScan,
            float rowWidth,
            int critPct,
            int warnPct)
        {
            CombinedLogisticsItem item = entry.Item;
            EHealthStatus status = item.GetHealthStatus(critPct, warnPct);

            Color accentColor;
            Color fillColor;
            Color transitColor;
            switch (status)
            {
                case EHealthStatus.Critical:
                    accentColor = CriticalColor;
                    fillColor = CriticalFillColor;
                    transitColor = CriticalTransitColor;
                    break;
                case EHealthStatus.Warning:
                    accentColor = WarningColor;
                    fillColor = WarningFillColor;
                    transitColor = WarningTransitColor;
                    break;
                default:
                    accentColor = HealthyColor;
                    fillColor = HealthyFillColor;
                    transitColor = HealthyTransitColor;
                    break;
            }

            row.StatusStripeImage.color = accentColor;
            row.FillBarImage.color = fillColor;
            float fillRatio = Mathf.Clamp01(item.FillRatio);
            row.FillRatioCached = fillRatio;
            row.FillBarRect.anchorMin = new Vector2(0f, 0f);
            row.FillBarRect.anchorMax = new Vector2(fillRatio, 1f);
            row.FillBarRect.offsetMin = Vector2.zero;
            row.FillBarRect.offsetMax = Vector2.zero;

            float transitRatio = _config.ShowInTransitBar.Value ? item.InboundTransitRatio : 0f;
            row.TransitRatioCached = transitRatio;
            if (transitRatio > 0.001f)
            {
                if (!row.TransitBarRect.gameObject.activeSelf)
                {
                    row.TransitBarRect.gameObject.SetActive(true);
                }
                row.TransitBarImage.color = transitColor;
                row.TransitBarRect.anchorMin = new Vector2(fillRatio, 0f);
                row.TransitBarRect.anchorMax = new Vector2(Mathf.Clamp01(fillRatio + transitRatio), 1f);
                row.TransitBarRect.offsetMin = Vector2.zero;
                row.TransitBarRect.offsetMax = Vector2.zero;
            }
            else if (row.TransitBarRect.gameObject.activeSelf)
            {
                row.TransitBarRect.gameObject.SetActive(false);
            }

            row.IconImage.sprite = item.Icon;
            row.IconImage.enabled = item.Icon != null;

            string displayName = isBottleneckScan
                ? $"{entry.PlanetName} • {item.ItemName}"
                : $"▸ {entry.PlanetName}";
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
            if (item.IsHub)
            {
                string counterRole = item.Logic == ELogisticStorage.Supply ? "Imp" : "Exp";
                string counterMax = CombinedLogisticsItem.FormatAmount(item.HubCounterpartMax);
                string hubBadge = $"<color=#5CE1FF>⇄ Hub ({counterRole} {counterMax})</color>";
                bool starRedundant = rowWidth < 370f &&
                    !string.IsNullOrEmpty(entry.StarName) &&
                    !string.IsNullOrEmpty(entry.PlanetName) &&
                    entry.PlanetName.StartsWith(entry.StarName, StringComparison.OrdinalIgnoreCase);
                row.MetaText.text = starRedundant
                    ? $"{slotDesc} • {hubBadge}"
                    : $"{entry.StarName} • {slotDesc} • {hubBadge}";
            }
            else
            {
                row.MetaText.text = $"{entry.StarName} • {slotDesc}";
            }

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

            string bottleneckPrefix = item.HasLogisticsBottleneck
                ? "<color=#FFD24C>⚡+ILS</color> • "
                : "";

            string transitStr = "";
            if (item.InTransitOrder != 0)
            {
                string sign = item.InTransitOrder > 0 ? "+" : "";
                transitStr = $" ({sign}{CombinedLogisticsItem.FormatAmount(item.InTransitOrder)})";
            }

            row.SubStatusText.text = $"{bottleneckPrefix}{statusTag}{pct}%{transitStr}";
            row.SubStatusText.color = status == EHealthStatus.Healthy
                ? new Color(0.62f, 0.82f, 0.72f, 0.95f)
                : accentColor;

            row.ShowMuteOnHover = true;
            ApplyRowMuteAnimationState(row);

            PlanetData capturedPlanet = entry.Planet;
            int capturedItemId = item.ItemId;
            row.OnRowClick = () =>
            {
                CloseDropdownMenus();
                // When clicking a row in the multi-item Finder Overview, also offer focusing that single item or returning to overview
                if (isBottleneckScan && capturedItemId > 0)
                {
                    if (!_hasSavedFinderSnapshot)
                    {
                        _config.CopyHiddenItemsSnapshot(_lastGalaxySeed, _lastTargetAstroId, _savedFinderHiddenSnapshot);
                        _hasSavedFinderSnapshot = true;
                    }
                    _config.SetCustomSingleItemOnly(_lastGalaxySeed, _lastTargetAstroId, capturedItemId);
                    if (_customPickerOpen)
                    {
                        SetCustomPickerTab(_customPickerActiveTab);
                    }
                    RefreshOptionsMenuState();
                    if (_scrollRect != null)
                    {
                        _scrollRect.verticalNormalizedPosition = 1f;
                    }
                    _forceRefresh = true;
                }
                SelectAndNavigateToPlanet(capturedPlanet);
            };
            row.OnMuteClick = () =>
            {
                CloseDropdownMenus();
                if (capturedItemId > 0)
                {
                    if (_hasSavedFinderSnapshot)
                    {
                        _savedFinderHiddenSnapshot.Add(capturedItemId);
                        ReturnToFinderOverview();
                        return;
                    }
                    _config.SetItemVisibleForAstro(_lastGalaxySeed, _lastTargetAstroId, capturedItemId, false);
                    RefreshCustomPickerCells();
                    RefreshOptionsMenuState();
                    _forceRefresh = true;
                }
            };
        }

        private void SelectAndNavigateToPlanet(PlanetData planet)
        {
            if (planet == null)
            {
                return;
            }

            // 1. Set the player's navigation indicator (yellow guidance line + Starmap pin)
            if (GameMain.mainPlayer?.navigation != null)
            {
                GameMain.mainPlayer.navigation.indicatorAstroId = planet.astroId;
            }

            // 2. Focus and center the planet on the Starmap
            if (_currentStarmap != null)
            {
                UIStarmapPlanet targetNode = null;
                if (_currentStarmap.planetUIs != null)
                {
                    for (int i = 0; i < _currentStarmap.planetUIs.Length; i++)
                    {
                        UIStarmapPlanet pui = _currentStarmap.planetUIs[i];
                        if (pui != null && pui.planet == planet)
                        {
                            targetNode = pui;
                            break;
                        }
                    }
                }

                UIStarmapStar starNode = null;
                if (planet.star != null && _currentStarmap.starUIs != null)
                {
                    for (int i = 0; i < _currentStarmap.starUIs.Length; i++)
                    {
                        UIStarmapStar sui = _currentStarmap.starUIs[i];
                        if (sui != null && sui.star == planet.star)
                        {
                            starNode = sui;
                            break;
                        }
                    }
                }

                if (targetNode != null)
                {
                    _currentStarmap.focusPlanet = targetNode;
                    _currentStarmap.focusStar = null;
                    _currentStarmap.OnCursorFunction2Click(0);
                }
                else if (starNode != null)
                {
                    _currentStarmap.focusStar = starNode;
                    _currentStarmap.focusPlanet = null;
                    _currentStarmap.OnCursorFunction2Click(0);
                }
            }
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
