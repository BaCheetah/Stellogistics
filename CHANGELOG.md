# Changelog

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
