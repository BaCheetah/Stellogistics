# Changelog

## 2.2.3
- **Fixed Live Language Switching & Font Fallback Resolution**:
  - Fixed blank text after switching languages live in-game by cleanly recreating the Starmap panel when `Localization.CurrentLanguageLCID` changes and adding a multi-step font fallback chain (`UIStarmap.cursorViewText`, `UIPlanetDetail.typeText`, `UIControlPanelWindow.FONT_SAIRASB`, `Resources.Load<Font>("UI/Fonts/SAIRASB")`, `"UI/Fonts/SAIRAR"`, `Configs.builtin.markerBreifInfoWordFont`, and Unity 2022's `"LegacyRuntime.ttf"`).
- **Improved CJK Typography (`简体中文` & `日本語`)**:
  - Uses normal font weight (`FontStyle.Normal`) instead of synthesized bold/italic when Simplified Chinese or Japanese is active so multi-stroke CJK characters remain crisp and legible.

## 2.2.2
- **README & Documentation Cleanup**:
  - Moved the Simplified Chinese (`简体中文`) and Japanese (`日本語`) overview sections above `Configuration` and `Source Code & How to Compile Yourself`, keeping `Configuration` and build instructions at the bottom of `README.md`, and streamlined repository link references.

## 2.2.1
- **Automatic Multi-Language Localization (`简体中文` / `日本語` / `English` / `Deutsch` / `Français`)**:
  - Automatically detects *Dyson Sphere Program*'s active game language (`Localization.CurrentLanguageLCID`) and translates the entire Stellogistics Starmap UI, Galaxy Finder, `Filter ▾` / `Options ▾` menus, and Custom Picker.
  - Supports instant live language switching in the game settings without restarting and adjusts row text truncation widths for CJK characters (`简体中文` & `日本語`).
  - **简体中文**: 自动识别游戏语言并完整支持简体中文界面（星图行星/星系星际物流进出口汇总、全星区物品与瓶颈查找器、配方原料追溯及自定义筛选器）。
  - **日本語**: ゲームの言語設定を自動検出し、日本語UIに完全対応（星図での惑星・星系別ILS輸出入集計、銀河全体のアイテム＆ボトルネック検索、レシピ原料の逆引き追跡、カスタムフィルタ）。

## 2.2.0
- **Upstream Recipe Chain Navigation in Finder (Click Item Icon)**:
  - Clicking any craftable item's icon on a Finder row dynamically queries *Dyson Sphere Program*'s `LDB` recipe tables (`maincraft` & alternate recipes) and automatically filters the Finder to that item's source ingredients (e.g., clicking *Proliferator Mk.III* filters to *Proliferator Mk.II + Carbon Nanotube*).
  - **Multi-Level Finder History Stack (`← Back`)**: Every icon-click or single-item drill-down pushes the previous Finder state and filter snapshot onto a history stack, allowing `← Back` to step up one recipe/filter level at a time inside the Finder and automatically restore your original filter.
- **Tri-State Category Checkboxes (`Filter ▾`)**:
  - Category checkboxes in `Filter ▾` now display a half-active indicator (`–`) when only a subset of items in that category is enabled via the Custom Picker, recipe source navigation, or the inline row `✕` mute button.
- **Clickable `Vessels` Display (`on route` / `idle`)**:
  - The header `Vessels` readout now defaults to showing active ships `on route` and can be stealthily clicked to toggle between `on route` and `idle`.
- **Updated `Options ▾` Defaults, Labels & Refresh Rates**:
  - Defaulted `Include Orbital Collectors (OC)` to `false` (disabled by default).
  - Renamed the Starmap hover option to `Switch to Planet / System on Hover`.
  - Updated the Refresh Rate selector to `Live` (0.10s / 10 Hz), `Medium` (1.00s / 1 Hz), `Slow` (10.0s / 0.1 Hz), and `Paused`.

## 2.1.4
- Disable auto-save per resize frame, make Finder single-item focus transient, add `EExcludeMode` & TTL caches, remove dead code, and split `UIStarmapLogisticsPanel` into partial classes.

## 2.1.3
- **Hover-Animated `✕` Item-Hide Button**:
  - Clicking `✕` on any row (both in the Galaxy Finder and in the Planet / Star System overview) now unchecks that specific item in the Picker instead of ignoring the entire planet.
  - The `✕` button stays completely hidden at rest and smoothly slides in from the right (while shifting the amount text left) only when the corresponding row is hovered.
- **Improved Readability on Hatched `+In-Transit` Bars**:
  - Subdued the brightness and opacity of the diagonal hatched `+In-Transit` bar, lightened critical/warning/healthy status text colors for higher luminance contrast, and added a subtle `1px` dark drop shadow to row texts so fully saturated in-transit bars remain crisp and readable.
- **Fixed Starmap Hover Mode & Defaulted to Disabled**:
  - Fixed `Show Panel on Starmap Hover` (`Options ▾`) so hovering over planets or stars in the Starmap takes precedence over an already-focused planet/star while enabled, and changed its default setting to `false` (disabled).
- **Streamlined Finder & Auto-Closing Dropdowns**:
  - Entering `Finder` no longer auto-opens the Item Picker window, and removed the redundant `Pick Item` header button in Finder (access the Picker uniformly via `Filter ▾ → Custom Picker`).
  - `Filter ▾` and `Options ▾` dropdown menus now automatically collapse whenever you click anywhere else in the UI or Starmap.

## 2.1.2
- **Unified Picker & Improved Deselected Item Visibility**:
  - Increased the brightness and opacity of deselected (hidden) item icons in the Picker so dimmed icons remain clearly recognizable while keeping a strong visual contrast against selected (shown) items.
  - Removed the redundant `! ≤40% Only` button from the Picker so the Picker toolbar is 100% identical in all modes and the `Exclude` threshold setting in `Filter ▾` (`≥90%` / `100%` / `>40%`) governs both normal Starmap views and the Galaxy Finder uniformly.

## 2.1.1
- **Task Manager-Style Refresh Rate Control (`Options ▾`)**:
  - Added a 4-mode Refresh Rate selector in `Options ▾`: `Fast` (0.10s), `Medium` (0.25s, Default), `Slow` (1.00s), and `Paused` (`⏸ Paused`, freezing periodic updates so rows stay still while inspecting).

## 2.1.0
- **Planet & Star System Hub Detection (`⇄ Hub`)**:
  - Automatically detects when a planet (or star system) both exports and imports the same item, displaying a cyan `⇄ Hub (Imp ...)` / `⇄ Hub (Exp ...)` badge with the counterpart capacity on both rows (even if one side is hidden by filters).
- **Logistics / Throughput Bottleneck Indicator (`⚡+ILS`)**:
  - Automatically flags importing entries with `⚡+ILS` when their storage is `STARVED` or `LOW` and current stock plus inbound vessels (`+In-Transit` hatched bar) does not fill capacity (`< 85%`) while another non-ignored planet in the galaxy has healthy/unclaimed export stock ready for pickup.
- **UI, Header & Scrollbar Polish**:
  - Enforced the vertical scrollbar as the true right boundary of the list and anchored fill/transit bars via normalized `RectTransform` anchors so rows never bleed behind or to the right of the scrollbar during resizing.
  - Made the inline Finder `[✕]` planet-ignore button subtle and unobtrusive at rest, lighting up only when hovered.
  - Improved header button row spacing, responsive stacking thresholds, and top-left collapse button margins.

## 2.0.1
- **Fixed Taskbar Tab Drag-to-Collapse Flicker**: Prevented `ApplyHeaderLayout` from transiently applying the expanded/stacked header height while dragging the resize grip in or above the collapsed Taskbar Tab state.

## 2.0.0
- **Galaxy Item & Bottleneck Finder (`Finder`)**:
  - Locate every planet across the entire star cluster that exports or imports any selected item(s), or scan for all active bottlenecks (`! ≤40% Only`) in one view.
  - **Click-to-Focus & Navigation Pin**: Clicking any planet row in the Finder centers and focuses that planet on the Starmap and pins the player's navigation guidance line (`indicatorAstroId`) to it.
  - **One-Click Drill-Down & `← All` Return**: Click any item row in normal or Finder view to isolate that single item across the galaxy, and click `← All` to jump right back to your previous Finder overview.
- **Planet & Star System Ignore Filter**:
  - Mute abandoned or starter planets/systems in the Finder using the inline `[✕]` button on Finder rows, the `Ignore Planet/Star System in Finder` toggle in `Filter ▾`, or the dedicated **`Planets`** tab inside the Picker (`✓ Show All Planets` to clear).
- **Unified Item, Building & Planet Picker**:
  - Merged the Finder and Filter item pickers into a single unified window (`Items`, `Buildings`, and `Planets` tabs) with `✓ Check All`, `✕ Uncheck All`, and `! ≤40% Only` controls.
- **Hatched `+In-Transit` Capacity Bar & `Options ▾` Menu**:
  - Added a diagonal hatched bar extension showing inbound vessel cargo (`+In-Transit`) relative to station capacity.
  - Added a dedicated `Options ▾` menu (`Show +In-Transit Bar`, `Include Orbital Collectors`, `Show Panel on Starmap Hover`, and `Reset UI Position & Size`).
- **UI & Resizer Polish**:
  - Responsive two-row stacked header buttons when the panel is resized narrow (`< 385px`).
  - Resizing the panel now preserves custom panel height ("dead space") without snapping back when few rows are displayed, and aligns the lower-right resize grip (`◢`) flush with the panel corner.

## 1.1.0
- **Global Starmap Filters**: Changed category and custom item filters to persist globally across the entire Starmap instead of resetting per planet/star system, making it easy to search for specific items (e.g., Photon Combiners) across all stars and planets.
- **Exclude Full / Freshly-Tapped Storages Filter**: Added a toggleable filter in `Filter ▾` to hide full or freshly-tapped storages, with a cyclable threshold button (`≥90%` Tapped, `100%` Full, or `>40%` Healthy).
- **Improved Percentage Sorting & Display**:
  - `Sort: Alert` now sorts strictly ascending by fill percentage (`0% → 100%`).
  - `Sort: Pct ↓` sorts strictly descending by fill percentage (`100% → 0%`).
  - Non-full storages (`StoredCount < MaxCount`) no longer round up to `100%` (clamped to `99%` until completely full).

## 1.0.0
- Official v1.0.0 release of Stellogistics!
- Added combined Starmap logistics panel for Planet and Star System imports and exports.
- Added shortage color coding (Critical/Empty, Warning/Low, Healthy) and automatic shortage-first sorting.
- Added smart interstellar filtering for Star System views (`Ext Only`, `Netted`, `All Slots`).
- Added Gas Giant Orbital Collector (`OC`) aggregation support.
- Added freely draggable, resizable, and collapsible UI with a compact `Stellogistics` taskbar tab mode and multi-stage drag-to-collapse/expand behavior.
- Added per-Star System and per-Planet saved item filters, including 6 broad Category Checkboxes and a spoiler-free `14 × 8` Custom Item & Building Picker with `Check All` and `Uncheck All` controls.
- Automatically blocks Starmap camera zoom while scrolling or interacting with the Stellogistics UI.
