using System;
using System.Collections.Generic;

namespace Stellogistics
{
    public static class LogisticsAggregator
    {
        private const double OneLightYearMeters = 2_390_000.0;
        private const int WarperItemId = 1210;

        public static LogisticsSummary AggregatePlanet(PlanetData planet, StellogisticsConfig config)
        {
            int galaxySeed = GameMain.galaxy != null ? GameMain.galaxy.seed : 0;
            int astroId = planet != null ? planet.astroId : 0;
            EItemCategoryFilter categoryFilter = config.GetFilterForAstro(galaxySeed, astroId);

            var summary = new LogisticsSummary
            {
                IsStarSystem = false,
                TargetAstroId = astroId,
                GalaxySeed = galaxySeed,
                ActiveCategoryFilter = categoryFilter,
                TargetName = planet != null ? planet.displayName : "Unknown Planet"
            };

            if (planet == null || GameMain.data == null)
            {
                return summary;
            }

            var exportsByItem = new Dictionary<int, CombinedLogisticsItem>();
            var importsByItem = new Dictionary<int, CombinedLogisticsItem>();

            StationComponent[] gStationPool = GameMain.data.galacticTransport?.stationPool;
            int starId = planet.star != null ? planet.star.id : (planet.id / 100);

            AggregatePlanetStations(
                planet,
                starId,
                config.IncludeOrbitalCollectors.Value,
                EStarFilterMode.AllRemoteSlots,
                gStationPool,
                exportsByItem,
                importsByItem,
                null,
                null,
                ref summary.TotalIlsCount,
                ref summary.TotalCollectorCount,
                ref summary.TotalWarperCount,
                ref summary.TotalIdleShips,
                ref summary.TotalWorkShips
            );

            summary.ColonizedPlanetCount = summary.HasAnyStations ? 1 : 0;
            FinalizeSummary(summary, exportsByItem, importsByItem, config);

            string collectorPart = summary.TotalCollectorCount > 0
                ? $", {summary.TotalCollectorCount} OC"
                : "";
            summary.TargetSubtitle = $"Planet • {summary.TotalIlsCount} ILS{collectorPart}";

            return summary;
        }

        public static LogisticsSummary AggregateStar(StarData star, StellogisticsConfig config)
        {
            int galaxySeed = GameMain.galaxy != null ? GameMain.galaxy.seed : 0;
            int astroId = star != null ? star.astroId : 0;
            EItemCategoryFilter categoryFilter = config.GetFilterForAstro(galaxySeed, astroId);

            var summary = new LogisticsSummary
            {
                IsStarSystem = true,
                TargetAstroId = astroId,
                GalaxySeed = galaxySeed,
                ActiveCategoryFilter = categoryFilter,
                TargetName = star != null ? $"{star.displayName} System" : "Unknown System"
            };

            if (star == null || star.planets == null || GameMain.data == null)
            {
                return summary;
            }

            var exportsByItem = new Dictionary<int, CombinedLogisticsItem>();
            var importsByItem = new Dictionary<int, CombinedLogisticsItem>();
            var exportPlanetsByItem = new Dictionary<int, HashSet<int>>();
            var importPlanetsByItem = new Dictionary<int, HashSet<int>>();

            StationComponent[] gStationPool = GameMain.data.galacticTransport?.stationPool;
            EStarFilterMode filterMode = config.StarSystemFilterMode.Value;
            bool includeCollectors = config.IncludeOrbitalCollectors.Value;

            for (int i = 0; i < star.planets.Length; i++)
            {
                PlanetData planet = star.planets[i];
                if (planet == null)
                {
                    continue;
                }

                int prevIls = summary.TotalIlsCount;
                int prevCol = summary.TotalCollectorCount;

                AggregatePlanetStations(
                    planet,
                    star.id,
                    includeCollectors,
                    filterMode,
                    gStationPool,
                    exportsByItem,
                    importsByItem,
                    exportPlanetsByItem,
                    importPlanetsByItem,
                    ref summary.TotalIlsCount,
                    ref summary.TotalCollectorCount,
                    ref summary.TotalWarperCount,
                    ref summary.TotalIdleShips,
                    ref summary.TotalWorkShips
                );

                if (summary.TotalIlsCount > prevIls || summary.TotalCollectorCount > prevCol)
                {
                    summary.ColonizedPlanetCount++;
                }
            }

            if (filterMode == EStarFilterMode.InterstellarSmart)
            {
                ApplyInterstellarSmartFilter(exportsByItem, importsByItem);
            }
            else if (filterMode == EStarFilterMode.NetOutIntraSystem)
            {
                ApplyNetOutIntraSystemFilter(exportsByItem, importsByItem);
            }

            FinalizeSummary(summary, exportsByItem, importsByItem, config);

            string modeLabel = filterMode switch
            {
                EStarFilterMode.InterstellarSmart => "Interstellar",
                EStarFilterMode.NetOutIntraSystem => "Netted",
                _ => "All Remote"
            };
            string collectorPart = summary.TotalCollectorCount > 0 ? $" +{summary.TotalCollectorCount} OC" : "";
            summary.TargetSubtitle = $"{modeLabel} • {summary.ColonizedPlanetCount}/{star.planets.Length} Planets • {summary.TotalIlsCount} ILS{collectorPart}";

            return summary;
        }

