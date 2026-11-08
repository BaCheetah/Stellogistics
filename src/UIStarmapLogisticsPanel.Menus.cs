using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Stellogistics
{
    public partial class UIStarmapLogisticsPanel
    {
        private GameObject CreatePopupWindow(
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            string title,
            out RectTransform rect,
            out PointerHoverTracker hoverTracker)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(PointerHoverTracker));
            go.transform.SetParent(_panelRect, false);
            rect = go.GetComponent<RectTransform>()
                .SetRect(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), anchoredPos, size);

            hoverTracker = go.GetComponent<PointerHoverTracker>();

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.10f, 0.16f, 0.98f);
            bg.raycastTarget = true;

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.32f, 0.68f, 0.95f, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);

            Text headerTitle = CreateText($"{name}Title", rect, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.97f, 1f, 1f));
            headerTitle.rectTransform.SetTopStretch(new Vector2(10f, -8f), new Vector2(-20f, 18f), 0f);
            headerTitle.text = title;

            return go;
        }

        private CheckboxToggleRow CreateCheckboxRow(
            RectTransform parent,
            string name,
            string label,
            float posY,
            Action onClick,
            Func<bool> getter = null,
            EItemCategoryFilter category = EItemCategoryFilter.None)
        {
            GameObject rowGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            rowGo.transform.SetParent(parent, false);
            RectTransform rect = rowGo.GetComponent<RectTransform>().SetTopStretch(new Vector2(0f, posY), new Vector2(-12f, 24f));

            rowGo.GetComponent<Image>().color = new Color(0.10f, 0.16f, 0.24f, 0.78f);

            Button btn = rowGo.GetComponent<Button>();
            UIHelpers.ApplyButtonTint(btn, new Color(1.30f, 1.36f, 1.44f, 1f), new Color(0.80f, 0.85f, 0.92f, 1f));

            Image boxImg = UIHelpers.CreateImage("CheckBox", rect, new Color(0.18f, 0.34f, 0.52f, 1f), false, out RectTransform boxRect);
            boxRect.SetRect(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(16f, 16f));

            Text checkTxt = CreateText("CheckMark", boxRect, 11, FontStyle.Bold, TextAnchor.MiddleCenter, HealthyColor);
            checkTxt.rectTransform.StretchFull();
            checkTxt.text = "✓";

            Text lblTxt = CreateText("Label", rect, 11, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.90f, 0.95f, 1f, 1f));
            lblTxt.rectTransform.Stretch(new Vector2(28f, 0f), new Vector2(-4f, 0f));
            lblTxt.text = label;

            btn.onClick.AddListener(() => onClick?.Invoke());

            return new CheckboxToggleRow
            {
                Getter = getter,
                Category = category,
                Btn = btn,
                CheckBoxBg = boxImg,
                CheckMarkText = checkTxt,
                LabelText = lblTxt
            };
        }

        private static void SetCheckboxVisual(CheckboxToggleRow row, bool enabled, Color? customActiveLabelColor = null)
        {
            row.CheckMarkText.text = enabled ? "✓" : "";
            row.CheckMarkText.color = HealthyColor;
            row.CheckBoxBg.color = enabled
                ? new Color(0.18f, 0.42f, 0.62f, 1f)
                : new Color(0.12f, 0.16f, 0.22f, 1f);
            row.LabelText.color = enabled
                ? (customActiveLabelColor ?? new Color(0.92f, 0.96f, 1f, 1f))
                : new Color(0.50f, 0.58f, 0.68f, 0.85f);
        }

        private static void SetCheckboxVisual(CheckboxToggleRow row, ECategoryFilterState state)
        {
            switch (state)
            {
                case ECategoryFilterState.Checked:
                    row.CheckMarkText.text = "✓";
                    row.CheckMarkText.color = HealthyColor;
                    row.CheckBoxBg.color = new Color(0.18f, 0.42f, 0.62f, 1f);
                    row.LabelText.color = new Color(0.92f, 0.96f, 1f, 1f);
                    break;
                case ECategoryFilterState.Partial:
                    row.CheckMarkText.text = "–";
                    row.CheckMarkText.color = WarningColor;
                    row.CheckBoxBg.color = new Color(0.22f, 0.34f, 0.48f, 1f);
                    row.LabelText.color = new Color(0.92f, 0.96f, 1f, 1f);
                    break;
                default:
                    row.CheckMarkText.text = "";
                    row.CheckMarkText.color = HealthyColor;
                    row.CheckBoxBg.color = new Color(0.12f, 0.16f, 0.22f, 1f);
                    row.LabelText.color = new Color(0.50f, 0.58f, 0.68f, 0.85f);
                    break;
            }
        }

        private void BuildOptionsMenu()
        {
            _optionsMenuGo = CreatePopupWindow(
                "OptionsMenu",
                new Vector2(-4f, -30f),
                new Vector2(236f, 304f),
                "ITEM FILTER",
                out _optionsMenuRect,
                out _optionsHoverTracker
            );

            _optionsTargetLabel = CreateText("TargetScopeLabel", _optionsMenuRect, 10, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.60f, 0.78f, 0.92f, 1f));
            _optionsTargetLabel.rectTransform.SetTopStretch(new Vector2(10f, -26f), new Vector2(-20f, 16f), 0f);
            _optionsTargetLabel.text = "Global Filter (All Stars & Planets)";

            (EItemCategoryFilter cat, string label)[] categories =
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
                CheckboxToggleRow toggleRow = CreateCheckboxRow(
                    _optionsMenuRect,
                    $"Toggle_{cat}",
                    categories[i].label,
                    y,
                    () => OnCategoryToggleClicked(cat),
                    null,
                    cat
                );
                _filterToggles.Add(toggleRow);
                y -= 26f;
            }

            // Thin divider before status/fill filter
            UIHelpers.CreateImage("StatusFilterDivider", _optionsMenuRect, new Color(0.28f, 0.50f, 0.72f, 0.45f), false, out RectTransform divRect);
            divRect.SetTopStretch(new Vector2(0f, y - 2f), new Vector2(-16f, 1f));
            y -= 6f;

            CheckboxToggleRow exclude100Row = CreateCheckboxRow(
                _optionsMenuRect,
                "Toggle_Exclude100Percent",
                "Exclude ≥90% (Full/Tapped)",
                y,
                () => OnCategoryToggleClicked(EItemCategoryFilter.Exclude100Percent),
                null,
                EItemCategoryFilter.Exclude100Percent
            );
            _excludeRowLabelText = exclude100Row.LabelText;
            _excludeRowLabelText.rectTransform.offsetMax = new Vector2(-50f, 0f);
            _filterToggles.Add(exclude100Row);

            RectTransform excludeRowRect = exclude100Row.Btn.GetComponent<RectTransform>();
            Button threshBtn = CreateHeaderButton("BtnExcludeThreshold", excludeRowRect, new Vector2(-3f, -2f), new Vector2(44f, 20f), true, out _excludeThresholdBtnText);
            _excludeThresholdBtnText.text = "≥90%";
            threshBtn.onClick.AddListener(() =>
            {
                _config.CycleExcludeMode();
                RefreshOptionsMenuState();
                _forceRefresh = true;
            });
            y -= 26f;

            _ignoreTargetToggleRow = CreateCheckboxRow(
                _optionsMenuRect,
                "OptToggle_IgnoreTarget",
                "Ignore Target in Finder",
                y,
                () =>
                {
                    if (_lastTargetAstroId > 0)
                    {
                        _config.ToggleAstroIgnored(CurrentGalaxySeed, _lastTargetAstroId);
                        RefreshOptionsMenuState();
                        RefreshCustomPickerPlanetsList();
                        _forceRefresh = true;
                    }
                },
                () => _lastTargetAstroId > 0 && _config.IsAstroIgnored(CurrentGalaxySeed, _lastTargetAstroId)
            );
            y -= 26f;

            Button resetFiltersBtn = CreateHeaderButton("BtnResetFilters", _optionsMenuRect, new Vector2(-120f, y - 6f), new Vector2(108f, 22f), true, out Text resetFiltersTxt);
            resetFiltersTxt.text = "Reset Filters";
            resetFiltersBtn.onClick.AddListener(() =>
            {
                _finderFocusItemId = 0;
                _config.ResetFilter();
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
            _generalOptionsMenuGo = CreatePopupWindow(
                "GeneralOptionsMenu",
                new Vector2(-4f, -32f),
                new Vector2(236f, 198f),
                "OPTIONS — DISPLAY & UI",
                out _generalOptionsMenuRect,
                out _generalOptionsHoverTracker
            );

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
                "Switch to Planet on Hover",
                y,
                () => _config.ShowOnHover.Value,
                () => _config.ShowOnHover.Value = !_config.ShowOnHover.Value
            ));
            y -= 28f;

            Text refreshTitle = CreateText("RefreshRateLabel", _generalOptionsMenuRect, 10, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.70f, 0.85f, 0.96f, 1f));
            refreshTitle.rectTransform.SetTopStretch(new Vector2(8f, y), new Vector2(-16f, 15f), 0f);
            refreshTitle.text = "REFRESH RATE";
            y -= 17f;

            string[] rateLabels = { "Live", "Medium", "Slow", "Paused" };
            for (int i = 0; i < 4; i++)
            {
                ERefreshRate rate = (ERefreshRate)i;
                Button btn = CreateHeaderButton($"BtnRefresh_{rate}", _generalOptionsMenuRect, new Vector2(6f + i * 57f, y), new Vector2(53f, 20f), false, out _refreshBtnTxts[i]);
                _refreshBtnTxts[i].text = rateLabels[i];
                _refreshBtnBgs[i] = btn.GetComponent<Image>();
                btn.onClick.AddListener(() => SetRefreshRate(rate));
            }
            y -= 26f;

            Button resetPosBtn = CreateHeaderButton("BtnResetPos", _generalOptionsMenuRect, new Vector2(-6f, y - 4f), new Vector2(224f, 22f), true, out Text resetPosTxt);
            resetPosTxt.text = "Reset UI Position & Size";
            resetPosBtn.onClick.AddListener(() =>
            {
                _config.ResetPanelLayout();
                _livePanelWidth = _config.PanelWidth.Value;
                _livePanelMaxHeight = _config.PanelMaxHeight.Value;
                _panelRect.anchoredPosition = new Vector2(StellogisticsConfig.DefaultPosX, StellogisticsConfig.DefaultPosY);
                PositionCustomPickerBesidePanel();
                _forceRefresh = true;
            });

            _generalOptionsMenuGo.SetActive(false);
        }

        private CheckboxToggleRow CreateGeneralOptionToggleRow(
            RectTransform parent,
            string label,
            float posY,
            Func<bool> getter,
            Action toggler)
        {
            return CreateCheckboxRow(
                parent,
                $"OptToggle_{label}",
                label,
                posY,
                () =>
                {
                    toggler();
                    _config.Save();
                    RefreshGeneralOptionsMenuState();
                    _forceRefresh = true;
                },
                getter
            );
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
            if (_panelCollapsed && _customPickerOpen && _customPickerGo != null)
            {
                _customPickerOpen = false;
                _customPickerGo.SetActive(false);
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
            int next = ((int)_config.StarSystemFilterMode.Value + 1) % 3;
            _config.StarSystemFilterMode.Value = (EStarFilterMode)next;
            _config.Save();
            _forceRefresh = true;
        }

        private void ReturnToFinderOverview()
        {
            if (_finderFocusItemId > 0)
            {
                _finderFocusItemId = 0;
            }
            else
            {
                _config.SetCustomItemsAllVisible(true);
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
                if (_finderFocusItemId > 0 || _lastFinderCheckedCount == 1)
                {
                    ReturnToFinderOverview();
                    return;
                }

                _finderModeActive = false;
                _finderFocusItemId = 0;
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
            _config.Save();
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
            _config.Save();
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
                CheckboxToggleRow row = _generalOptionToggles[i];
                SetCheckboxVisual(row, row.Getter());
            }

            ERefreshRate currentRate = _config.RefreshRate.Value;
            Color activeBg = new Color(0.22f, 0.44f, 0.68f, 0.98f);
            Color pausedActiveBg = new Color(0.52f, 0.34f, 0.12f, 0.98f);
            Color inactiveBg = new Color(0.11f, 0.17f, 0.26f, 0.85f);
            Color activeTxt = new Color(0.95f, 0.99f, 1f, 1f);
            Color inactiveTxt = new Color(0.58f, 0.70f, 0.82f, 0.85f);

            for (int i = 0; i < 4; i++)
            {
                ERefreshRate r = (ERefreshRate)i;
                bool isSel = currentRate == r;
                if (_refreshBtnBgs[i] != null)
                {
                    _refreshBtnBgs[i].color = isSel
                        ? (r == ERefreshRate.Paused ? pausedActiveBg : activeBg)
                        : inactiveBg;
                }
                if (_refreshBtnTxts[i] != null)
                {
                    _refreshBtnTxts[i].color = isSel
                        ? (r == ERefreshRate.Paused ? WarningColor : activeTxt)
                        : inactiveTxt;
                }
            }
        }

        private void OnCategoryToggleClicked(EItemCategoryFilter category)
        {
            if (category == EItemCategoryFilter.Exclude100Percent)
            {
                _config.ToggleExcludeEnabled();
                RefreshOptionsMenuState();
                _forceRefresh = true;
                return;
            }

            _finderFocusItemId = 0;
            ECategoryFilterState currentState = _config.GetCategoryFilterState(category);
            bool enableCategory = currentState != ECategoryFilterState.Checked;
            EItemCategoryFilter current = _config.CategoryFilter;
            EItemCategoryFilter updated = enableCategory
                ? (current | category)
                : (current & ~category);

            _config.SetCategoryFilter(updated, category);
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

            _optionsTargetLabel.text = _config.HasCustomFilter
                ? "Global Filter (Active across Starmap)"
                : "Global Filter (All Shown)";

            if (_excludeRowLabelText != null && _excludeThresholdBtnText != null)
            {
                _config.GetValidatedThresholds(out _, out int warnPct);
                switch (_config.ExcludeMode)
                {
                    case EExcludeMode.Full100:
                        _excludeRowLabelText.text = "Exclude 100% Entries";
                        _excludeThresholdBtnText.text = "100%";
                        break;
                    case EExcludeMode.Tapped90:
                        _excludeRowLabelText.text = "Exclude ≥90% (Tapped)";
                        _excludeThresholdBtnText.text = "≥90%";
                        break;
                    default:
                        _excludeRowLabelText.text = $"Exclude >{warnPct}% (Healthy)";
                        _excludeThresholdBtnText.text = $">{warnPct}%";
                        break;
                }
            }

            for (int i = 0; i < _filterToggles.Count; i++)
            {
                CheckboxToggleRow row = _filterToggles[i];
                SetCheckboxVisual(row, _config.GetCategoryFilterState(row.Category));
            }

            if (_ignoreTargetToggleRow != null)
            {
                bool ignored = _ignoreTargetToggleRow.Getter();
                int totalIgnored = _config.GetIgnoredAstroCount(CurrentGalaxySeed);
                string targetKind = (_lastResolvedStar != null && _lastResolvedPlanet == null) ? "Star System" : "Planet";
                string countSuffix = totalIgnored > 0 ? $" ({totalIgnored} ignored)" : "";
                _ignoreTargetToggleRow.LabelText.text = $"Ignore {targetKind} in Finder{countSuffix}";
                SetCheckboxVisual(_ignoreTargetToggleRow, ignored, WarningColor);
                if (!ignored)
                {
                    _ignoreTargetToggleRow.LabelText.color = new Color(0.92f, 0.96f, 1f, 1f);
                }
            }
        }
    }
}
