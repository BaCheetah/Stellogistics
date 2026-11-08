using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Stellogistics
{
    public partial class UIStarmapLogisticsPanel
    {
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
                    int diag = (x + y) & 7;
                    float alpha = diag < 4 ? 1.0f : 0.12f;
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

        private void BuildUI()
        {
            _panelRect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            Vector2 initialPos = _config.UseCustomPosition.Value
                ? new Vector2(_config.CustomPanelPosX.Value, _config.CustomPanelPosY.Value)
                : new Vector2(StellogisticsConfig.DefaultPosX, StellogisticsConfig.DefaultPosY);
            _panelRect.SetTopLeft(initialPos, new Vector2(_livePanelWidth, 240f));

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
            _headerRect = headerGo.GetComponent<RectTransform>().SetTopStretch(Vector2.zero, new Vector2(0f, HeaderHeight));
            Image headerImg = headerGo.GetComponent<Image>();
            headerImg.color = HeaderBgColor;
            headerImg.raycastTarget = true;

            PanelDragHandler dragHandler = headerGo.GetComponent<PanelDragHandler>();
            dragHandler.TargetRect = _panelRect;
            dragHandler.OnDragStarted = () => CloseDropdownMenus();
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
            _titleText.rectTransform.SetTopStretch(new Vector2(36f, -8f), new Vector2(-314f, 20f), 0f);
            _titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _titleText.verticalOverflow = VerticalWrapMode.Truncate;
            _titleText.resizeTextForBestFit = true;
            _titleText.resizeTextMinSize = 10;
            _titleText.resizeTextMaxSize = 13;

            // Top-right Header Buttons
            _filterModeBtn = CreateHeaderButton("FilterModeBtn", _headerRect, new Vector2(-222f, -8f), new Vector2(50f, 20f), true, out _filterModeBtnText);
            _filterModeBtnBg = _filterModeBtn.GetComponent<Image>();
            _filterModeBtn.onClick.AddListener(OnFilterModeButtonClicked);

            _finderModeBtn = CreateHeaderButton("FinderModeBtn", _headerRect, new Vector2(-172f, -8f), new Vector2(46f, 20f), true, out _finderModeBtnText);
            _finderModeBtnBg = _finderModeBtn.GetComponent<Image>();
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
            _subtitleText.rectTransform.SetTopStretch(new Vector2(10f, -33f), new Vector2(-20f, 18f), 0f);

            // Fleet / Warper Info (stealthily clickable to toggle Vessels between 'on route' and 'idle')
            _fleetInfoText = CreateText("FleetInfoText", _headerRect, 11, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.56f, 0.72f, 0.85f, 0.95f));
            _fleetInfoText.rectTransform.SetTopStretch(new Vector2(10f, -52f), new Vector2(-20f, 18f), 0f);
            _fleetInfoText.raycastTarget = true;
            PointerClickHandler fleetClick = _fleetInfoText.gameObject.AddComponent<PointerClickHandler>();
            fleetClick.OnClick = () =>
            {
                if (_finderModeActive)
                {
                    return;
                }
                CloseDropdownMenus();
                _config.ShowIdleVessels.Value = !_config.ShowIdleVessels.Value;
                _config.Save();
                _forceRefresh = true;
            };

            // Scroll View
            GameObject scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGo.transform.SetParent(_panelRect, false);
            _scrollViewportRect = scrollGo.GetComponent<RectTransform>().SetTopStretch(new Vector2(0f, -HeaderHeight), new Vector2(0f, 160f));

            Image maskImg = scrollGo.GetComponent<Image>();
            maskImg.color = new Color(0f, 0f, 0f, 0.01f);
            maskImg.raycastTarget = true;
            scrollGo.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(_scrollViewportRect, false);
            _scrollContentRect = contentGo.GetComponent<RectTransform>().SetTopStretch(Vector2.zero, new Vector2(0f, 160f), 0f);

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
            RectTransform emptyRect = _emptyStateGo.GetComponent<RectTransform>().SetTopStretch(new Vector2(0f, -8f), new Vector2(-20f, 40f));
            _emptyStateText = CreateText("EmptyText", emptyRect, 12, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.60f, 0.72f, 0.82f, 0.95f));
            _emptyStateText.rectTransform.StretchFull();
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

            BuildResizeGrip();
        }

        private void BuildScrollbar()
        {
            _scrollbarGo = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            _scrollbarGo.transform.SetParent(_scrollViewportRect, false);
            RectTransform sbRect = _scrollbarGo.GetComponent<RectTransform>()
                .SetRect(new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(ScrollbarWidth, -14f));

            Image trackImg = _scrollbarGo.GetComponent<Image>();
            trackImg.color = new Color(0.03f, 0.05f, 0.09f, 0.96f);
            trackImg.raycastTarget = true;

            GameObject slidingAreaGo = new GameObject("SlidingArea", typeof(RectTransform));
            slidingAreaGo.transform.SetParent(sbRect, false);
            RectTransform slidingRect = slidingAreaGo.GetComponent<RectTransform>().Stretch(new Vector2(1f, 1f), new Vector2(0f, -1f));

            Image handleImg = UIHelpers.CreateImage("Handle", slidingRect, new Color(0.28f, 0.54f, 0.78f, 0.90f), true, out RectTransform handleRect);
            handleRect.StretchFull();

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
            RectTransform gripRect = _resizeGripGo.GetComponent<RectTransform>()
                .SetRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(16f, 16f));

            Image gripBg = _resizeGripGo.GetComponent<Image>();
            gripBg.color = new Color(0f, 0f, 0f, 0.01f);
            gripBg.raycastTarget = true;

            Text gripIcon = CreateText("GripIcon", gripRect, 11, FontStyle.Bold, TextAnchor.LowerRight, new Color(0.55f, 0.82f, 1f, 0.85f));
            gripIcon.rectTransform.Stretch(new Vector2(0f, -3f), new Vector2(-1f, -3f));
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
                float desiredViewportH = newTotalH - HeaderHeight;

                // Stage 1: Dragged high up into the header -> collapse whole panel into Tabbed Mode
                if (desiredViewportH <= 10f)
                {
                    if (!_panelCollapsed)
                    {
                        _panelCollapsed = true;
                        _panelCollapseBtnText.text = "▶";
                        CloseDropdownMenus();
                        if (_customPickerOpen && _customPickerGo != null)
                        {
                            _customPickerOpen = false;
                            _customPickerGo.SetActive(false);
                        }
                        _forceRefresh = true;
                    }
                    ApplyCollapsedTabLayout();
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
                    _livePanelWidth = newW;
                    if (!_panelCollapsed)
                    {
                        ApplyCurrentLayoutSizes(_livePanelWidth, _livePanelMaxHeight);
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

                _livePanelWidth = newW;
                bool showFifthBtn = !_finderModeActive && (_lastResolvedStar != null && _lastResolvedPlanet == null);
                float activeHeaderH = ApplyHeaderLayout(_livePanelWidth, showFifthBtn);
                float clampedViewportH = Mathf.Clamp(newTotalH - activeHeaderH, 82f, 950f);
                _livePanelMaxHeight = clampedViewportH;
                ApplyCurrentLayoutSizes(_livePanelWidth, clampedViewportH);
            };
            resizeHandler.OnResizeFinished = (newW, newTotalH) =>
            {
                _isResizing = false;
                float savedMaxH = Mathf.Max(82f, _livePanelMaxHeight);
                _livePanelMaxHeight = savedMaxH;
                _config.SavePanelSize(_livePanelWidth, savedMaxH);
                _forceRefresh = true;
            };
        }

        private void ApplyCollapsedTabLayout()
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
                _titleText.rectTransform.sizeDelta = new Vector2(-148f, 20f);
                optBtnRect.anchoredPosition = new Vector2(-62f, -8f);
                genOptBtnRect.anchoredPosition = new Vector2(-6f, -8f);

                filterBtnRect.anchoredPosition = new Vector2(-116f, -32f);
                finderBtnRect.anchoredPosition = new Vector2(-66f, -32f);
                sortBtnRect.anchoredPosition = new Vector2(-6f, -32f);

                _subtitleText.rectTransform.anchoredPosition = new Vector2(10f, -57f);
                _fleetInfoText.rectTransform.anchoredPosition = new Vector2(10f, -76f);
            }
            else
            {
                _titleText.rectTransform.sizeDelta = new Vector2(showFifthButton ? -314f : -260f, 20f);

                filterBtnRect.anchoredPosition = new Vector2(-222f, -8f);
                finderBtnRect.anchoredPosition = new Vector2(-172f, -8f);
                sortBtnRect.anchoredPosition = new Vector2(-112f, -8f);
                optBtnRect.anchoredPosition = new Vector2(-62f, -8f);
                genOptBtnRect.anchoredPosition = new Vector2(-6f, -8f);

                _subtitleText.rectTransform.anchoredPosition = new Vector2(10f, -33f);
                _fleetInfoText.rectTransform.anchoredPosition = new Vector2(10f, -52f);
            }

            if (_optionsMenuRect != null) _optionsMenuRect.anchoredPosition = new Vector2(-4f, -32f);
            if (_generalOptionsMenuRect != null) _generalOptionsMenuRect.anchoredPosition = new Vector2(-4f, -32f);

            return currentHeaderH;
        }

        private void ApplyCurrentLayoutSizes(float panelWidth, float maxScrollHeight)
        {
            if (_panelCollapsed)
            {
                return;
            }

            bool showFifthBtn = !_finderModeActive && (_lastResolvedStar != null && _lastResolvedPlanet == null);
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
            float ax = anchorRight ? 1f : 0f;
            RectTransform rect = btnGo.GetComponent<RectTransform>()
                .SetRect(new Vector2(ax, 1f), new Vector2(ax, 1f), new Vector2(ax, 1f), anchoredPos, size);

            btnGo.GetComponent<Image>().color = new Color(0.15f, 0.26f, 0.39f, 0.96f);

            Outline outline = btnGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.30f, 0.54f, 0.78f, 0.45f);
            outline.effectDistance = new Vector2(1f, -1f);

            Button btn = btnGo.GetComponent<Button>();
            UIHelpers.ApplyButtonTint(btn, new Color(1.30f, 1.35f, 1.42f, 1f), new Color(0.80f, 0.85f, 0.92f, 1f));

            btnText = CreateText("Text", rect, 10, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.90f, 0.97f, 1f, 1f));
            btnText.rectTransform.StretchFull();

            return btn;
        }

        private void CreateSectionHeader(
            string name,
            UnityAction onClickAction,
            out GameObject headerGo,
            out RectTransform headerRect,
            out Text titleText,
            out Text alertText)
        {
            headerGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            headerGo.transform.SetParent(_scrollContentRect, false);
            headerRect = headerGo.GetComponent<RectTransform>().SetTopStretch(Vector2.zero, new Vector2(0f, SectionHeaderHeight));

            Image bg = headerGo.GetComponent<Image>();
            bg.color = SectionHeaderBgColor;
            bg.raycastTarget = true;

            Button btn = headerGo.GetComponent<Button>();
            UIHelpers.ApplyButtonTint(btn, new Color(1.25f, 1.30f, 1.38f, 1f), new Color(0.82f, 0.87f, 0.94f, 1f));
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
            RectTransform rowRect = rowGo.GetComponent<RectTransform>().SetTopStretch(Vector2.zero, new Vector2(0f, RowHeight));

            PointerHoverTracker rowHoverTracker = rowGo.GetComponent<PointerHoverTracker>();

            Image bgImg = rowGo.GetComponent<Image>();
            bgImg.color = (index % 2 == 0) ? RowEvenBgColor : RowOddBgColor;

            Button rowBtn = rowGo.GetComponent<Button>();
            UIHelpers.ApplyButtonTint(rowBtn, new Color(1.22f, 1.28f, 1.38f, 1f), new Color(0.85f, 0.90f, 0.98f, 1f));
            rowBtn.targetGraphic = bgImg;

            Image fillImg = UIHelpers.CreateImage("FillBar", rowRect, HealthyFillColor, false, out RectTransform fillRect);
            fillRect.SetRect(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

            Image transitImg = UIHelpers.CreateImage("TransitBar", rowRect, HealthyTransitColor, false, out RectTransform transitRect);
            transitRect.SetRect(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            transitImg.sprite = GetOrCreateHatchedTransitSprite();
            transitImg.type = Image.Type.Tiled;
            transitRect.gameObject.SetActive(false);

            Image stripeImg = UIHelpers.CreateImage("StatusStripe", rowRect, HealthyColor, false, out RectTransform stripeRect);
            stripeRect.SetRect(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 0f));

            Image iconImg = UIHelpers.CreateImage("Icon", rowRect, Color.white, false, out RectTransform iconRect);
            iconRect.SetRect(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(28f, 28f));
            iconImg.preserveAspect = true;
            PointerClickHandler iconClick = iconImg.gameObject.AddComponent<PointerClickHandler>();
            ItemCellHoverHandler iconHover = iconImg.gameObject.AddComponent<ItemCellHoverHandler>();

            Text nameText = CreateText("NameText", rowRect, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.95f, 0.98f, 1f, 1f));
            nameText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            nameText.rectTransform.anchorMax = new Vector2(0.64f, 1f);
            nameText.rectTransform.offsetMin = new Vector2(44f, 0f);
            nameText.rectTransform.offsetMax = new Vector2(-4f, -1f);
            AddRowTextShadow(nameText);

            Text metaText = CreateText("MetaText", rowRect, 10, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.68f, 0.82f, 0.94f, 0.98f));
            metaText.rectTransform.anchorMin = new Vector2(0f, 0f);
            metaText.rectTransform.anchorMax = new Vector2(0.64f, 0.5f);
            metaText.rectTransform.offsetMin = new Vector2(44f, 1f);
            metaText.rectTransform.offsetMax = new Vector2(-4f, 0f);
            AddRowTextShadow(metaText);

            Text amountText = CreateText("AmountText", rowRect, 12, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);
            amountText.rectTransform.anchorMin = new Vector2(0.55f, 0.5f);
            amountText.rectTransform.anchorMax = new Vector2(1f, 1f);
            amountText.rectTransform.offsetMin = new Vector2(0f, 0f);
            amountText.rectTransform.offsetMax = new Vector2(-10f, -1f);
            AddRowTextShadow(amountText);

            Text subStatusText = CreateText("SubStatusText", rowRect, 10, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.7f, 0.82f, 0.92f, 1f));
            subStatusText.rectTransform.anchorMin = new Vector2(0.55f, 0f);
            subStatusText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            subStatusText.rectTransform.offsetMin = new Vector2(0f, 1f);
            subStatusText.rectTransform.offsetMax = new Vector2(-10f, 0f);
            AddRowTextShadow(subStatusText);

            // Hover-revealed [✕] button on rows with slide-in animation
            GameObject muteBtnGo = new GameObject("MutePlanetBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PointerHoverTracker), typeof(ItemCellHoverHandler));
            muteBtnGo.transform.SetParent(rowRect, false);
            RectTransform muteRect = muteBtnGo.GetComponent<RectTransform>()
                .SetRect(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(10f, 0f), new Vector2(15f, 15f));

            PointerHoverTracker muteHoverTracker = muteBtnGo.GetComponent<PointerHoverTracker>();

            Image muteBg = muteBtnGo.GetComponent<Image>();
            muteBg.color = new Color(0.14f, 0.22f, 0.32f, 0f);

            Button muteBtn = muteBtnGo.GetComponent<Button>();
            UIHelpers.ApplyButtonTint(muteBtn, Color.white, new Color(0.82f, 0.72f, 0.72f, 1f));

            Text muteTxt = CreateText("Text", muteRect, 9, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.76f, 0.88f, 0.98f, 0f));
            muteTxt.rectTransform.StretchFull();
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

            iconHover.OnHoverEnter = () =>
            {
                if (view.BoundIsFinderRow && view.IconImage.raycastTarget)
                {
                    view.IconImage.rectTransform.localScale = new Vector3(1.16f, 1.16f, 1f);
                }
            };
            iconHover.OnHoverExit = () =>
            {
                view.IconImage.rectTransform.localScale = Vector3.one;
            };
            iconClick.OnClick = () => OnBoundRowIconClicked(view);

            rowBtn.onClick.AddListener(() => OnBoundRowClicked(view));
            muteBtn.onClick.AddListener(() => OnBoundRowMuteClicked(view));
            return view;
        }

        private void OnBoundRowIconClicked(ItemRowView row)
        {
            CloseDropdownMenus();
            row.IconImage.rectTransform.localScale = Vector3.one;
            if (!row.BoundIsFinderRow || row.BoundItemId <= 0)
            {
                return;
            }

            System.Collections.Generic.List<int> sourceIds = LogisticsAggregator.GetSourceIngredientIds(row.BoundItemId);
            if (sourceIds == null || sourceIds.Count == 0)
            {
                return;
            }

            _finderFocusItemId = 0;
            _finderModeActive = true;
            _config.SaveFilterSnapshotIfNone();
            _config.SetOnlyItemsVisible(sourceIds);

            RefreshCustomPickerCells();
            RefreshOptionsMenuState();

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
            _forceRefresh = true;
        }

        private void OnBoundRowClicked(ItemRowView row)
        {
            CloseDropdownMenus();
            if (row.BoundIsFinderRow)
            {
                if (row.BoundIsBottleneckScan && row.BoundItemId > 0)
                {
                    _finderFocusItemId = row.BoundItemId;
                    if (_scrollRect != null)
                    {
                        _scrollRect.verticalNormalizedPosition = 1f;
                    }
                    _forceRefresh = true;
                }
                SelectAndNavigateToPlanet(row.BoundPlanet);
            }
            else if (row.BoundItemId > 0)
            {
                _finderModeActive = true;
                _finderFocusItemId = row.BoundItemId;
                if (_scrollRect != null)
                {
                    _scrollRect.verticalNormalizedPosition = 1f;
                }
                _forceRefresh = true;
            }
        }

        private void OnBoundRowMuteClicked(ItemRowView row)
        {
            CloseDropdownMenus();
            row.MuteSlideProgress = 0f;
            if (row.BoundItemId <= 0)
            {
                return;
            }

            _config.SetItemVisible(row.BoundItemId, false);
            if (_finderFocusItemId == row.BoundItemId)
            {
                _finderFocusItemId = 0;
            }
            RefreshCustomPickerCells();
            RefreshOptionsMenuState();
            _forceRefresh = true;
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
                row.MuteBtnBg.color = muteDirectHover
                    ? new Color(0.54f, 0.16f, 0.22f, 0.95f * eased)
                    : new Color(0.06f, 0.10f, 0.16f, 0.86f * eased);
                row.MuteBtnText.color = muteDirectHover
                    ? new Color(1f, 0.86f, 0.88f, 1f * eased)
                    : new Color(0.86f, 0.94f, 1f, 0.95f * eased);
            }
        }

        private static void AddRowTextShadow(Text txt)
        {
            if (txt == null)
            {
                return;
            }
            Shadow shadow = txt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.01f, 0.03f, 0.06f, 0.88f);
            shadow.effectDistance = new Vector2(1f, -1f);
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
    }
}
