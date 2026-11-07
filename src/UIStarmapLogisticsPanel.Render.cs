using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stellogistics
{
    public partial class UIStarmapLogisticsPanel
    {
        private string GetSortLabel()
        {
            return _config.SortOrder.Value switch
            {
                ESortOrder.ShortageFirst => "Sort: Alert",
                ESortOrder.ItemId => "Sort: ID",
                _ => "Sort: Pct ↓"
            };
        }

        private void ShowEmptyState(string message, float panelWidth, float maxScrollHeight, float activeHeaderH)
        {
            _exportsHeaderGo.SetActive(false);
            _importsHeaderGo.SetActive(false);
            HideRowsFrom(0);

            _emptyStateGo.SetActive(true);
            _emptyStateText.text = message;

            _activeContentHeight = MinScrollHeight;
            float emptyViewportHeight = Mathf.Max(MinScrollHeight, maxScrollHeight);
            SetScrollbarActive(false);
            _scrollContentRect.anchoredPosition = Vector2.zero;
            _scrollContentRect.sizeDelta = new Vector2(0f, _activeContentHeight);
            _scrollViewportRect.sizeDelta = new Vector2(0f, emptyViewportHeight);
            _panelRect.sizeDelta = new Vector2(panelWidth, activeHeaderH + emptyViewportHeight);
            _resizeGripGo.transform.SetAsLastSibling();
        }

        private void LayoutSections<T>(
            IList<T> exports,
            string exportsTitle,
            int critExports,
            int warnExports,
            IList<T> imports,
            string importsTitle,
            int critImports,
            int warnImports,
            float panelWidth,
            float maxScrollHeight,
            float activeHeaderH,
            Action<ItemRowView, T, float, int, int> bindAction)
        {
            _emptyStateGo.SetActive(false);

            float estimatedHeight = 0f;
            if (exports.Count > 0)
            {
                estimatedHeight += SectionHeaderHeight + RowSpacing;
                if (!_exportsCollapsed)
                {
                    estimatedHeight += exports.Count * (RowHeight + RowSpacing);
                }
            }
            if (imports.Count > 0)
            {
                if (estimatedHeight > 0f) estimatedHeight += 4f;
                estimatedHeight += SectionHeaderHeight + RowSpacing;
                if (!_importsCollapsed)
                {
                    estimatedHeight += imports.Count * (RowHeight + RowSpacing);
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
            _config.GetValidatedThresholds(out int critPct, out int warnPct);

            if (exports.Count > 0)
            {
                _exportsHeaderGo.SetActive(true);
                _exportsHeaderRect.anchoredPosition = new Vector2(0f, -cursorY);
                string expArrow = _exportsCollapsed ? "▶" : "▼";
                _exportsHeaderText.text = $"{expArrow} {exportsTitle} ({exports.Count})";
                _exportsAlertText.text = FormatAlertSummary(critExports, warnExports, "EMPTY", "LOW");
                _exportsAlertText.color = critExports > 0 ? CriticalColor : WarningColor;
                cursorY += SectionHeaderHeight + RowSpacing;

                if (!_exportsCollapsed)
                {
                    for (int i = 0; i < exports.Count; i++)
                    {
                        ItemRowView row = GetOrCreateRow(rowIndex++);
                        row.RootRect.anchoredPosition = new Vector2(0f, -cursorY);
                        bindAction(row, exports[i], rowAvailableWidth, critPct, warnPct);
                        cursorY += RowHeight + RowSpacing;
                    }
                }
            }
            else
            {
                _exportsHeaderGo.SetActive(false);
            }

            if (imports.Count > 0)
            {
                if (cursorY > 0f)
                {
                    cursorY += 4f;
                }
                _importsHeaderGo.SetActive(true);
                _importsHeaderRect.anchoredPosition = new Vector2(0f, -cursorY);
                string impArrow = _importsCollapsed ? "▶" : "▼";
                _importsHeaderText.text = $"{impArrow} {importsTitle} ({imports.Count})";
                _importsAlertText.text = FormatAlertSummary(critImports, warnImports, "STARVED", "LOW");
                _importsAlertText.color = critImports > 0 ? CriticalColor : WarningColor;
                cursorY += SectionHeaderHeight + RowSpacing;

                if (!_importsCollapsed)
                {
                    for (int i = 0; i < imports.Count; i++)
                    {
                        ItemRowView row = GetOrCreateRow(rowIndex++);
                        row.RootRect.anchoredPosition = new Vector2(0f, -cursorY);
                        bindAction(row, imports[i], rowAvailableWidth, critPct, warnPct);
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

        private void RenderSummary(LogisticsSummary summary)
        {
            if (_panelCollapsed)
            {
                ApplyCollapsedTabLayout();
                return;
            }

            float panelWidth = _isResizing ? _livePanelWidth : _config.PanelWidth.Value;
            float maxScrollHeight = _isResizing ? _livePanelMaxHeight : _config.PanelMaxHeight.Value;
            float activeHeaderH = ApplyHeaderLayout(panelWidth, summary.IsStarSystem);

            _subtitleText.gameObject.SetActive(true);
            _fleetInfoText.gameObject.SetActive(true);
            _finderModeBtn.gameObject.SetActive(true);
            _finderModeBtnText.text = "Finder";
            if (_finderModeBtnBg != null) _finderModeBtnBg.color = new Color(0.15f, 0.26f, 0.39f, 0.96f);
            if (_filterModeBtnBg != null) _filterModeBtnBg.color = new Color(0.15f, 0.26f, 0.39f, 0.96f);
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

            _sortModeBtnText.text = GetSortLabel();

            if (!_scrollRect.gameObject.activeSelf)
            {
                _scrollRect.gameObject.SetActive(true);
            }

            if (!summary.HasAnySlots)
            {
                string emptyMsg = summary.FilteredOutItemCount > 0
                    ? $"{summary.FilteredOutItemCount} item(s) hidden by Filter."
                    : (summary.HasAnyStations
                        ? "No matching remote supply/demand slots."
                        : "No Interplanetary Logistics Stations built.");
                ShowEmptyState(emptyMsg, panelWidth, maxScrollHeight, activeHeaderH);
                return;
            }

            bool isStar = summary.IsStarSystem;
            LayoutSections(
                summary.Exports,
                "EXPORTS",
                summary.CriticalExportCount,
                summary.WarningExportCount,
                summary.Imports,
                "IMPORTS",
                summary.CriticalImportCount,
                summary.WarningImportCount,
                panelWidth,
                maxScrollHeight,
                activeHeaderH,
                (row, item, rowW, cPct, wPct) => BindRow(row, item, isStar, rowW, cPct, wPct)
            );
        }

        private void RenderGalaxySearchSummary(GalaxyItemSearchSummary summary)
        {
            if (_panelCollapsed)
            {
                ApplyCollapsedTabLayout();
                return;
            }

            float panelWidth = _isResizing ? _livePanelWidth : _config.PanelWidth.Value;
            float maxScrollHeight = _isResizing ? _livePanelMaxHeight : _config.PanelMaxHeight.Value;
            float activeHeaderH = ApplyHeaderLayout(panelWidth, false);

            _subtitleText.gameObject.SetActive(true);
            _fleetInfoText.gameObject.SetActive(true);
            _filterModeBtn.gameObject.SetActive(false);

            _finderModeBtn.gameObject.SetActive(true);
            _lastFinderCheckedCount = summary.CheckedItemCount;
            bool isSingleItemFocused = summary.CheckedItemCount == 1;
            if (isSingleItemFocused)
            {
                _finderModeBtnText.text = "← All";
                if (_finderModeBtnBg != null) _finderModeBtnBg.color = new Color(0.26f, 0.50f, 0.74f, 0.98f);
            }
            else
            {
                _finderModeBtnText.text = "← Back";
                if (_finderModeBtnBg != null) _finderModeBtnBg.color = new Color(0.22f, 0.40f, 0.62f, 0.98f);
            }

            _sortModeBtn.gameObject.SetActive(true);
            _optionsBtn.gameObject.SetActive(true);
            _generalOptionsBtn.gameObject.SetActive(true);
            _resizeGripGo.SetActive(true);

            int totalIgnoredAstros = _config.GetIgnoredAstroCount(CurrentGalaxySeed);
            string ignoredSuffix = summary.IgnoredPlanetMatchCount > 0
                ? $" • {summary.IgnoredPlanetMatchCount} Ignored"
                : (totalIgnoredAstros > 0 ? $" • {totalIgnoredAstros} Filtered" : "");

            bool onlyBottlenecks = _config.ExcludeEnabled && _config.ExcludeMode == EExcludeMode.Healthy;
            string pausedSuffix = _config.RefreshRate.Value == ERefreshRate.Paused
                ? " • <color=#FFD24C>⏸ Paused</color>"
                : "";
            _titleText.text = $"Finder: {summary.ItemName}";
            _subtitleText.text = (onlyBottlenecks && summary.CheckedItemCount != 1)
                ? $"Galaxy Scan • {summary.ExportingPlanets.Count} Export / {summary.ImportingPlanets.Count} Import Alerts{ignoredSuffix}{pausedSuffix}"
                : $"Galaxy Scan • {summary.ExportingPlanets.Count} Exporting • {summary.ImportingPlanets.Count} Importing{ignoredSuffix}{pausedSuffix}";
            _fleetInfoText.text = "Click row to focus & pin • ✕ to hide item";

            _sortModeBtnText.text = GetSortLabel();

            if (!_scrollRect.gameObject.activeSelf)
            {
                _scrollRect.gameObject.SetActive(true);
            }

            if (!summary.HasAnyLocations)
            {
                _config.GetValidatedThresholds(out _, out int warnPct);
                string emptyMsg;
                if (summary.CheckedItemCount == 0)
                {
                    emptyMsg = "No items checked — click any item(s) in the Picker to locate.";
                }
                else if (summary.IgnoredPlanetMatchCount > 0)
                {
                    emptyMsg = $"{summary.IgnoredPlanetMatchCount} matching planet(s) hidden by Planet Filter (Filter ▾ → Custom Picker → Planets).";
                }
                else
                {
                    emptyMsg = onlyBottlenecks
                        ? $"All checked remote slots are Healthy (>{warnPct}%)!"
                        : $"No planet exports or imports {summary.ItemName}.";
                }
                ShowEmptyState(emptyMsg, panelWidth, maxScrollHeight, activeHeaderH);
                return;
            }

            bool isBottleneck = summary.IsBottleneckScan;
            LayoutSections(
                summary.ExportingPlanets,
                "EXPORTING PLANETS",
                summary.CriticalExportPlanetCount,
                summary.WarningExportPlanetCount,
                summary.ImportingPlanets,
                "IMPORTING PLANETS",
                summary.CriticalImportPlanetCount,
                summary.WarningImportPlanetCount,
                panelWidth,
                maxScrollHeight,
                activeHeaderH,
                (row, entry, rowW, cPct, wPct) => BindPlanetLocationRow(row, entry, isBottleneck, rowW, cPct, wPct)
            );
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

        private static string FormatSlotDescription(CombinedLogisticsItem item)
        {
            int ilsSlots = item.SlotCount - item.CollectorSlotCount;
            if (item.CollectorSlotCount > 0 && ilsSlots > 0)
            {
                return $"{ilsSlots} ILS +{item.CollectorSlotCount} OC";
            }
            if (item.CollectorSlotCount > 0)
            {
                return $"{item.CollectorSlotCount} OC";
            }
            return $"{item.SlotCount} ILS";
        }

        private static string FormatHubBadge(CombinedLogisticsItem item)
        {
            string counterRole = item.Logic == ELogisticStorage.Supply ? "Imp" : "Exp";
            string counterMax = CombinedLogisticsItem.FormatAmount(item.HubCounterpartMax);
            return $"<color=#5CE1FF>⇄ Hub ({counterRole} {counterMax})</color>";
        }

        private void BindItemVisuals(
            ItemRowView row,
            CombinedLogisticsItem item,
            int critPct,
            int warnPct)
        {
            EHealthStatus status = item.GetHealthStatus(critPct, warnPct);
            var (accentColor, statusTextColor, fillColor, transitColor) = StatusPalette[(int)status];

            row.StatusStripeImage.color = accentColor;
            row.FillBarImage.color = fillColor;
            float fillRatio = Mathf.Clamp01(item.FillRatio);
            row.FillBarRect.anchorMin = new Vector2(0f, 0f);
            row.FillBarRect.anchorMax = new Vector2(fillRatio, 1f);
            row.FillBarRect.offsetMin = Vector2.zero;
            row.FillBarRect.offsetMax = Vector2.zero;

            float transitRatio = _config.ShowInTransitBar.Value ? item.InboundTransitRatio : 0f;
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

            string storedStr = CombinedLogisticsItem.FormatAmount(item.StoredCount);
            string maxStr = CombinedLogisticsItem.FormatAmount(item.MaxCount);
            row.AmountText.text = $"{storedStr} / {maxStr}";
            row.AmountText.color = status == EHealthStatus.Healthy
                ? new Color(0.95f, 1.0f, 0.97f, 1f)
                : statusTextColor;

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
            row.SubStatusText.color = statusTextColor;

            row.ShowMuteOnHover = true;
            ApplyRowMuteAnimationState(row);
        }

        private static string TruncateDisplayName(string displayName, float rowWidth)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                return "";
            }
            int maxChars = Mathf.Max(18, Mathf.RoundToInt((rowWidth - 135f) / 6.8f));
            return displayName.Length > maxChars
                ? displayName.Substring(0, maxChars - 2) + ".."
                : displayName;
        }

        private void BindRow(
            ItemRowView row,
            CombinedLogisticsItem item,
            bool isStarSystem,
            float rowWidth,
            int critPct,
            int warnPct)
        {
            BindItemVisuals(row, item, critPct, warnPct);

            row.NameText.text = TruncateDisplayName(item.ItemName, rowWidth);

            string slotDesc = FormatSlotDescription(item);
            if (isStarSystem && item.PlanetCount > 1)
            {
                slotDesc = $"{item.PlanetCount} Planets • {slotDesc}";
            }
            if (item.IsHub)
            {
                slotDesc = $"{slotDesc} • {FormatHubBadge(item)}";
            }
            row.MetaText.text = slotDesc;

            row.BoundItemId = item.ItemId;
            row.BoundPlanet = null;
            row.BoundIsFinderRow = false;
            row.BoundIsBottleneckScan = false;
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
            BindItemVisuals(row, item, critPct, warnPct);

            string rawName = isBottleneckScan
                ? $"{entry.PlanetName} • {item.ItemName}"
                : $"▸ {entry.PlanetName}";
            row.NameText.text = TruncateDisplayName(rawName, rowWidth);

            string slotDesc = FormatSlotDescription(item);
            if (item.IsHub)
            {
                string hubBadge = FormatHubBadge(item);
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

            row.BoundItemId = item.ItemId;
            row.BoundPlanet = entry.Planet;
            row.BoundIsFinderRow = true;
            row.BoundIsBottleneckScan = isBottleneckScan;
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
