using UnityEngine;
using UnityEngine.UI;

namespace Stellogistics
{
    public partial class UIStarmapLogisticsPanel
    {
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
            _customPickerRect = _customPickerGo.GetComponent<RectTransform>()
                .SetRect(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(8f, 0f), new Vector2(winWidth, winHeight));

            _customPickerHoverTracker = _customPickerGo.GetComponent<PointerHoverTracker>();

            Image winBg = _customPickerGo.GetComponent<Image>();
            winBg.color = new Color(0.06f, 0.10f, 0.15f, 0.98f);
            winBg.raycastTarget = true;

            Outline outline = _customPickerGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.32f, 0.68f, 0.95f, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Draggable header bar
            GameObject headerGo = new GameObject("PickerHeader", typeof(RectTransform), typeof(Image), typeof(PanelDragHandler));
            headerGo.transform.SetParent(_customPickerRect, false);
            RectTransform headerRect = headerGo.GetComponent<RectTransform>().SetTopStretch(Vector2.zero, new Vector2(0f, 28f));

            Image headerBg = headerGo.GetComponent<Image>();
            headerBg.color = HeaderBgColor;
            headerBg.raycastTarget = true;

            headerGo.GetComponent<PanelDragHandler>().TargetRect = _customPickerRect;

            _customPickerTitleText = CreateText("PickerTitle", headerRect, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.97f, 1f, 1f));
            _customPickerTitleText.rectTransform.Stretch(new Vector2(10f, 0f), new Vector2(-34f, 0f));
            _customPickerTitleText.text = "ITEM, BUILDING & PLANET PICKER (Click to toggle)";

            Button closeBtn = CreateHeaderButton("BtnClosePicker", headerRect, new Vector2(-4f, -3f), new Vector2(22f, 22f), true, out Text closeTxt);
            closeTxt.text = "✕";
            closeBtn.onClick.AddListener(() =>
            {
                _customPickerOpen = false;
                _customPickerGo.SetActive(false);
            });

            // Tabs row
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

