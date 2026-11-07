using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;

namespace Stellogistics
{
    public enum EStarFilterMode
    {
        InterstellarSmart = 0,
        NetOutIntraSystem = 1,
        AllRemoteSlots = 2
    }

    public enum ESortOrder
    {
        ShortageFirst = 0,
        ItemId = 1,
        StoredAmountDesc = 2
    }

    public enum ERefreshRate
    {
        Fast = 0,
        Medium = 1,
        Slow = 2,
        Paused = 3
    }

    [Flags]
    public enum EItemCategoryFilter
    {
        None = 0,
        RawResources = 1 << 0,
        Materials = 1 << 1,
        Components = 1 << 2,
        Buildings = 1 << 3,
        ConsumablesAndPods = 1 << 4,
        Matrices = 1 << 5,
        All = RawResources | Materials | Components | Buildings | ConsumablesAndPods | Matrices,
        Exclude100Percent = 1 << 6
    }

    public class StellogisticsConfig
    {
        private readonly ConfigFile _configFile;

        public const float DefaultPosX = 82f;
        public const float DefaultPosY = -470f;
        public const float DefaultWidth = 310f;
        public const float DefaultMaxHeight = 460f;

        public ConfigEntry<bool> EnableMod { get; }
        public ConfigEntry<bool> IncludeOrbitalCollectors { get; }
        public ConfigEntry<EStarFilterMode> StarSystemFilterMode { get; }
        public ConfigEntry<ESortOrder> SortOrder { get; }
        public ConfigEntry<bool> ShowOnHover { get; }
        public ConfigEntry<bool> ShowInTransitBar { get; }
        public ConfigEntry<ERefreshRate> RefreshRate { get; }
        public ConfigEntry<int> CriticalThresholdPercent { get; }
        public ConfigEntry<int> WarningThresholdPercent { get; }
        public ConfigEntry<int> ExcludeFullThresholdPercent { get; }
        public ConfigEntry<float> PanelWidth { get; }
        public ConfigEntry<float> PanelMaxHeight { get; }

        public ConfigEntry<bool> UseCustomPosition { get; }
        public ConfigEntry<float> CustomPanelPosX { get; }
        public ConfigEntry<float> CustomPanelPosY { get; }

        public ConfigEntry<EItemCategoryFilter> DefaultCategoryFilter { get; }
        public ConfigEntry<string> SavedAstroFilters { get; }
        public ConfigEntry<string> SavedAstroItemOverrides { get; }
        public ConfigEntry<string> SavedIgnoredAstros { get; }

        private readonly Dictionary<string, EItemCategoryFilter> _astroFilterCache = new Dictionary<string, EItemCategoryFilter>();
        private readonly Dictionary<string, HashSet<int>> _astroItemOverridesCache = new Dictionary<string, HashSet<int>>();
        private readonly Dictionary<int, HashSet<int>> _ignoredAstrosBySeed = new Dictionary<int, HashSet<int>>();