        private static void AggregatePlanetStations(
            PlanetData planet,
            int starId,
            bool includeCollectors,
            EStarFilterMode filterMode,
            StationComponent[] gStationPool,
            Dictionary<int, CombinedLogisticsItem> exportsByItem,
            Dictionary<int, CombinedLogisticsItem> importsByItem,
            Dictionary<int, HashSet<int>> exportPlanetsByItem,
            Dictionary<int, HashSet<int>> importPlanetsByItem,
            ref int totalIlsCount,
            ref int totalCollectorCount,
            ref int totalWarperCount,
            ref int totalIdleShips,
            ref int totalWorkShips)
        {
            StationComponent[] stationPool = planet.factory?.transport?.stationPool;
            int stationCursor = planet.factory?.transport != null ? planet.factory.transport.stationCursor : 0;

            if (stationPool == null || stationCursor <= 0)
            {
                return;
            }

            for (int i = 1; i < stationCursor; i++)
            {
                StationComponent station = stationPool[i];
                if (station == null || station.id != i || !station.isStellar)
                {
                    continue;
                }

                if (station.isCollector)
                {
                    if (!includeCollectors)
                    {
                        continue;
                    }
                    totalCollectorCount++;
                }
                else
                {
                    totalIlsCount++;
                    totalWarperCount += station.warperCount;
                    totalIdleShips += station.idleShipCount;
                    totalWorkShips += station.workShipCount;
                }

                StationStore[] storage = station.storage;
                if (storage == null)
                {
                    continue;
                }

                bool stationRangeAllowsInterstellar = station.isCollector || station.tripRangeShips >= OneLightYearMeters;
                bool stationHasShipsAndWarpers = !station.isCollector &&
                    (station.idleShipCount + station.workShipCount > 0) &&
                    stationRangeAllowsInterstellar &&
                    (!station.warperNecessary || station.warperCount > 0 || station.warperFree || HasWarperSlot(storage));

                for (int s = 0; s < storage.Length; s++)
                {
                    StationStore store = storage[s];
                    if (store.itemId <= 0 || store.remoteLogic == ELogisticStorage.None)
                    {
                        continue;
                    }

                    bool hasExternalPair = false;
                    bool hasInternalPair = false;

                    if (filterMode == EStarFilterMode.InterstellarSmart && gStationPool != null)
                    {
                        InspectSlotRemotePairs(station, s, starId, gStationPool, out hasExternalPair, out hasInternalPair);

                        if (hasInternalPair && !hasExternalPair && !stationHasShipsAndWarpers)
                        {
                            continue;
                        }
                        if (store.remoteLogic == ELogisticStorage.Demand && !stationRangeAllowsInterstellar && !hasExternalPair)
                        {
                            continue;
                        }
                    }

                    if (store.remoteLogic == ELogisticStorage.Supply)
                    {
                        AccumulateSlot(
                            exportsByItem,
                            exportPlanetsByItem,
                            planet.id,
                            store,
                            station.isCollector,
                            hasExternalPair,
                            stationHasShipsAndWarpers
                        );
                    }
                    else if (store.remoteLogic == ELogisticStorage.Demand)
                    {
                        AccumulateSlot(
                            importsByItem,
                            importPlanetsByItem,
                            planet.id,
                            store,
                            station.isCollector,
                            hasExternalPair,
                            stationHasShipsAndWarpers
                        );
                    }
                }
            }
        }