            // Vertical separator
            UIHelpers.CreateImage("ToolbarDivider", _customPickerRect, new Color(0.32f, 0.56f, 0.78f, 0.55f), false, out RectTransform sepRect);
            sepRect.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(248f, -36f), new Vector2(1f, 24f));

            // Bulk action buttons
            _pickerCheckAllBtn = CreateHeaderButton("PickerCheckAll", _customPickerRect, new Vector2(254f, -36f), new Vector2(74f, 24f), false, out Text pickerCheckAllTxt);
            pickerCheckAllTxt.text = "✓ Check All";
            pickerCheckAllTxt.color = new Color(0.86f, 1f, 0.92f, 1f);
            _pickerCheckAllBtn.GetComponent<Image>().color = new Color(0.11f, 0.30f, 0.23f, 0.96f);
            Outline checkOutline = _pickerCheckAllBtn.GetComponent<Outline>();
            if (checkOutline != null) checkOutline.effectColor = new Color(0.26f, 0.72f, 0.54f, 0.70f);
            _pickerCheckAllBtn.onClick.AddListener(() =>
            {
                _finderFocusItemId = 0;
                _config.SetCustomItemsAllVisible(true);
                RefreshCustomPickerCells();
                RefreshOptionsMenuState();
                _forceRefresh = true;
            });

            _pickerUncheckAllBtn = CreateHeaderButton("PickerUncheckAll", _customPickerRect, new Vector2(332f, -36f), new Vector2(82f, 24f), false, out Text pickerUncheckAllTxt);
            pickerUncheckAllTxt.text = "✕ Uncheck All";
            pickerUncheckAllTxt.color = new Color(1f, 0.88f, 0.90f, 1f);
            _pickerUncheckAllBtn.GetComponent<Image>().color = new Color(0.30f, 0.14f, 0.18f, 0.96f);
            Outline uncheckOutline = _pickerUncheckAllBtn.GetComponent<Outline>();
            if (uncheckOutline != null) uncheckOutline.effectColor = new Color(0.78f, 0.34f, 0.40f, 0.70f);
            _pickerUncheckAllBtn.onClick.AddListener(() =>
            {
                _finderFocusItemId = 0;
                _config.SetCustomItemsAllVisible(false);
                RefreshCustomPickerCells();
                RefreshOptionsMenuState();
                _forceRefresh = true;
            });

            _pickerShowAllPlanetsBtn = CreateHeaderButton("PickerShowAllPlanets", _customPickerRect, new Vector2(256f, -36f), new Vector2(124f, 24f), false, out Text pickerShowAllPlanetsTxt);
            pickerShowAllPlanetsTxt.text = "✓ Show All Planets";
            pickerShowAllPlanetsTxt.color = new Color(0.86f, 1f, 0.92f, 1f);
            _pickerShowAllPlanetsBtn.GetComponent<Image>().color = new Color(0.11f, 0.30f, 0.23f, 0.96f);
            Outline showAllPlanetsOutline = _pickerShowAllPlanetsBtn.GetComponent<Outline>();
            if (showAllPlanetsOutline != null) showAllPlanetsOutline.effectColor = new Color(0.26f, 0.72f, 0.54f, 0.70f);
            _pickerShowAllPlanetsBtn.onClick.AddListener(() =>
            {
                _config.ClearAllIgnoredAstros(CurrentGalaxySeed);
                RefreshCustomPickerPlanetsList();
                RefreshOptionsMenuState();
                _forceRefresh = true;
            });
            _pickerShowAllPlanetsBtn.gameObject.SetActive(false);

            _customPickerHoveredLabel = CreateText("HoveredItemLabel", _customPickerRect, 11, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.80f, 0.92f, 1f, 1f));
            _customPickerHoveredLabel.rectTransform.SetTopStretch(new Vector2(-12f, -36f), new Vector2(-440f, 24f), 1f);
            _customPickerHoveredLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _customPickerHoveredLabel.verticalOverflow = VerticalWrapMode.Truncate;
            _customPickerHoveredLabel.resizeTextForBestFit = true;
            _customPickerHoveredLabel.resizeTextMinSize = 9;
            _customPickerHoveredLabel.resizeTextMaxSize = 11;
            _customPickerHoveredLabel.text = "Bright = Shown • Dimmed = Hidden";

            // Grid container
            Image gridBg = UIHelpers.CreateImage("GridContainer", _customPickerRect, new Color(0.04f, 0.06f, 0.09f, 0.90f), false, out RectTransform gridRect);
            _customPickerGridGo = gridBg.gameObject;
            gridRect.SetTopLeft(new Vector2(padX, -topHeight), new Vector2(gridWidth, gridHeight));

            // Background empty slot cells (14 x 8)
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    UIHelpers.CreateImage($"Slot_{r}_{c}", gridRect, new Color(0.10f, 0.14f, 0.20f, 0.55f), false, out RectTransform slotRect);
                    slotRect.SetTopLeft(new Vector2(c * (cellSize + cellGap), -r * (cellSize + cellGap)), new Vector2(cellSize, cellSize));
                }
            }

            // Populate all items & buildings from LDB.items.dataArray
            _customPickerCells.Clear();
            ItemProto[] allProtos = LDB.items?.dataArray;
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
                    RectTransform cellRect = cellGo.GetComponent<RectTransform>()
                        .SetTopLeft(new Vector2(col * (cellSize + cellGap), -row * (cellSize + cellGap)), new Vector2(cellSize, cellSize));

                    Image cellBg = cellGo.GetComponent<Image>();
                    cellBg.color = new Color(0.14f, 0.24f, 0.36f, 0.90f);

                    Image iconImg = UIHelpers.CreateImage("Icon", cellRect, Color.white, false, out RectTransform iconRect);
                    iconRect.Stretch(new Vector2(3f, 3f), new Vector2(-3f, -3f));
                    iconImg.sprite = proto.iconSprite;
                    iconImg.preserveAspect = true;

                    Outline cellOutline = cellGo.AddComponent<Outline>();
                    cellOutline.effectColor = new Color(0.28f, 0.50f, 0.74f, 0.40f);
                    cellOutline.effectDistance = new Vector2(1f, -1f);

                    Button btn = cellGo.GetComponent<Button>();
                    UIHelpers.ApplyButtonTint(btn, new Color(1.32f, 1.38f, 1.48f, 1f), new Color(0.80f, 0.86f, 0.94f, 1f));
                    btn.onClick.AddListener(() => OnCustomPickerItemClicked(itemId, itemName, itemCategory));

                    ItemCellHoverHandler hover = cellGo.GetComponent<ItemCellHoverHandler>();
                    hover.OnHoverEnter = () =>
                    {
                        if (_customPickerHoveredLabel == null) return;
                        bool vis = _config.IsItemVisible(itemId, itemCategory);
                        _customPickerHoveredLabel.text = vis ? $"{itemName} (Shown)" : $"{itemName} (Hidden)";
                        _customPickerHoveredLabel.color = vis ? HealthyColor : WarningColor;
                    };
                    hover.OnHoverExit = () =>
                    {
                        if (_customPickerHoveredLabel == null) return;
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
            RectTransform scrollRectTransform = _customPickerPlanetsScrollGo.GetComponent<RectTransform>()
                .SetTopLeft(new Vector2(posX, posY), new Vector2(width, height));

            Image bgImg = _customPickerPlanetsScrollGo.GetComponent<Image>();
            bgImg.color = new Color(0.04f, 0.06f, 0.09f, 0.92f);
            bgImg.raycastTarget = true;
            _customPickerPlanetsScrollGo.GetComponent<Mask>().showMaskGraphic = true;

            GameObject contentGo = new GameObject("PlanetsContent", typeof(RectTransform));
            contentGo.transform.SetParent(scrollRectTransform, false);
            _customPickerPlanetsContentRect = contentGo.GetComponent<RectTransform>().SetTopStretch(Vector2.zero, new Vector2(0f, height), 0f);

            ScrollRect sr = _customPickerPlanetsScrollGo.GetComponent<ScrollRect>();
            sr.viewport = scrollRectTransform;
            sr.content = _customPickerPlanetsContentRect;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 28f;

            _customPickerPlanetsEmptyGo = new GameObject("PlanetsEmpty", typeof(RectTransform));
            _customPickerPlanetsEmptyGo.transform.SetParent(_customPickerPlanetsContentRect, false);
            RectTransform emptyRect = _customPickerPlanetsEmptyGo.GetComponent<RectTransform>()
                .SetTopStretch(new Vector2(0f, -20f), new Vector2(-20f, 40f));
            Text emptyTxt = CreateText("EmptyText", emptyRect, 12, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.60f, 0.72f, 0.82f, 0.95f));
            emptyTxt.rectTransform.StretchFull();
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
                RectTransform rect = rowGo.GetComponent<RectTransform>().SetTopLeft(Vector2.zero, new Vector2(272f, 26f));

                Image rowBg = rowGo.GetComponent<Image>();
                rowBg.color = new Color(0.10f, 0.16f, 0.24f, 0.85f);

                Outline outline = rowGo.GetComponent<Outline>();
                outline.effectColor = new Color(0.26f, 0.48f, 0.72f, 0.35f);
                outline.effectDistance = new Vector2(1f, -1f);

                Button btn = rowGo.GetComponent<Button>();
                UIHelpers.ApplyButtonTint(btn, new Color(1.28f, 1.34f, 1.42f, 1f), new Color(0.80f, 0.85f, 0.92f, 1f));

                Image boxImg = UIHelpers.CreateImage("CheckBox", rect, new Color(0.18f, 0.34f, 0.52f, 1f), false, out RectTransform boxRect);
                boxRect.SetRect(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(16f, 16f));

                Text checkTxt = CreateText("CheckMark", boxRect, 11, FontStyle.Bold, TextAnchor.MiddleCenter, HealthyColor);
                checkTxt.rectTransform.StretchFull();
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
                btn.onClick.AddListener(() => OnPlanetFilterRowClicked(view));
                _planetFilterRowPool.Add(view);
            }

            PlanetFilterRowView row = _planetFilterRowPool[index];
            if (!row.RootGo.activeSelf)
            {
                row.RootGo.SetActive(true);
            }
            return row;
        }

        private void OnPlanetFilterRowClicked(PlanetFilterRowView row)
        {
            int seed = CurrentGalaxySeed;
            if (!row.BoundIsStarSystem && _config.IsAstroIgnored(seed, row.BoundParentStarAstroId))
            {
                _config.SetAstroIgnored(seed, row.BoundParentStarAstroId, false);
            }
            else
            {
                _config.ToggleAstroIgnored(seed, row.BoundAstroId);
            }
            RefreshCustomPickerPlanetsList();
            RefreshOptionsMenuState();
            _forceRefresh = true;
        }

        private void RefreshCustomPickerPlanetsList()
        {
            if (_customPickerGo == null)
            {
                return;
            }

            int seed = CurrentGalaxySeed;
            int ignoredCount = _config.GetIgnoredAstroCount(seed);
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

                bool isSelfIgnored = _config.IsAstroIgnored(seed, entry.AstroId);
                bool isParentStarIgnored = !entry.IsStarSystem && _config.IsAstroIgnored(seed, entry.ParentStarAstroId);
                bool effectivelyIgnored = isSelfIgnored || isParentStarIgnored;

                row.BgImage.color = entry.IsStarSystem
                    ? (effectivelyIgnored ? new Color(0.20f, 0.10f, 0.12f, 0.78f) : new Color(0.14f, 0.24f, 0.36f, 0.95f))
                    : (effectivelyIgnored ? new Color(0.15f, 0.08f, 0.10f, 0.68f) : new Color(0.09f, 0.15f, 0.23f, 0.88f));

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

                row.BoundAstroId = entry.AstroId;
                row.BoundParentStarAstroId = entry.ParentStarAstroId;
                row.BoundIsStarSystem = entry.IsStarSystem;
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
            LogisticsAggregator.CollectStoredItemIds(_storedItemsSet, forceRefresh: true);

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
                _customPickerRect.anchorMin = new Vector2(0f, 1f);
                _customPickerRect.anchorMax = new Vector2(0f, 1f);
                _customPickerRect.pivot = new Vector2(1f, 1f);
                _customPickerRect.anchoredPosition = new Vector2(-8f, 0f);
            }
            else
            {
                _customPickerRect.anchorMin = new Vector2(1f, 1f);
                _customPickerRect.anchorMax = new Vector2(1f, 1f);
                _customPickerRect.pivot = new Vector2(0f, 1f);
                _customPickerRect.anchoredPosition = new Vector2(8f, 0f);
            }
        }

        private void SetCustomPickerTab(int tab)
        {
            _customPickerActiveTab = tab;
            Color activeTab = new Color(0.24f, 0.44f, 0.66f, 1f);
            Color inactiveTab = new Color(0.12f, 0.18f, 0.28f, 0.85f);

            if (_customTabItemsBg != null) _customTabItemsBg.color = (tab == 1) ? activeTab : inactiveTab;
            if (_customTabBuildingsBg != null) _customTabBuildingsBg.color = (tab == 2) ? activeTab : inactiveTab;
            if (_customTabPlanetsBg != null) _customTabPlanetsBg.color = (tab == 3) ? activeTab : inactiveTab;

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
            _finderFocusItemId = 0;
            _config.ToggleItemOverride(itemId);
            bool nowVisible = _config.IsItemVisible(itemId, itemCategory);

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

                bool visible = _config.IsItemVisible(cell.ItemId, cell.Category);
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
    }
}