        public StellogisticsConfig(ConfigFile config)
        {
            _configFile = config;

            EnableMod = config.Bind(
                "General",
                "EnableMod",
                true,
                "Enable the Stellogistics import/export panel on the Starmap."
            );

            IncludeOrbitalCollectors = config.Bind(
                "Logistics",
                "IncludeOrbitalCollectors",
                true,
                "Include Gas Giant Orbital Collectors (Hydrogen, Deuterium, Fire Ice) in Exports."
            );

            StarSystemFilterMode = config.Bind(
                "Logistics",
                "StarSystemFilterMode",
                EStarFilterMode.InterstellarSmart,
                "How to filter intra-system trade when viewing an entire Star System:\n" +
                "InterstellarSmart: Excludes slots restricted to intra-system range (<1 LY) and internal supply+demand pairs that have no external star routes.\n" +
                "NetOutIntraSystem: Nets total Supply against total Demand within the same star system.\n" +
                "AllRemoteSlots: Shows all Remote Supply and Remote Demand slots in the star system."
            );

            SortOrder = config.Bind(
                "Display",
                "SortOrder",
                ESortOrder.ShortageFirst,
                "How combined items are sorted in the Exports and Imports lists:\n" +
                "ShortageFirst: Items running empty (Exports) or starved (Imports) appear at the top.\n" +
                "ItemId: Sorted by DSP item ID.\n" +
                "StoredAmountDesc: Sorted by highest stored amount first."
            );

            ShowOnHover = config.Bind(
                "Display",
                "HoverStarmapTargets",
                false,
                "Show logistics when hovering a planet or star in the Starmap (Default: false)."
            );

            ShowInTransitBar = config.Bind(
                "Display",
                "ShowInTransitBar",
                true,
                "Show a hatched bar extension on item rows indicating inbound (+ in-transit) cargo on the way."
            );

            RefreshRate = config.Bind(
                "Display",
                "RefreshRate",
                ERefreshRate.Medium,
                "How frequently the Starmap logistics panel refreshes its data:\n" +
                "Fast: Every 0.10s (10 Hz)\n" +
                "Medium: Every 0.25s (4 Hz, Default)\n" +
                "Slow: Every 1.00s (1 Hz)\n" +
                "Paused: Pauses automatic periodic refreshes (still updates on target change or UI clicks)."
            );

            CriticalThresholdPercent = config.Bind(
                "Thresholds",
                "CriticalThresholdPercent",
                15,
                new ConfigDescription(
                    "Storage fullness percentage at or below which an item is highlighted Red (Critical / Empty).",
                    new AcceptableValueRange<int>(1, 50)
                )
            );

            WarningThresholdPercent = config.Bind(
                "Thresholds",
                "WarningThresholdPercent",
                40,
                new ConfigDescription(
                    "Storage fullness percentage at or below which an item is highlighted Orange (Low / Depleting).",
                    new AcceptableValueRange<int>(10, 85)
                )
            );

            ExcludeFullThresholdPercent = config.Bind(
                "Thresholds",
                "ExcludeFullThresholdPercent",
                90,
                new ConfigDescription(
                    "Storage fullness percentage at or above which an item is excluded when the Exclude Full filter is enabled (90 = excludes 90%–100% including freshly tapped storages; 100 = only 100%; 41 = excludes all green >40% entries).",
                    new AcceptableValueRange<int>(40, 100)
                )
            );

            PanelWidth = config.Bind(
                "UI",
                "PanelWidth",
                DefaultWidth,
                new ConfigDescription(
                    "Width of the Stellogistics panel in pixels.",
                    new AcceptableValueRange<float>(240f, 700f)
                )
            );

            PanelMaxHeight = config.Bind(
                "UI",
                "PanelMaxHeight",
                DefaultMaxHeight,
                new ConfigDescription(
                    "Maximum height of the scrollable list area in pixels.",
                    new AcceptableValueRange<float>(56f, 950f)
                )
            );

            UseCustomPosition = config.Bind(
                "UI",
                "UseCustomPosition",
                false,
                "True when the user has dragged the Stellogistics panel to a custom screen position."
            );

            CustomPanelPosX = config.Bind(
                "UI",
                "CustomPanelPosX",
                DefaultPosX,
                "Saved X anchoredPosition (from top-left) of the Stellogistics panel."
            );

            CustomPanelPosY = config.Bind(
                "UI",
                "CustomPanelPosY",
                DefaultPosY,
                "Saved Y anchoredPosition (from top-left) of the Stellogistics panel."
            );

            DefaultCategoryFilter = config.Bind(
                "Filters",
                "DefaultCategoryFilter",
                EItemCategoryFilter.All,
                "Default item category filter for planets and star systems that do not have a custom per-body filter saved."
            );

            SavedAstroFilters = config.Bind(
                "Filters",
                "SavedAstroFilters",
                "",
                "Serialized per-star and per-planet item category filters (format: seed_astroId:mask,...)."
            );

            SavedAstroItemOverrides = config.Bind(
                "Filters",
                "SavedAstroItemOverrides",
                "",
                "Serialized per-star and per-planet individual item filter overrides (format: seed_astroId:id1|id2,...)."
            );

            SavedIgnoredAstros = config.Bind(
                "Filters",
                "SavedIgnoredAstros",
                "",
                "Serialized per-galaxy ignored planet and star system astroIds for the Finder (format: seed:astroId1|astroId2,...)."
            );

            ParseSavedAstroFilters();
            ParseSavedAstroItemOverrides();
            ParseSavedIgnoredAstros();
        }

        private const string GlobalFilterKey = "global";

        private static string MakeAstroKey(int galaxySeed, int astroId)
        {
            return GlobalFilterKey;
        }