        private static bool HasWarperSlot(StationStore[] storage)
        {
            for (int i = 0; i < storage.Length; i++)
            {
                if (storage[i].itemId == WarperItemId && storage[i].count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static void InspectSlotRemotePairs(
            StationComponent station,
            int slotIndex,
            int starId,
            StationComponent[] gStationPool,
            out bool hasExternalPair,
            out bool hasInternalPair)
        {
            hasExternalPair = false;
            hasInternalPair = false;

            SupplyDemandPair[] pairs = station.remotePairs;
            if (pairs == null)
            {
                return;
            }

            int pairCount = Math.Min(station.remotePairTotalCount, pairs.Length);
            int selfGid = station.gid;

            for (int i = 0; i < pairCount; i++)
            {
                SupplyDemandPair pair = pairs[i];
                int otherGid = -1;

                if (pair.supplyId == selfGid && pair.supplyIndex == slotIndex)
                {
                    otherGid = pair.demandId;
                }
                else if (pair.demandId == selfGid && pair.demandIndex == slotIndex)
                {
                    otherGid = pair.supplyId;
                }

                if (otherGid > 0 && otherGid < gStationPool.Length)
                {
                    StationComponent other = gStationPool[otherGid];
                    if (other != null && other.gid == otherGid)
                    {
                        int otherStarId = other.planetId / 100;
                        if (otherStarId != starId)
                        {
                            hasExternalPair = true;
                        }
                        else
                        {
                            hasInternalPair = true;
                        }
                    }
                }
            }
        }

        private static void AccumulateSlot(
            Dictionary<int, CombinedLogisticsItem> dict,
            Dictionary<int, HashSet<int>> planetsByItem,
            int planetId,
            StationStore store,
            bool isCollector,
            bool hasExternalPair,
            bool hasWarpCapableStation)
        {
            if (!dict.TryGetValue(store.itemId, out CombinedLogisticsItem combined))
            {
                ItemProto proto = LDB.items?.Select(store.itemId);
                combined = new CombinedLogisticsItem
                {
                    ItemId = store.itemId,
                    ItemName = proto != null ? proto.name : $"Item #{store.itemId}",
                    Icon = proto != null ? proto.iconSprite : null,
                    Category = CombinedLogisticsItem.ClassifyItem(proto),
                    Logic = store.remoteLogic,
                    PlanetCount = 1
                };
                dict[store.itemId] = combined;
            }

            combined.StoredCount += store.count;
            combined.MaxCount += store.max;
            combined.InTransitOrder += store.remoteOrder;
            combined.SlotCount++;
            if (isCollector)
            {
                combined.CollectorSlotCount++;
            }
            if (hasExternalPair)
            {
                combined.HasExternalStarPair = true;
            }
            if (hasWarpCapableStation)
            {
                combined.HasWarpCapableStation = true;
            }

            if (planetsByItem != null)
            {
                if (!planetsByItem.TryGetValue(store.itemId, out HashSet<int> planetSet))
                {
                    planetSet = new HashSet<int>();
                    planetsByItem[store.itemId] = planetSet;
                }
                planetSet.Add(planetId);
                combined.PlanetCount = planetSet.Count;
            }
        }

        private static void ApplyInterstellarSmartFilter(
            Dictionary<int, CombinedLogisticsItem> exportsByItem,
            Dictionary<int, CombinedLogisticsItem> importsByItem)
        {
            var dualItemIds = new List<int>();
            foreach (int itemId in exportsByItem.Keys)
            {
                if (importsByItem.ContainsKey(itemId))
                {
                    dualItemIds.Add(itemId);
                }
            }

            for (int i = 0; i < dualItemIds.Count; i++)
            {
                int itemId = dualItemIds[i];
                CombinedLogisticsItem exp = exportsByItem[itemId];
                CombinedLogisticsItem imp = importsByItem[itemId];

                if (exp.HasExternalStarPair && !imp.HasExternalStarPair)
                {
                    importsByItem.Remove(itemId);
                }
                else if (imp.HasExternalStarPair && !exp.HasExternalStarPair)
                {
                    exportsByItem.Remove(itemId);
                }
                else if (!exp.HasExternalStarPair && !imp.HasExternalStarPair)
                {
                    exportsByItem.Remove(itemId);
                    importsByItem.Remove(itemId);
                }
            }
        }

        private static void ApplyNetOutIntraSystemFilter(
            Dictionary<int, CombinedLogisticsItem> exportsByItem,
            Dictionary<int, CombinedLogisticsItem> importsByItem)
        {
            var dualItemIds = new List<int>();
            foreach (int itemId in exportsByItem.Keys)
            {
                if (importsByItem.ContainsKey(itemId))
                {
                    dualItemIds.Add(itemId);
                }
            }

            for (int i = 0; i < dualItemIds.Count; i++)
            {
                int itemId = dualItemIds[i];
                CombinedLogisticsItem exp = exportsByItem[itemId];
                CombinedLogisticsItem imp = importsByItem[itemId];

                int nonCollectorSupplySlots = exp.SlotCount - exp.CollectorSlotCount;
                if (nonCollectorSupplySlots <= imp.SlotCount && exp.CollectorSlotCount == 0)
                {
                    exportsByItem.Remove(itemId);
                    if (imp.SlotCount <= nonCollectorSupplySlots)
                    {
                        importsByItem.Remove(itemId);
                    }
                }
                else
                {
                    importsByItem.Remove(itemId);
                }
            }
        }

        private static void FinalizeSummary(
            LogisticsSummary summary,
            Dictionary<int, CombinedLogisticsItem> exportsByItem,
            Dictionary<int, CombinedLogisticsItem> importsByItem,
            StellogisticsConfig config)
        {
            int critPct = config.CriticalThresholdPercent.Value;
            int warnPct = config.WarningThresholdPercent.Value;
            int excludeThresholdPct = UnityEngine.Mathf.Clamp(config.ExcludeFullThresholdPercent.Value, 40, 100);
            ESortOrder sortOrder = config.SortOrder.Value;
            bool excludeFull = config.GetExclude100PercentForAstro(summary.GalaxySeed, summary.TargetAstroId);

            foreach (CombinedLogisticsItem item in exportsByItem.Values)
            {
                bool isFullOrTapped = item.DisplayPercent >= excludeThresholdPct;
                if (!config.IsItemVisibleForAstro(summary.GalaxySeed, summary.TargetAstroId, item.ItemId, item.Category) ||
                    (excludeFull && isFullOrTapped))
                {
                    summary.FilteredOutItemCount++;
                    continue;
                }
                summary.Exports.Add(item);
                EHealthStatus status = item.GetHealthStatus(critPct, warnPct);
                if (status == EHealthStatus.Critical) summary.CriticalExportCount++;
                else if (status == EHealthStatus.Warning) summary.WarningExportCount++;
            }

            foreach (CombinedLogisticsItem item in importsByItem.Values)
            {
                bool isFullOrTapped = item.DisplayPercent >= excludeThresholdPct;
                if (!config.IsItemVisibleForAstro(summary.GalaxySeed, summary.TargetAstroId, item.ItemId, item.Category) ||
                    (excludeFull && isFullOrTapped))
                {
                    summary.FilteredOutItemCount++;
                    continue;
                }
                summary.Imports.Add(item);
                EHealthStatus status = item.GetHealthStatus(critPct, warnPct);
                if (status == EHealthStatus.Critical) summary.CriticalImportCount++;
                else if (status == EHealthStatus.Warning) summary.WarningImportCount++;
            }

            Comparison<CombinedLogisticsItem> comparer = (a, b) =>
            {
                if (sortOrder == ESortOrder.ShortageFirst)
                {
                    int pctCmp = a.DisplayPercent.CompareTo(b.DisplayPercent);
                    if (pctCmp != 0)
                    {
                        return pctCmp;
                    }
                    int ratioCmp = a.FillRatio.CompareTo(b.FillRatio);
                    if (ratioCmp != 0)
                    {
                        return ratioCmp;
                    }
                    int maxCmp = b.MaxCount.CompareTo(a.MaxCount);
                    if (maxCmp != 0)
                    {
                        return maxCmp;
                    }
                }
                else if (sortOrder == ESortOrder.StoredAmountDesc)
                {
                    int pctCmp = b.DisplayPercent.CompareTo(a.DisplayPercent);
                    if (pctCmp != 0)
                    {
                        return pctCmp;
                    }
                    int ratioCmp = b.FillRatio.CompareTo(a.FillRatio);
                    if (ratioCmp != 0)
                    {
                        return ratioCmp;
                    }
                    int amtCmp = b.StoredCount.CompareTo(a.StoredCount);
                    if (amtCmp != 0)
                    {
                        return amtCmp;
                    }
                }
                return a.ItemId.CompareTo(b.ItemId);
            };

            summary.Exports.Sort(comparer);
            summary.Imports.Sort(comparer);
        }

        /// <summary>
        /// Populates <paramref name="result"/> with every item/building ID that has a slot, filter, or stored stack
        /// in any Logistics Station (ILS, PLS, Collector), Logistics Distributor, Storage Box, or Fluid Tank
        /// anywhere in the galaxy so unstored/unresearched items are not spoiled in the Custom Item Picker.
        /// </summary>
        public static void CollectStoredItemIds(HashSet<int> result)
        {
            result.Clear();
            GameData data = GameMain.data;
            if (data == null || data.factories == null)
            {
                return;
            }

            int factoryCount = Math.Min(data.factoryCount, data.factories.Length);
            for (int f = 0; f < factoryCount; f++)
            {
                PlanetFactory factory = data.factories[f];
                if (factory == null)
                {
                    continue;
                }

                PlanetTransport transport = factory.transport;
                if (transport != null)
                {
                    StationComponent[] stationPool = transport.stationPool;
                    if (stationPool != null)
                    {
                        int cursor = Math.Min(transport.stationCursor, stationPool.Length);
                        for (int i = 1; i < cursor; i++)
                        {
                            StationComponent sc = stationPool[i];
                            if (sc == null || sc.id != i || sc.storage == null)
                            {
                                continue;
                            }
                            for (int s = 0; s < sc.storage.Length; s++)
                            {
                                int itemId = sc.storage[s].itemId;
                                if (itemId > 0)
                                {
                                    result.Add(itemId);
                                }
                            }
                        }
                    }

                    DispenserComponent[] dispenserPool = transport.dispenserPool;
                    if (dispenserPool != null)
                    {
                        int cursor = Math.Min(transport.dispenserCursor, dispenserPool.Length);
                        for (int i = 1; i < cursor; i++)
                        {
                            DispenserComponent dc = dispenserPool[i];
                            if (dc != null && dc.id == i && dc.filter > 0)
                            {
                                result.Add(dc.filter);
                            }
                        }
                    }
                }

                FactoryStorage fs = factory.factoryStorage;
                if (fs != null)
                {
                    StorageComponent[] storagePool = fs.storagePool;
                    if (storagePool != null)
                    {
                        int cursor = Math.Min(fs.storageCursor, storagePool.Length);
                        for (int i = 1; i < cursor; i++)
                        {
                            StorageComponent st = storagePool[i];
                            if (st == null || st.id != i || st.grids == null)
                            {
                                continue;
                            }
                            for (int g = 0; g < st.grids.Length; g++)
                            {
                                int id = st.grids[g].itemId;
                                if (id > 0)
                                {
                                    result.Add(id);
                                }
                                int filterId = st.grids[g].filter;
                                if (filterId > 0)
                                {
                                    result.Add(filterId);
                                }
                            }
                        }
                    }

                    TankComponent[] tankPool = fs.tankPool;
                    if (tankPool != null)
                    {
                        int cursor = Math.Min(fs.tankCursor, tankPool.Length);
                        for (int i = 1; i < cursor; i++)
                        {
                            TankComponent tk = tankPool[i];
                            if (tk.id == i && tk.fluidId > 0)
                            {
                                result.Add(tk.fluidId);
                            }
                        }
                    }
                }
            }
        }
    }
}
