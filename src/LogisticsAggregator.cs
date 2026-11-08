using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stellogistics
{
    public static class LogisticsAggregator
    {
        private const double OneLightYearMeters = 2_390_000.0;
        private const int WarperItemId = 1210;

        private struct SupplyAvailability
        {
            public int HealthySupplierPlanetCount;
            public int SingleSupplierPlanetId;
            public int SingleSupplierStarId;
            public bool MultipleStarSystems;
        }

        private struct StationTotals
        {
            public int TotalIlsCount;
            public int TotalCollectorCount;
            public int TotalWarperCount;
            public int TotalIdleShips;
            public int TotalWorkShips;
        }

        // Cached stored item IDs (TTL 5.0s)
        private static readonly HashSet<int> _cachedStoredIds = new HashSet<int>();
        private static float _storedIdsCacheExpiryTime = -1f;
        private static int _storedIdsCachedSeed = -1;

        // Cached recipe source ingredient IDs per output item ID
        private static readonly Dictionary<int, List<int>> _cachedSourceIngredients = new Dictionary<int, List<int>>();

        // Cached galaxy supply availability (TTL 1.0s)
        private static readonly Dictionary<int, SupplyAvailability> _cachedSupplyAvailability = new Dictionary<int, SupplyAvailability>();
        private static float _supplyCacheExpiryTime = -1f;
        private static int _supplyCachedSeed = -1;
        private static bool _supplyCachedIncludeOc;
        private static int _supplyCachedCritPct = -1;
        private static int _supplyCachedWarnPct = -1;

        public static void InvalidateCaches()
        {
            _storedIdsCacheExpiryTime = -1f;
            _supplyCacheExpiryTime = -1f;
        }

        private static void ForEachLogisticsPlanet(Action<StarData, PlanetData, StationComponent[], int> action)
        {
            StarData[] stars = GameMain.galaxy?.stars;
            if (stars == null || GameMain.data == null)
            {
                return;
            }

            for (int s = 0; s < stars.Length; s++)
            {
                StarData star = stars[s];
                if (star?.planets == null)
                {
                    continue;
                }

                for (int p = 0; p < star.planets.Length; p++)
                {
                    PlanetData planet = star.planets[p];
                    PlanetTransport transport = planet?.factory?.transport;
                    StationComponent[] stationPool = transport?.stationPool;
                    if (stationPool == null || transport.stationCursor <= 0)
                    {
                        continue;
                    }

                    int cursor = Math.Min(transport.stationCursor, stationPool.Length);
                    if (cursor > 1)
                    {
                        action(star, planet, stationPool, cursor);
                    }
                }
            }
        }

        public static LogisticsSummary AggregatePlanet(PlanetData planet, StellogisticsConfig config)
        {
            int galaxySeed = GameMain.galaxy != null ? GameMain.galaxy.seed : 0;
            int astroId = planet != null ? planet.astroId : 0;

            var summary = new LogisticsSummary
            {
                IsStarSystem = false,
                TargetAstroId = astroId,
                GalaxySeed = galaxySeed,
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

            StationTotals totals = default;
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
                ref totals
            );

            ApplyTotalsToSummary(summary, totals);
            summary.ColonizedPlanetCount = summary.HasAnyStations ? 1 : 0;
            LinkHubItems(exportsByItem, importsByItem);
            FinalizeSummary(summary, exportsByItem, importsByItem, config, planet.id, 0);

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

            var summary = new LogisticsSummary
            {
                IsStarSystem = true,
                TargetAstroId = astroId,
                GalaxySeed = galaxySeed,
                TargetName = star != null ? $"{star.displayName} System" : "Unknown System"
            };

            if (star?.planets == null || GameMain.data == null)
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

            StationTotals totals = default;
            for (int i = 0; i < star.planets.Length; i++)
            {
                PlanetData planet = star.planets[i];
                if (planet == null)
                {
                    continue;
                }

                int prevIls = totals.TotalIlsCount;
                int prevCol = totals.TotalCollectorCount;

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
                    ref totals
                );

                if (totals.TotalIlsCount > prevIls || totals.TotalCollectorCount > prevCol)
                {
                    summary.ColonizedPlanetCount++;
                }
            }

            ApplyTotalsToSummary(summary, totals);

            if (filterMode == EStarFilterMode.InterstellarSmart)
            {
                ApplyInterstellarSmartFilter(exportsByItem, importsByItem);
            }
            else if (filterMode == EStarFilterMode.NetOutIntraSystem)
            {
                ApplyNetOutIntraSystemFilter(exportsByItem, importsByItem);
            }

            LinkHubItems(exportsByItem, importsByItem);
            FinalizeSummary(summary, exportsByItem, importsByItem, config, 0, star.id);

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

        private static void ApplyTotalsToSummary(LogisticsSummary summary, StationTotals totals)
        {
            summary.TotalIlsCount = totals.TotalIlsCount;
            summary.TotalCollectorCount = totals.TotalCollectorCount;
            summary.TotalWarperCount = totals.TotalWarperCount;
            summary.TotalIdleShips = totals.TotalIdleShips;
            summary.TotalWorkShips = totals.TotalWorkShips;
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
            ref StationTotals totals)
        {
            PlanetTransport transport = planet?.factory?.transport;
            StationComponent[] stationPool = transport?.stationPool;
            if (stationPool == null || transport.stationCursor <= 0)
            {
                return;
            }

            int cursor = Math.Min(transport.stationCursor, stationPool.Length);
            for (int i = 1; i < cursor; i++)
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
                    totals.TotalCollectorCount++;
                }
                else
                {
                    totals.TotalIlsCount++;
                    totals.TotalWarperCount += station.warperCount;
                    totals.TotalIdleShips += station.idleShipCount;
                    totals.TotalWorkShips += station.workShipCount;
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
                        AccumulateSlot(exportsByItem, exportPlanetsByItem, planet.id, store, station.isCollector, hasExternalPair);
                    }
                    else if (store.remoteLogic == ELogisticStorage.Demand)
                    {
                        AccumulateSlot(importsByItem, importPlanetsByItem, planet.id, store, station.isCollector, hasExternalPair);
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
            bool hasExternalPair)
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

        private static void LinkHubItems(
            Dictionary<int, CombinedLogisticsItem> exportsByItem,
            Dictionary<int, CombinedLogisticsItem> importsByItem)
        {
            foreach (KeyValuePair<int, CombinedLogisticsItem> kvp in exportsByItem)
            {
                if (importsByItem.TryGetValue(kvp.Key, out CombinedLogisticsItem impItem))
                {
                    CombinedLogisticsItem expItem = kvp.Value;
                    expItem.IsHub = true;
                    expItem.HubCounterpartMax = impItem.MaxCount;

                    impItem.IsHub = true;
                    impItem.HubCounterpartMax = expItem.StoredCount > 0 || expItem.MaxCount > 0 ? expItem.MaxCount : 0;
                }
            }
        }

        private static void RecordPlanetSupplyAvailability(
            Dictionary<int, SupplyAvailability> supplyByItem,
            int planetId,
            int starId,
            CombinedLogisticsItem expItem,
            CombinedLogisticsItem impItemOnSamePlanet,
            int critPct,
            int warnPct)
        {
            if (expItem == null || expItem.StoredCount <= 0)
            {
                return;
            }

            EHealthStatus expStatus = expItem.GetHealthStatus(critPct, warnPct);
            bool isStarvingHub = impItemOnSamePlanet != null &&
                impItemOnSamePlanet.GetHealthStatus(critPct, warnPct) != EHealthStatus.Healthy &&
                impItemOnSamePlanet.MaxCount > expItem.MaxCount;
            if (isStarvingHub)
            {
                return;
            }

            long unclaimedStock = expItem.StoredCount + expItem.InTransitOrder;
            if (expStatus != EHealthStatus.Healthy && unclaimedStock < 400)
            {
                return;
            }

            if (!supplyByItem.TryGetValue(expItem.ItemId, out SupplyAvailability avail))
            {
                avail.HealthySupplierPlanetCount = 1;
                avail.SingleSupplierPlanetId = planetId;
                avail.SingleSupplierStarId = starId;
                avail.MultipleStarSystems = false;
            }
            else
            {
                if (avail.HealthySupplierPlanetCount == 1 && avail.SingleSupplierPlanetId != planetId)
                {
                    avail.HealthySupplierPlanetCount = 2;
                }
                else if (avail.HealthySupplierPlanetCount > 1)
                {
                    avail.HealthySupplierPlanetCount++;
                }
                if (avail.SingleSupplierStarId != starId)
                {
                    avail.MultipleStarSystems = true;
                }
            }
            supplyByItem[expItem.ItemId] = avail;
        }

        private static bool HasExternalSupply(
            Dictionary<int, SupplyAvailability> supplyByItem,
            int itemId,
            int targetPlanetId,
            int targetStarId)
        {
            if (!supplyByItem.TryGetValue(itemId, out SupplyAvailability avail) || avail.HealthySupplierPlanetCount <= 0)
            {
                return false;
            }
            if (targetPlanetId > 0)
            {
                return avail.HealthySupplierPlanetCount >= 2 || avail.SingleSupplierPlanetId != targetPlanetId;
            }
            if (targetStarId > 0)
            {
                return avail.MultipleStarSystems || avail.SingleSupplierStarId != targetStarId;
            }
            return true;
        }

        private static Dictionary<int, SupplyAvailability> GetOrCollectGalaxySupplyAvailability(
            StellogisticsConfig config,
            int galaxySeed,
            int critPct,
            int warnPct)
        {
            float now = Time.unscaledTime;
            bool includeCollectors = config.IncludeOrbitalCollectors.Value;

            if (now < _supplyCacheExpiryTime &&
                _supplyCachedSeed == galaxySeed &&
                _supplyCachedIncludeOc == includeCollectors &&
                _supplyCachedCritPct == critPct &&
                _supplyCachedWarnPct == warnPct)
            {
                return _cachedSupplyAvailability;
            }

            _cachedSupplyAvailability.Clear();
            _supplyCachedSeed = galaxySeed;
            _supplyCachedIncludeOc = includeCollectors;
            _supplyCachedCritPct = critPct;
            _supplyCachedWarnPct = warnPct;
            _supplyCacheExpiryTime = now + 1.0f;

            var planetExports = new Dictionary<int, CombinedLogisticsItem>();
            var planetImports = new Dictionary<int, CombinedLogisticsItem>();

            ForEachLogisticsPlanet((star, planet, _, __) =>
            {
                if (config.IsPlanetOrStarIgnored(galaxySeed, planet))
                {
                    return;
                }

                planetExports.Clear();
                planetImports.Clear();
                StationTotals dummy = default;
                AggregatePlanetStations(
                    planet,
                    star.id,
                    includeCollectors,
                    EStarFilterMode.AllRemoteSlots,
                    null,
                    planetExports,
                    planetImports,
                    null,
                    null,
                    ref dummy
                );

                foreach (CombinedLogisticsItem expItem in planetExports.Values)
                {
                    planetImports.TryGetValue(expItem.ItemId, out CombinedLogisticsItem impOnSame);
                    RecordPlanetSupplyAvailability(_cachedSupplyAvailability, planet.id, star.id, expItem, impOnSame, critPct, warnPct);
                }
            });

            return _cachedSupplyAvailability;
        }

        private static void EvaluateImportBottleneck(
            CombinedLogisticsItem impItem,
            EHealthStatus status,
            Dictionary<int, SupplyAvailability> supplyByItem,
            int planetId,
            int starId)
        {
            if (impItem == null || impItem.Logic != ELogisticStorage.Demand || impItem.MaxCount <= 0)
            {
                return;
            }
            if (status != EHealthStatus.Healthy &&
                (impItem.FillRatio + impItem.InboundTransitRatio) < 0.85f &&
                HasExternalSupply(supplyByItem, impItem.ItemId, planetId, starId))
            {
                impItem.HasLogisticsBottleneck = true;
            }
        }

        private static int CompareItems(CombinedLogisticsItem a, CombinedLogisticsItem b, ESortOrder sortOrder)
        {
            if (sortOrder == ESortOrder.ShortageFirst)
            {
                int pctCmp = a.DisplayPercent.CompareTo(b.DisplayPercent);
                if (pctCmp != 0) return pctCmp;
                int ratioCmp = a.FillRatio.CompareTo(b.FillRatio);
                if (ratioCmp != 0) return ratioCmp;
                int maxCmp = b.MaxCount.CompareTo(a.MaxCount);
                if (maxCmp != 0) return maxCmp;
            }
            else if (sortOrder == ESortOrder.StoredAmountDesc)
            {
                int pctCmp = b.DisplayPercent.CompareTo(a.DisplayPercent);
                if (pctCmp != 0) return pctCmp;
                int ratioCmp = b.FillRatio.CompareTo(a.FillRatio);
                if (ratioCmp != 0) return ratioCmp;
                int amtCmp = b.StoredCount.CompareTo(a.StoredCount);
                if (amtCmp != 0) return amtCmp;
            }
            return a.ItemId.CompareTo(b.ItemId);
        }

        private static int CompareLocationEntries(PlanetItemLocationEntry a, PlanetItemLocationEntry b, ESortOrder sortOrder)
        {
            int cmp = CompareItems(a.Item, b.Item, sortOrder);
            if (cmp != 0) return cmp;
            return a.PlanetId.CompareTo(b.PlanetId);
        }

        private static PlanetItemLocationEntry MakeLocationEntry(PlanetData planet, StarData star, CombinedLogisticsItem item)
        {
            return new PlanetItemLocationEntry
            {
                Planet = planet,
                Star = star,
                PlanetId = planet.id,
                PlanetAstroId = planet.astroId,
                PlanetName = planet.displayName,
                StarName = star.displayName,
                Item = item
            };
        }

        private static void FilterSummaryDictionary(
            Dictionary<int, CombinedLogisticsItem> source,
            List<CombinedLogisticsItem> target,
            StellogisticsConfig config,
            int critPct,
            int warnPct,
            ref int filteredOutCount,
            ref int critCount,
            ref int warnCount)
        {
            foreach (CombinedLogisticsItem item in source.Values)
            {
                EHealthStatus status = item.GetHealthStatus(critPct, warnPct);
                if (!config.IsItemVisible(item.ItemId, item.Category) || config.IsItemExcludedByFill(item, status))
                {
                    filteredOutCount++;
                    continue;
                }
                target.Add(item);
                if (status == EHealthStatus.Critical) critCount++;
                else if (status == EHealthStatus.Warning) warnCount++;
            }
        }

        private static void FinalizeSummary(
            LogisticsSummary summary,
            Dictionary<int, CombinedLogisticsItem> exportsByItem,
            Dictionary<int, CombinedLogisticsItem> importsByItem,
            StellogisticsConfig config,
            int targetPlanetId,
            int targetStarId)
        {
            config.GetValidatedThresholds(out int critPct, out int warnPct);
            ESortOrder sortOrder = config.SortOrder.Value;

            FilterSummaryDictionary(
                exportsByItem,
                summary.Exports,
                config,
                critPct,
                warnPct,
                ref summary.FilteredOutItemCount,
                ref summary.CriticalExportCount,
                ref summary.WarningExportCount
            );

            FilterSummaryDictionary(
                importsByItem,
                summary.Imports,
                config,
                critPct,
                warnPct,
                ref summary.FilteredOutItemCount,
                ref summary.CriticalImportCount,
                ref summary.WarningImportCount
            );

            Dictionary<int, SupplyAvailability> supplyByItem = null;
            for (int i = 0; i < summary.Imports.Count; i++)
            {
                CombinedLogisticsItem item = summary.Imports[i];
                EHealthStatus status = item.GetHealthStatus(critPct, warnPct);
                if (status != EHealthStatus.Healthy && (item.FillRatio + item.InboundTransitRatio) < 0.85f)
                {
                    supplyByItem ??= GetOrCollectGalaxySupplyAvailability(config, summary.GalaxySeed, critPct, warnPct);
                    EvaluateImportBottleneck(item, status, supplyByItem, targetPlanetId, targetStarId);
                }
            }

            Comparison<CombinedLogisticsItem> comparer = (a, b) => CompareItems(a, b, sortOrder);
            summary.Exports.Sort(comparer);
            summary.Imports.Sort(comparer);
        }

        /// <summary>
        /// Populates <paramref name="result"/> with every item/building ID that has a slot, filter, or stored stack
        /// in any Logistics Station (ILS, PLS, Collector), Logistics Distributor, Storage Box, or Fluid Tank
        /// anywhere in the galaxy so unstored/unresearched items are not spoiled in the Custom Item Picker.
        /// Uses a 5-second cache unless <paramref name="forceRefresh"/> is true.
        /// </summary>
        public static void CollectStoredItemIds(HashSet<int> result, bool forceRefresh = false)
        {
            int seed = GameMain.galaxy != null ? GameMain.galaxy.seed : 0;
            float now = Time.unscaledTime;

            if (!forceRefresh && now < _storedIdsCacheExpiryTime && _storedIdsCachedSeed == seed)
            {
                result.Clear();
                result.UnionWith(_cachedStoredIds);
                return;
            }

            _cachedStoredIds.Clear();
            _storedIdsCachedSeed = seed;
            _storedIdsCacheExpiryTime = now + 5.0f;

            GameData data = GameMain.data;
            if (data?.factories != null)
            {
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
                                        _cachedStoredIds.Add(itemId);
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
                                    _cachedStoredIds.Add(dc.filter);
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
                                        _cachedStoredIds.Add(id);
                                    }
                                    int filterId = st.grids[g].filter;
                                    if (filterId > 0)
                                    {
                                        _cachedStoredIds.Add(filterId);
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
                                    _cachedStoredIds.Add(tk.fluidId);
                                }
                            }
                        }
                    }
                }
            }

            result.Clear();
            result.UnionWith(_cachedStoredIds);
        }

        public static bool HasSourceIngredients(int itemId)
        {
            List<int> sources = GetSourceIngredientIds(itemId);
            return sources != null && sources.Count > 0;
        }

        public static List<int> GetSourceIngredientIds(int itemId)
        {
            if (itemId <= 0 || LDB.items == null)
            {
                return null;
            }

            if (_cachedSourceIngredients.TryGetValue(itemId, out List<int> cached))
            {
                return cached;
            }

            ItemProto proto = LDB.items.Select(itemId);
            if (proto == null)
            {
                return null;
            }

            var result = new List<int>();
            var seen = new HashSet<int>();

            // 1. Primary recipe (maincraft) first
            if (proto.maincraft?.Items != null && Array.IndexOf(proto.maincraft.Items, itemId) < 0)
            {
                for (int i = 0; i < proto.maincraft.Items.Length; i++)
                {
                    int ingId = proto.maincraft.Items[i];
                    if (ingId > 0 && ingId != itemId && seen.Add(ingId))
                    {
                        result.Add(ingId);
                    }
                }
            }

            // 2. Any additional recipes in proto.recipes (e.g. alternate rare-resource recipes)
            if (proto.recipes != null)
            {
                for (int r = 0; r < proto.recipes.Count; r++)
                {
                    RecipeProto recipe = proto.recipes[r];
                    if (recipe?.Items == null || Array.IndexOf(recipe.Items, itemId) >= 0)
                    {
                        continue;
                    }
                    for (int i = 0; i < recipe.Items.Length; i++)
                    {
                        int ingId = recipe.Items[i];
                        if (ingId > 0 && ingId != itemId && seen.Add(ingId))
                        {
                            result.Add(ingId);
                        }
                    }
                }
            }

            // 3. Fallback: scan LDB.recipes if neither maincraft nor proto.recipes was populated
            if (result.Count == 0 && LDB.recipes?.dataArray != null)
            {
                RecipeProto[] allRecipes = LDB.recipes.dataArray;
                for (int r = 0; r < allRecipes.Length; r++)
                {
                    RecipeProto recipe = allRecipes[r];
                    if (recipe?.Results == null || recipe.Items == null)
                    {
                        continue;
                    }
                    if (Array.IndexOf(recipe.Results, itemId) < 0 || Array.IndexOf(recipe.Items, itemId) >= 0)
                    {
                        continue;
                    }
                    for (int i = 0; i < recipe.Items.Length; i++)
                    {
                        int ingId = recipe.Items[i];
                        if (ingId > 0 && ingId != itemId && seen.Add(ingId))
                        {
                            result.Add(ingId);
                        }
                    }
                }
            }

            _cachedSourceIngredients[itemId] = result;
            return result;
        }

        public static GalaxyItemSearchSummary ScanGalaxyFinder(StellogisticsConfig config, int focusItemId = 0)
        {
            int galaxySeed = GameMain.galaxy != null ? GameMain.galaxy.seed : 0;
            var storedIds = new HashSet<int>();
            CollectStoredItemIds(storedIds);

            int validStoredCount = 0;
            int checkedCount = 0;
            int singleItemId = -1;
            ItemProto singleProto = null;
            List<string> shortCheckedNames = null;

            if (focusItemId > 0)
            {
                singleProto = LDB.items?.Select(focusItemId);
                singleItemId = focusItemId;
                checkedCount = 1;
                validStoredCount = 1;
            }
            else
            {
                foreach (int id in storedIds)
                {
                    ItemProto proto = LDB.items?.Select(id);
                    if (proto == null || proto.ID <= 0 || proto.GridIndex <= 0)
                    {
                        continue;
                    }
                    validStoredCount++;
                    EItemCategoryFilter cat = CombinedLogisticsItem.ClassifyItem(proto);
                    if (config.IsItemVisible(id, cat))
                    {
                        checkedCount++;
                        if (checkedCount == 1)
                        {
                            singleItemId = id;
                            singleProto = proto;
                        }
                        else
                        {
                            singleItemId = -1;
                            singleProto = null;
                        }
                    }
                }

                // Also check LDB when a small set of specific items (e.g. recipe sources) is selected,
                // even if one of the ingredients isn't currently stored in a container yet.
                if (LDB.items?.dataArray != null)
                {
                    int ldbVisibleCount = 0;
                    int ldbSingleId = -1;
                    ItemProto ldbSingleProto = null;
                    var ldbNames = new List<string>(4);
                    ItemProto[] allProtos = LDB.items.dataArray;
                    for (int i = 0; i < allProtos.Length; i++)
                    {
                        ItemProto proto = allProtos[i];
                        if (proto == null || proto.ID <= 0 || proto.GridIndex <= 0)
                        {
                            continue;
                        }
                        EItemCategoryFilter cat = CombinedLogisticsItem.ClassifyItem(proto);
                        if (config.IsItemVisible(proto.ID, cat))
                        {
                            ldbVisibleCount++;
                            if (ldbVisibleCount == 1)
                            {
                                ldbSingleId = proto.ID;
                                ldbSingleProto = proto;
                            }
                            if (ldbVisibleCount <= 3 && !string.IsNullOrEmpty(proto.name))
                            {
                                ldbNames.Add(proto.name);
                            }
                        }
                    }

                    if (ldbVisibleCount > 0 && ldbVisibleCount <= 3)
                    {
                        shortCheckedNames = ldbNames;
                    }
                    if (ldbVisibleCount > 0 && ldbVisibleCount <= 6 && ldbVisibleCount > checkedCount)
                    {
                        checkedCount = ldbVisibleCount;
                        if (checkedCount == 1)
                        {
                            singleItemId = ldbSingleId;
                            singleProto = ldbSingleProto;
                        }
                        else
                        {
                            singleItemId = -1;
                            singleProto = null;
                        }
                    }
                }
            }

            bool includeCollectors = config.IncludeOrbitalCollectors.Value;
            config.GetValidatedThresholds(out int critPct, out int warnPct);
            bool onlyBottlenecks = config.ExcludeEnabled && config.ExcludeMode == EExcludeMode.Healthy;
            ESortOrder sortOrder = config.SortOrder.Value;

            string summaryTitle;
            if (checkedCount == 0)
            {
                summaryTitle = "No Items Checked";
            }
            else if (singleItemId > 0)
            {
                summaryTitle = singleProto != null ? singleProto.name : $"Item #{singleItemId}";
            }
            else if (shortCheckedNames != null && shortCheckedNames.Count > 1)
            {
                summaryTitle = string.Join(" + ", shortCheckedNames.ToArray());
            }
            else if (onlyBottlenecks)
            {
                summaryTitle = checkedCount < validStoredCount ? $"Bottlenecks ({checkedCount} Items)" : "Bottlenecks";
            }
            else
            {
                summaryTitle = checkedCount < validStoredCount ? $"{checkedCount} Items" : "All Items";
            }

            var summary = new GalaxyItemSearchSummary
            {
                IsBottleneckScan = (singleItemId <= 0),
                CheckedItemCount = checkedCount,
                ItemId = singleItemId,
                ItemName = summaryTitle,
                Icon = singleProto != null ? singleProto.iconSprite : null
            };

            if (checkedCount == 0 || GameMain.galaxy?.stars == null || GameMain.data == null)
            {
                return summary;
            }

            var exportsByItem = new Dictionary<int, CombinedLogisticsItem>();
            var importsByItem = new Dictionary<int, CombinedLogisticsItem>();
            var supplyByItem = new Dictionary<int, SupplyAvailability>();

            ForEachLogisticsPlanet((star, planet, _, __) =>
            {
                exportsByItem.Clear();
                importsByItem.Clear();
                StationTotals dummy = default;

                AggregatePlanetStations(
                    planet,
                    star.id,
                    includeCollectors,
                    EStarFilterMode.AllRemoteSlots,
                    null,
                    exportsByItem,
                    importsByItem,
                    null,
                    null,
                    ref dummy
                );

                LinkHubItems(exportsByItem, importsByItem);

                bool isIgnored = config.IsPlanetOrStarIgnored(galaxySeed, planet);
                bool hadMatchOnPlanet = false;

                foreach (CombinedLogisticsItem expItem in exportsByItem.Values)
                {
                    if (!isIgnored)
                    {
                        importsByItem.TryGetValue(expItem.ItemId, out CombinedLogisticsItem impOnSame);
                        RecordPlanetSupplyAvailability(supplyByItem, planet.id, star.id, expItem, impOnSame, critPct, warnPct);
                    }
                }

                CollectFinderMatchesForPlanet(
                    exportsByItem,
                    summary.ExportingPlanets,
                    planet,
                    star,
                    config,
                    focusItemId,
                    critPct,
                    warnPct,
                    isIgnored,
                    ref hadMatchOnPlanet,
                    ref summary.CriticalExportPlanetCount,
                    ref summary.WarningExportPlanetCount
                );

                CollectFinderMatchesForPlanet(
                    importsByItem,
                    summary.ImportingPlanets,
                    planet,
                    star,
                    config,
                    focusItemId,
                    critPct,
                    warnPct,
                    isIgnored,
                    ref hadMatchOnPlanet,
                    ref summary.CriticalImportPlanetCount,
                    ref summary.WarningImportPlanetCount
                );

                if (hadMatchOnPlanet)
                {
                    summary.IgnoredPlanetMatchCount++;
                }
            });

            // Share the fresh supply snapshot with the 1s cache
            _cachedSupplyAvailability.Clear();
            foreach (KeyValuePair<int, SupplyAvailability> kvp in supplyByItem)
            {
                _cachedSupplyAvailability[kvp.Key] = kvp.Value;
            }
            _supplyCachedSeed = galaxySeed;
            _supplyCachedIncludeOc = includeCollectors;
            _supplyCachedCritPct = critPct;
            _supplyCachedWarnPct = warnPct;
            _supplyCacheExpiryTime = Time.unscaledTime + 1.0f;

            for (int i = 0; i < summary.ImportingPlanets.Count; i++)
            {
                PlanetItemLocationEntry impEntry = summary.ImportingPlanets[i];
                EHealthStatus status = impEntry.Item.GetHealthStatus(critPct, warnPct);
                int starId = impEntry.Star != null ? impEntry.Star.id : (impEntry.PlanetId / 100);
                EvaluateImportBottleneck(impEntry.Item, status, supplyByItem, impEntry.PlanetId, starId);
            }

            Comparison<PlanetItemLocationEntry> comparer = (a, b) => CompareLocationEntries(a, b, sortOrder);
            summary.ExportingPlanets.Sort(comparer);
            summary.ImportingPlanets.Sort(comparer);

            return summary;
        }

        private static void CollectFinderMatchesForPlanet(
            Dictionary<int, CombinedLogisticsItem> source,
            List<PlanetItemLocationEntry> target,
            PlanetData planet,
            StarData star,
            StellogisticsConfig config,
            int focusItemId,
            int critPct,
            int warnPct,
            bool isIgnored,
            ref bool hadMatchOnPlanet,
            ref int critCount,
            ref int warnCount)
        {
            foreach (CombinedLogisticsItem item in source.Values)
            {
                bool matchesItem = focusItemId > 0
                    ? item.ItemId == focusItemId
                    : config.IsItemVisible(item.ItemId, item.Category);
                if (!matchesItem)
                {
                    continue;
                }

                EHealthStatus status = item.GetHealthStatus(critPct, warnPct);
                if (config.IsItemExcludedByFill(item, status))
                {
                    continue;
                }

                if (isIgnored)
                {
                    hadMatchOnPlanet = true;
                    continue;
                }

                target.Add(MakeLocationEntry(planet, star, item));
                if (status == EHealthStatus.Critical) critCount++;
                else if (status == EHealthStatus.Warning) warnCount++;
            }
        }

        public static void CollectLogisticsPlanetsAndStars(StellogisticsConfig config, List<PlanetLogisticsFilterEntry> outEntries)
        {
            outEntries.Clear();
            if (GameMain.galaxy?.stars == null)
            {
                return;
            }

            int galaxySeed = GameMain.galaxy.seed;
            bool includeCollectors = config.IncludeOrbitalCollectors.Value;
            var starPlanetEntries = new List<PlanetLogisticsFilterEntry>();

            StarData[] stars = GameMain.galaxy.stars;
            for (int s = 0; s < stars.Length; s++)
            {
                StarData star = stars[s];
                if (star?.planets == null)
                {
                    continue;
                }

                starPlanetEntries.Clear();
                int starIls = 0;
                int starCollectors = 0;

                for (int p = 0; p < star.planets.Length; p++)
                {
                    PlanetData planet = star.planets[p];
                    if (planet == null)
                    {
                        continue;
                    }

                    int ilsCount = 0;
                    int collectorCount = 0;
                    PlanetTransport transport = planet.factory?.transport;
                    StationComponent[] stationPool = transport?.stationPool;
                    if (stationPool != null && transport.stationCursor > 0)
                    {
                        int cursor = Math.Min(transport.stationCursor, stationPool.Length);
                        for (int i = 1; i < cursor; i++)
                        {
                            StationComponent station = stationPool[i];
                            if (station == null || station.id != i || !station.isStellar)
                            {
                                continue;
                            }
                            if (station.isCollector)
                            {
                                if (includeCollectors) collectorCount++;
                            }
                            else
                            {
                                ilsCount++;
                            }
                        }
                    }

                    bool isPlanetIgnored = config.IsAstroIgnored(galaxySeed, planet.astroId);
                    if (ilsCount > 0 || collectorCount > 0 || isPlanetIgnored)
                    {
                        starIls += ilsCount;
                        starCollectors += collectorCount;

                        string meta;
                        if (ilsCount > 0 && collectorCount > 0)
                            meta = $"{ilsCount} ILS +{collectorCount} OC";
                        else if (collectorCount > 0)
                            meta = $"{collectorCount} OC";
                        else if (ilsCount > 0)
                            meta = $"{ilsCount} ILS";
                        else
                            meta = "0 ILS (Ignored)";

                        starPlanetEntries.Add(new PlanetLogisticsFilterEntry
                        {
                            IsStarSystem = false,
                            AstroId = planet.astroId,
                            ParentStarAstroId = star.astroId,
                            Star = star,
                            Planet = planet,
                            DisplayName = planet.displayName,
                            MetaInfo = meta,
                            IlsCount = ilsCount,
                            CollectorCount = collectorCount
                        });
                    }
                }

                bool isStarIgnored = config.IsAstroIgnored(galaxySeed, star.astroId);
                if (starPlanetEntries.Count > 0 || isStarIgnored)
                {
                    string starMeta = starCollectors > 0
                        ? $"System • {starPlanetEntries.Count} Planets • {starIls} ILS +{starCollectors} OC"
                        : $"System • {starPlanetEntries.Count} Planets • {starIls} ILS";

                    outEntries.Add(new PlanetLogisticsFilterEntry
                    {
                        IsStarSystem = true,
                        AstroId = star.astroId,
                        ParentStarAstroId = star.astroId,
                        Star = star,
                        Planet = null,
                        DisplayName = $"★ {star.displayName}",
                        MetaInfo = starMeta,
                        IlsCount = starIls,
                        CollectorCount = starCollectors
                    });

                    for (int i = 0; i < starPlanetEntries.Count; i++)
                    {
                        outEntries.Add(starPlanetEntries[i]);
                    }
                }
            }
        }
    }
}
