using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Stellogistics
{
    public partial class UIStarmapLogisticsPanel : MonoBehaviour
    {
        private class ItemRowView
        {
            public GameObject RootGo;
            public RectTransform RootRect;
            public Image BgImage;
            public Button RowButton;
            public PointerHoverTracker RowHoverTracker;
            public GameObject MuteBtnGo;
            public RectTransform MuteBtnRect;
            public Image MuteBtnBg;
            public Text MuteBtnText;
            public PointerHoverTracker MuteHoverTracker;
            public bool ShowMuteOnHover;
            public float MuteSlideProgress;
            public RectTransform FillBarRect;
            public Image FillBarImage;
            public RectTransform TransitBarRect;
            public Image TransitBarImage;
            public Image StatusStripeImage;
            public Image IconImage;
            public Text NameText;
            public Text MetaText;
            public Text AmountText;
            public Text SubStatusText;

            // Bound row context (eliminates per-refresh closure allocations)
            public int BoundItemId;
            public PlanetData BoundPlanet;
            public bool BoundIsFinderRow;
            public bool BoundIsBottleneckScan;
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
            public int BoundAstroId;
            public int BoundParentStarAstroId;
            public bool BoundIsStarSystem;
        }

        private class CheckboxToggleRow
        {
            public Func<bool> Getter;
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

        private struct TrackedPanelText
        {
            public Text Text;
            public FontStyle BaseStyle;
        }

        private StellogisticsConfig _config;
        private UIGame _uiGame;
        private Font _gameFont;
        private readonly List<TrackedPanelText> _allPanelTexts = new List<TrackedPanelText>();
        private int _languageSwitchWarmupFrames;

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
        private Image _filterModeBtnBg;
        private Text _filterModeBtnText;
        private Button _finderModeBtn;
        private Image _finderModeBtnBg;
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
        private Text _optionsMenuTitleText;
        private Text _optionsTargetLabel;
        private Text _excludeRowLabelText;
        private Text _excludeThresholdBtnText;
        private Text _resetFiltersBtnText;
        private Text _customPickerBtnText;
        private CheckboxToggleRow _ignoreTargetToggleRow;
        private readonly List<CheckboxToggleRow> _filterToggles = new List<CheckboxToggleRow>();

        private GameObject _generalOptionsMenuGo;
        private RectTransform _generalOptionsMenuRect;
        private Text _generalOptionsMenuTitleText;
        private Text _refreshRateTitleText;
        private Text _resetPosBtnText;
        private readonly List<CheckboxToggleRow> _generalOptionToggles = new List<CheckboxToggleRow>();
        private readonly Image[] _refreshBtnBgs = new Image[4];
        private readonly Text[] _refreshBtnTxts = new Text[4];

        private GameObject _customPickerGo;
        private RectTransform _customPickerRect;
        private GameObject _customPickerGridGo;
        private GameObject _customPickerPlanetsScrollGo;
        private RectTransform _customPickerPlanetsContentRect;
        private GameObject _customPickerPlanetsEmptyGo;
        private Text _customPickerPlanetsEmptyText;
        private Text _customPickerTitleText;
        private Text _customPickerHoveredLabel;
        private Image _customTabItemsBg;
        private Text _customTabItemsText;
        private Image _customTabBuildingsBg;
        private Text _customTabBuildingsText;
        private Image _customTabPlanetsBg;
        private Text _customTabPlanetsText;
        private Button _pickerCheckAllBtn;
        private Text _pickerCheckAllText;
        private Button _pickerUncheckAllBtn;
        private Text _pickerUncheckAllText;
        private Button _pickerShowAllPlanetsBtn;
        private Text _pickerShowAllPlanetsText;
        private int _customPickerActiveTab = 1;
        private bool _customPickerOpen = false;
        private readonly List<CustomPickerCell> _customPickerCells = new List<CustomPickerCell>();
        private readonly List<PlanetFilterRowView> _planetFilterRowPool = new List<PlanetFilterRowView>();
        private readonly List<PlanetLogisticsFilterEntry> _planetFilterEntries = new List<PlanetLogisticsFilterEntry>();
        private readonly HashSet<int> _storedItemsSet = new HashSet<int>();

        private readonly List<ItemRowView> _rowPool = new List<ItemRowView>();

        private struct FinderHistoryEntry
        {
            public int FocusItemId;
            public FilterSnapshot Filter;
        }

        private UIStarmap _currentStarmap;
        private bool _finderModeActive = false;
        private int _finderFocusItemId = 0;
        private int _lastFinderCheckedCount = 0;
        private readonly List<FinderHistoryEntry> _finderHistoryStack = new List<FinderHistoryEntry>();

        private bool ShowFifthHeaderButton => !_finderModeActive && (_lastResolvedStar != null && _lastResolvedPlanet == null);

        private int _lastTargetAstroId = -1;
        private int _lastGalaxySeed = 0;
        private int _lastLanguageLcid = -1;
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
        private float _livePanelWidth = StellogisticsConfig.DefaultWidth;
        private float _livePanelMaxHeight = StellogisticsConfig.DefaultMaxHeight;

        private static readonly Color PanelBgColor = new Color(0.055f, 0.085f, 0.13f, 0.94f);
        private static readonly Color HeaderBgColor = new Color(0.09f, 0.145f, 0.22f, 0.98f);
        private static readonly Color SectionHeaderBgColor = new Color(0.12f, 0.19f, 0.28f, 0.95f);
        private static readonly Color RowEvenBgColor = new Color(0.08f, 0.12f, 0.18f, 0.78f);
        private static readonly Color RowOddBgColor = new Color(0.065f, 0.10f, 0.15f, 0.78f);

        private static readonly Color CriticalColor = new Color(1.0f, 0.34f, 0.34f, 1f);
        private static readonly Color CriticalTextColor = new Color(1.0f, 0.66f, 0.66f, 1f);
        private static readonly Color CriticalFillColor = new Color(0.80f, 0.16f, 0.18f, 0.24f);
        private static readonly Color CriticalTransitColor = new Color(0.72f, 0.18f, 0.22f, 0.22f);
        private static readonly Color WarningColor = new Color(1.0f, 0.70f, 0.24f, 1f);
        private static readonly Color WarningTextColor = new Color(1.0f, 0.85f, 0.48f, 1f);
        private static readonly Color WarningFillColor = new Color(0.80f, 0.48f, 0.10f, 0.22f);
        private static readonly Color WarningTransitColor = new Color(0.72f, 0.44f, 0.10f, 0.20f);
        private static readonly Color HealthyColor = new Color(0.30f, 0.90f, 0.58f, 1f);
        private static readonly Color HealthySubTextColor = new Color(0.82f, 0.97f, 0.89f, 0.98f);
        private static readonly Color HealthyFillColor = new Color(0.16f, 0.62f, 0.38f, 0.20f);
        private static readonly Color HealthyTransitColor = new Color(0.16f, 0.58f, 0.38f, 0.19f);

        private static readonly (Color accent, Color text, Color fill, Color transit)[] StatusPalette =
        {
            (CriticalColor, CriticalTextColor, CriticalFillColor, CriticalTransitColor),
            (WarningColor, WarningTextColor, WarningFillColor, WarningTransitColor),
            (HealthyColor, HealthySubTextColor, HealthyFillColor, HealthyTransitColor)
        };

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

        private static int CurrentGalaxySeed => GameMain.galaxy != null ? GameMain.galaxy.seed : 0;

        private Font ResolveCurrentGameFont()
        {
            Font font = null;
            if (_uiGame?.starmap?.cursorViewText != null)
            {
                font = _uiGame.starmap.cursorViewText.font;
            }
            if (font == null && _uiGame?.planetDetail?.typeText != null)
            {
                font = _uiGame.planetDetail.typeText.font;
            }
            if (font == null && _uiGame?.controlPanelWindow != null)
            {
                font = _uiGame.controlPanelWindow.FONT_SAIRASB;
            }
            if (font == null)
            {
                font = Resources.Load<Font>("UI/Fonts/SAIRASB");
            }
            if (font == null)
            {
                font = Resources.Load<Font>("UI/Fonts/SAIRAR");
            }
            if (font == null && Configs.builtin != null)
            {
                font = Configs.builtin.markerBreifInfoWordFont;
            }
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return font;
        }

        public void Initialize(UIGame uiGame, StellogisticsConfig config)
        {
            _uiGame = uiGame;
            _config = config;
            _livePanelWidth = _config.PanelWidth.Value;
            _livePanelMaxHeight = _config.PanelMaxHeight.Value;

            _gameFont = ResolveCurrentGameFont();

            BuildUI();
            BuildOptionsMenu();
            BuildGeneralOptionsMenu();
            BuildCustomItemPicker();
            _lastLanguageLcid = Localization.CurrentLanguageLCID;
            ApplyLocalizedStaticLabels();
            _languageSwitchWarmupFrames = 2;
        }

        public void OnStarmapOpened()
        {
            int seed = CurrentGalaxySeed;
            if (seed != _lastGalaxySeed)
            {
                _lastGalaxySeed = seed;
                _lastTargetAstroId = -1;
                _lastResolvedPlanet = null;
                _lastResolvedStar = null;
                _finderFocusItemId = 0;
                _finderHistoryStack.Clear();
                LogisticsAggregator.InvalidateCaches();
            }
            RequestImmediateRefresh();
        }

        public void RequestImmediateRefresh()
        {
            _forceRefresh = true;
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

            int seed = CurrentGalaxySeed;
            if (seed != _lastGalaxySeed)
            {
                _lastGalaxySeed = seed;
                _lastTargetAstroId = -1;
                _lastResolvedPlanet = null;
                _lastResolvedStar = null;
                _finderFocusItemId = 0;
                _finderHistoryStack.Clear();
                LogisticsAggregator.InvalidateCaches();
                _forceRefresh = true;
            }

            int currentLcid = Localization.CurrentLanguageLCID;
            bool languageChanged = currentLcid != _lastLanguageLcid;
            if (languageChanged)
            {
                _lastLanguageLcid = currentLcid;
                _gameFont = ResolveCurrentGameFont();
                LogisticsAggregator.InvalidateCaches();
                _forceRefresh = true;
                _languageSwitchWarmupFrames = 3;
            }

            ResolveTarget(starmap, out PlanetData targetPlanet, out StarData targetStar);

            if (!_finderModeActive && targetPlanet == null && targetStar == null)
            {
                if (languageChanged)
                {
                    ApplyLocalizedStaticLabels();
                }
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

            if (languageChanged)
            {
                ApplyLocalizedStaticLabels();
            }

            UpdatePanelPosition();
            CheckAutoCloseDropdownMenus();
            UpdateRowHoverAnimations();

            if (!_finderModeActive && (targetPlanet != null || targetStar != null))
            {
                int currentAstroId = targetPlanet != null ? targetPlanet.astroId : targetStar.astroId;
                if (currentAstroId != _lastTargetAstroId)
                {
                    _lastTargetAstroId = currentAstroId;
                    _forceRefresh = true;
                    if (_scrollRect != null)
                    {
                        _scrollRect.verticalNormalizedPosition = 1f;
                    }
                    if (_optionsMenuOpen)
                    {
                        RefreshOptionsMenuState();
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
                    ERefreshRate.Slow => 10.0f,
                    _ => 1.00f
                };
                _nextRefreshTime = now + interval;

                if (_finderModeActive)
                {
                    GalaxyItemSearchSummary searchSummary = LogisticsAggregator.ScanGalaxyFinder(_config, _finderFocusItemId);
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

            if (_languageSwitchWarmupFrames > 0)
            {
                _languageSwitchWarmupFrames--;
                PrewarmAllPanelTexts();
            }
        }

        private void ResolveTarget(UIStarmap starmap, out PlanetData targetPlanet, out StarData targetStar)
        {
            targetPlanet = null;
            targetStar = null;

            // Do not switch target based on background 3D hover raycasts while the cursor is interacting with our UI
            if (IsPointerOverUI() && (_lastResolvedPlanet != null || _lastResolvedStar != null))
            {
                targetPlanet = _lastResolvedPlanet;
                targetStar = _lastResolvedStar;
                return;
            }

            if (_config.ShowOnHover.Value)
            {
                if (starmap.mouseHoverPlanet?.planet != null)
                {
                    targetPlanet = starmap.mouseHoverPlanet.planet;
                    return;
                }
                if (starmap.mouseHoverStar?.star != null)
                {
                    targetStar = starmap.mouseHoverStar.star;
                    return;
                }
            }

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

            if (starmap.focusPlanet?.planet != null)
            {
                targetPlanet = starmap.focusPlanet.planet;
                return;
            }
            if (starmap.focusStar?.star != null)
            {
                targetStar = starmap.focusStar.star;
                return;
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

        private void SelectAndNavigateToPlanet(PlanetData planet)
        {
            if (planet == null)
            {
                return;
            }

            if (GameMain.mainPlayer?.navigation != null)
            {
                GameMain.mainPlayer.navigation.indicatorAstroId = planet.astroId;
            }

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
    }
}
