using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stellogistics
{
    public enum EHealthStatus
    {
        Critical = 0, // Running empty (Export) or Starved (Import) -> Red
        Warning = 1,  // Low stock / depleting -> Orange / Amber
        Healthy = 2   // Well stocked -> Green / Cyan
    }

    public class CombinedLogisticsItem
    {
        public int ItemId;
        public string ItemName;
        public Sprite Icon;
        public EItemCategoryFilter Category;
        public ELogisticStorage Logic; // Supply = Export, Demand = Import
        public long StoredCount;
        public long MaxCount;
        public long InTransitOrder; // remoteOrder: negative when outbound from Supply, positive when inbound to Demand
        public int SlotCount;
        public int CollectorSlotCount;
        public int PlanetCount;
        public bool HasExternalStarPair;
        public bool HasWarpCapableStation;

        public float FillRatio
        {
            get
            {
                if (MaxCount <= 0)
                {
                    return StoredCount > 0 ? 1f : 0f;
                }
                return Mathf.Clamp01((float)StoredCount / MaxCount);
            }
        }

        public int DisplayPercent
        {
            get
            {
                if (MaxCount <= 0)
                {
                    return StoredCount > 0 ? 100 : 0;
                }
                if (StoredCount <= 0)
                {
                    return 0;
                }
                if (StoredCount >= MaxCount)
                {
                    return 100;
                }
                return Mathf.Clamp(Mathf.RoundToInt((float)StoredCount / MaxCount * 100f), 1, 99);
            }
        }

        public EHealthStatus GetHealthStatus(int criticalThresholdPct, int warningThresholdPct)
        {
            if (MaxCount <= 0)
            {
                return StoredCount > 0 ? EHealthStatus.Healthy : EHealthStatus.Critical;
            }

            int pct = DisplayPercent;
            if (StoredCount <= 0 || pct <= criticalThresholdPct)
            {
                return EHealthStatus.Critical;
            }
            if (pct <= warningThresholdPct)
            {
                return EHealthStatus.Warning;
            }
            return EHealthStatus.Healthy;
        }

        public static EItemCategoryFilter ClassifyItem(ItemProto proto)
        {
            if (proto == null)
            {
                return EItemCategoryFilter.Materials;
            }

            if (proto.Type == EItemType.Matrix)
            {
                return EItemCategoryFilter.Matrices;
            }

            if (proto.CanBuild ||
                proto.Type == EItemType.Logistics ||
                proto.Type == EItemType.Production ||
                proto.Type == EItemType.Decoration ||
                proto.Type == EItemType.Turret ||
                proto.Type == EItemType.Defense)
            {
                return EItemCategoryFilter.Buildings;
            }

            return proto.Type switch
            {
                EItemType.Resource => EItemCategoryFilter.RawResources,
                EItemType.Material => EItemCategoryFilter.Materials,
                EItemType.DarkFog => EItemCategoryFilter.Materials,
                EItemType.Component => EItemCategoryFilter.Components,
                EItemType.Product => EItemCategoryFilter.ConsumablesAndPods,
                _ => EItemCategoryFilter.Materials
            };
        }

        public static string FormatAmount(long amount)
        {
            long abs = Math.Abs(amount);
            string sign = amount < 0 ? "-" : "";
            if (abs >= 1_000_000)
            {
                return $"{sign}{abs / 1_000_000.0:0.#}M";
            }
            if (abs >= 10_000)
            {
                return $"{sign}{abs / 1_000.0:0.#}k";
            }
            if (abs >= 1_000)
            {
                return $"{sign}{abs / 1_000.0:0.0}k";
            }
            return $"{sign}{abs}";
        }
    }

    public class LogisticsSummary
    {
        public string TargetName = "";
        public string TargetSubtitle = "";
        public bool IsStarSystem;
        public int TargetAstroId;
        public int GalaxySeed;
        public EItemCategoryFilter ActiveCategoryFilter = EItemCategoryFilter.All;
        public int FilteredOutItemCount;

        public int TotalIlsCount;
        public int TotalCollectorCount;
        public int TotalWarperCount;
        public int TotalIdleShips;
        public int TotalWorkShips;
        public int ColonizedPlanetCount;

        public List<CombinedLogisticsItem> Exports = new List<CombinedLogisticsItem>();
        public List<CombinedLogisticsItem> Imports = new List<CombinedLogisticsItem>();

        public int CriticalExportCount;
        public int WarningExportCount;
        public int CriticalImportCount;
        public int WarningImportCount;

        public bool HasAnyStations => TotalIlsCount > 0 || TotalCollectorCount > 0;
        public bool HasAnySlots => Exports.Count > 0 || Imports.Count > 0;
    }
}
