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

        private readonly Dictionary<string, EItemCategoryFilter> _astroFilterCache = new Dictionary<string, EItemCategoryFilter>();
        private readonly Dictionary<string, HashSet<int>> _astroItemOverridesCache = new Dictionary<string, HashSet<int>>();

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
                "ShowOnHover",
                true,
                "Show logistics when hovering a planet or star in the Starmap even if not clicked/selected."
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
                    new AcceptableValueRange<float>(120f, 950f)
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

            ParseSavedAstroFilters();
            ParseSavedAstroItemOverrides();
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
