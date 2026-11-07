# Changelog

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