        private void ParseSavedAstroFilters()
        {
            _astroFilterCache.Clear();
            string raw = SavedAstroFilters.Value;
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            string[] entries = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string[] kv = entries[i].Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1], out int maskInt))
                {
                    _astroFilterCache[kv[0].Trim()] = (EItemCategoryFilter)(maskInt & (int)(EItemCategoryFilter.All | EItemCategoryFilter.Exclude100Percent));
                }
            }
        }

        private void FlushSavedAstroFilters()
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, EItemCategoryFilter> kvp in _astroFilterCache)
            {
                if (sb.Length > 0)
                {
                    sb.Append(',');
                }
                sb.Append(kvp.Key).Append(':').Append((int)kvp.Value);
            }
            SavedAstroFilters.Value = sb.ToString();
            _configFile.Save();
        }

        private void ParseSavedAstroItemOverrides()
        {
            _astroItemOverridesCache.Clear();
            string raw = SavedAstroItemOverrides.Value;
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            string[] entries = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string[] kv = entries[i].Split(':');
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim();
                    string[] itemStrs = kv[1].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    var set = new HashSet<int>();
                    for (int j = 0; j < itemStrs.Length; j++)
                    {
                        if (int.TryParse(itemStrs[j], out int id) && id > 0)
                        {
                            set.Add(id);
                        }
                    }
                    if (set.Count > 0)
                    {
                        _astroItemOverridesCache[key] = set;
                    }
                }
            }
        }

        private void FlushSavedAstroItemOverrides()
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, HashSet<int>> kvp in _astroItemOverridesCache)
            {
                if (kvp.Value == null || kvp.Value.Count == 0)
                {
                    continue;
                }
                if (sb.Length > 0)
                {
                    sb.Append(',');
                }
                sb.Append(kvp.Key).Append(':');
                bool first = true;
                foreach (int id in kvp.Value)
                {
                    if (!first) sb.Append('|');
                    sb.Append(id);
                    first = false;
                }
            }
            SavedAstroItemOverrides.Value = sb.ToString();
            _configFile.Save();
        }

        public EItemCategoryFilter GetFilterForAstro(int galaxySeed, int astroId)
        {
            if (_astroFilterCache.TryGetValue(GlobalFilterKey, out EItemCategoryFilter mask))
            {
                return mask;
            }
            return DefaultCategoryFilter.Value;
        }

        public bool GetExclude100PercentForAstro(int galaxySeed, int astroId)
        {
            return (GetFilterForAstro(galaxySeed, astroId) & EItemCategoryFilter.Exclude100Percent) != 0;
        }

        public void ToggleExclude100PercentForAstro(int galaxySeed, int astroId)
        {
            EItemCategoryFilter current = GetFilterForAstro(galaxySeed, astroId);
            EItemCategoryFilter updated = (current & EItemCategoryFilter.Exclude100Percent) != 0
                ? (current & ~EItemCategoryFilter.Exclude100Percent)
                : (current | EItemCategoryFilter.Exclude100Percent);

            _astroFilterCache[GlobalFilterKey] = updated;
            DefaultCategoryFilter.Value = updated;
            FlushSavedAstroFilters();
        }

        public bool HasCustomFilterForAstro(int galaxySeed, int astroId)
        {
            EItemCategoryFilter mask = GetFilterForAstro(galaxySeed, astroId);
            bool hasNonDefaultMask = mask != EItemCategoryFilter.All;
            bool hasItemOverrides = _astroItemOverridesCache.TryGetValue(GlobalFilterKey, out HashSet<int> set) && set.Count > 0;
            return hasNonDefaultMask || hasItemOverrides;
        }

        private HashSet<int> GetOrInitHiddenItemsSet(int galaxySeed, int astroId)
        {
            if (!_astroItemOverridesCache.TryGetValue(GlobalFilterKey, out HashSet<int> hiddenItems))
            {
                hiddenItems = new HashSet<int>();
                EItemCategoryFilter catMask = GetFilterForAstro(galaxySeed, astroId) & EItemCategoryFilter.All;
                if (catMask != EItemCategoryFilter.All && LDB.items != null && LDB.items.dataArray != null)
                {
                    ItemProto[] allProtos = LDB.items.dataArray;
                    for (int i = 0; i < allProtos.Length; i++)
                    {
                        ItemProto proto = allProtos[i];
                        if (proto == null || proto.ID <= 0)
                        {
                            continue;
                        }
                        EItemCategoryFilter itemCat = CombinedLogisticsItem.ClassifyItem(proto);
                        if ((catMask & itemCat) == 0)
                        {
                            hiddenItems.Add(proto.ID);
                        }
                    }
                }
                _astroItemOverridesCache[GlobalFilterKey] = hiddenItems;
            }
            return hiddenItems;
        }

        public void SetFilterForAstro(int galaxySeed, int astroId, EItemCategoryFilter mask, EItemCategoryFilter? toggledCategories = null)
        {
            HashSet<int> hiddenItems = GetOrInitHiddenItemsSet(galaxySeed, astroId);
            EItemCategoryFilter existingExclude100 = GetFilterForAstro(galaxySeed, astroId) & EItemCategoryFilter.Exclude100Percent;
            EItemCategoryFilter clampedMask = (mask & EItemCategoryFilter.All) | existingExclude100;
            _astroFilterCache[GlobalFilterKey] = clampedMask;
            DefaultCategoryFilter.Value = clampedMask;

            if (toggledCategories.HasValue && LDB.items != null && LDB.items.dataArray != null)
            {
                EItemCategoryFilter changedCats = toggledCategories.Value;
                ItemProto[] allProtos = LDB.items.dataArray;
                for (int i = 0; i < allProtos.Length; i++)
                {
                    ItemProto proto = allProtos[i];
                    if (proto == null || proto.ID <= 0)
                    {
                        continue;
                    }
                    EItemCategoryFilter itemCat = CombinedLogisticsItem.ClassifyItem(proto);
                    if ((changedCats & itemCat) != 0)
                    {
                        if ((clampedMask & itemCat) != 0)
                        {
                            hiddenItems.Remove(proto.ID);
                        }
                        else
                        {
                            hiddenItems.Add(proto.ID);
                        }
                    }
                }
            }

            FlushSavedAstroItemOverrides();
            FlushSavedAstroFilters();
        }

        public void SetCustomItemsAllVisible(int galaxySeed, int astroId, bool visible)
        {
            HashSet<int> hiddenItems = GetOrInitHiddenItemsSet(galaxySeed, astroId);
            if (visible)
            {
                hiddenItems.Clear();
            }
            else if (LDB.items != null && LDB.items.dataArray != null)
            {
                ItemProto[] allProtos = LDB.items.dataArray;
                for (int i = 0; i < allProtos.Length; i++)
                {
                    ItemProto proto = allProtos[i];
                    if (proto != null && proto.ID > 0)
                    {
                        hiddenItems.Add(proto.ID);
                    }
                }
            }
            FlushSavedAstroItemOverrides();
        }

        public void CopyHiddenItemsSnapshot(int galaxySeed, int astroId, HashSet<int> destination)
        {
            if (destination == null)
            {
                return;
            }
            destination.Clear();
            HashSet<int> hiddenItems = GetOrInitHiddenItemsSet(galaxySeed, astroId);
            destination.UnionWith(hiddenItems);
        }

        public void RestoreHiddenItemsSnapshot(int galaxySeed, int astroId, HashSet<int> snapshot)
        {
            if (snapshot == null)
            {
                return;
            }
            HashSet<int> hiddenItems = GetOrInitHiddenItemsSet(galaxySeed, astroId);
            hiddenItems.Clear();
            hiddenItems.UnionWith(snapshot);
            FlushSavedAstroItemOverrides();
        }

        public void SetCustomSingleItemOnly(int galaxySeed, int astroId, int itemId)
        {
            if (itemId <= 0)
            {
                return;
            }
            HashSet<int> hiddenItems = GetOrInitHiddenItemsSet(galaxySeed, astroId);
            hiddenItems.Clear();
            if (LDB.items != null && LDB.items.dataArray != null)
            {
                ItemProto[] allProtos = LDB.items.dataArray;
                for (int i = 0; i < allProtos.Length; i++)
                {
                    ItemProto proto = allProtos[i];
                    if (proto != null && proto.ID > 0 && proto.ID != itemId)
                    {
                        hiddenItems.Add(proto.ID);
                    }
                }
            }
            FlushSavedAstroItemOverrides();
        }

        public void ResetFilterForAstro(int galaxySeed, int astroId)
        {
            DefaultCategoryFilter.Value = EItemCategoryFilter.All;
            _astroFilterCache.Clear();
            _astroItemOverridesCache.Clear();
            FlushSavedAstroFilters();
            FlushSavedAstroItemOverrides();
        }

        public bool IsItemVisibleForAstro(int galaxySeed, int astroId, int itemId, EItemCategoryFilter itemCategory)
        {
            if (_astroItemOverridesCache.TryGetValue(GlobalFilterKey, out HashSet<int> hiddenItems))
            {
                return !hiddenItems.Contains(itemId);
            }
            EItemCategoryFilter catMask = GetFilterForAstro(galaxySeed, astroId);
            return (catMask & itemCategory) != 0;
        }

        public void ToggleItemOverrideForAstro(int galaxySeed, int astroId, int itemId)
        {
            if (itemId <= 0)
            {
                return;
            }
            HashSet<int> hiddenItems = GetOrInitHiddenItemsSet(galaxySeed, astroId);
            if (!hiddenItems.Remove(itemId))
            {
                hiddenItems.Add(itemId);
            }
            FlushSavedAstroItemOverrides();
        }

        public void SetItemVisibleForAstro(int galaxySeed, int astroId, int itemId, bool visible)
        {
            if (itemId <= 0)
            {
                return;
            }
            HashSet<int> hiddenItems = GetOrInitHiddenItemsSet(galaxySeed, astroId);
            bool changed = visible ? hiddenItems.Remove(itemId) : hiddenItems.Add(itemId);
            if (changed)
            {
                FlushSavedAstroItemOverrides();
            }
        }

        private void ParseSavedIgnoredAstros()
        {
            _ignoredAstrosBySeed.Clear();
            string raw = SavedIgnoredAstros.Value;
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            string[] entries = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string[] kv = entries[i].Split(':');
                if (kv.Length == 2 && int.TryParse(kv[0].Trim(), out int seed))
                {
                    string[] idStrs = kv[1].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    var set = new HashSet<int>();
                    for (int j = 0; j < idStrs.Length; j++)
                    {
                        if (int.TryParse(idStrs[j], out int astroId) && astroId > 0)
                        {
                            set.Add(astroId);
                        }
                    }
                    if (set.Count > 0)
                    {
                        _ignoredAstrosBySeed[seed] = set;
                    }
                }
            }
        }

        private void FlushSavedIgnoredAstros()
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<int, HashSet<int>> kvp in _ignoredAstrosBySeed)
            {
                if (kvp.Value == null || kvp.Value.Count == 0)
                {
                    continue;
                }
                if (sb.Length > 0)
                {
                    sb.Append(',');
                }
                sb.Append(kvp.Key).Append(':');
                bool first = true;
                foreach (int astroId in kvp.Value)
                {
                    if (!first) sb.Append('|');
                    sb.Append(astroId);
                    first = false;
                }
            }
            SavedIgnoredAstros.Value = sb.ToString();
            _configFile.Save();
        }

        public bool IsAstroIgnored(int galaxySeed, int astroId)
        {
            if (astroId <= 0)
            {
                return false;
            }
            return _ignoredAstrosBySeed.TryGetValue(galaxySeed, out HashSet<int> set) && set.Contains(astroId);
        }

        public bool IsPlanetOrStarIgnored(int galaxySeed, PlanetData planet)
        {
            if (planet == null)
            {
                return false;
            }
            if (IsAstroIgnored(galaxySeed, planet.astroId))
            {
                return true;
            }
            if (planet.star != null && IsAstroIgnored(galaxySeed, planet.star.astroId))
            {
                return true;
            }
            return false;
        }

        public void ToggleAstroIgnored(int galaxySeed, int astroId)
        {
            if (astroId <= 0)
            {
                return;
            }
            if (!_ignoredAstrosBySeed.TryGetValue(galaxySeed, out HashSet<int> set))
            {
                set = new HashSet<int>();
                _ignoredAstrosBySeed[galaxySeed] = set;
            }
            if (!set.Remove(astroId))
            {
                set.Add(astroId);
            }
            FlushSavedIgnoredAstros();
        }

        public void SetAstroIgnored(int galaxySeed, int astroId, bool ignored)
        {
            if (astroId <= 0)
            {
                return;
            }
            if (!_ignoredAstrosBySeed.TryGetValue(galaxySeed, out HashSet<int> set))
            {
                if (!ignored) return;
                set = new HashSet<int>();
                _ignoredAstrosBySeed[galaxySeed] = set;
            }
            bool changed = ignored ? set.Add(astroId) : set.Remove(astroId);
            if (changed)
            {
                FlushSavedIgnoredAstros();
            }
        }

        public void ClearAllIgnoredAstros(int galaxySeed)
        {
            if (_ignoredAstrosBySeed.Remove(galaxySeed))
            {
                FlushSavedIgnoredAstros();
            }
        }

        public int GetIgnoredAstroCount(int galaxySeed)
        {
            return _ignoredAstrosBySeed.TryGetValue(galaxySeed, out HashSet<int> set) ? set.Count : 0;
        }

        public void SaveCustomPanelPosition(float x, float y)
        {
            UseCustomPosition.Value = true;
            CustomPanelPosX.Value = x;
            CustomPanelPosY.Value = y;
            _configFile.Save();
        }

        public void SavePanelSize(float width, float maxHeight)
        {
            PanelWidth.Value = width;
            PanelMaxHeight.Value = maxHeight;
            _configFile.Save();
        }

        public void ResetPanelLayout()
        {
            UseCustomPosition.Value = false;
            CustomPanelPosX.Value = DefaultPosX;
            CustomPanelPosY.Value = DefaultPosY;
            PanelWidth.Value = DefaultWidth;
            PanelMaxHeight.Value = DefaultMaxHeight;
            _configFile.Save();
        }
    }
}
