using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stellogistics
{
    // Calm row ordering for Live refresh:
    //  1. Hysteresis: rows only swap when their fill percent moved by >= ReorderHysteresisPct
    //     since the last time they were placed, or when their health status changed.
    //  2. Cadence: the order is recomputed at most every ReorderMinInterval seconds
    //     (status changes bypass this) and is frozen while the pointer is over the panel.
    //  3. Identity: rows are pooled per item key and glide to their new slot (SmoothDamp),
    //     so the eye can follow where an item moved.
    public partial class UIStarmapLogisticsPanel
    {
        private const int ReorderHysteresisPct = 5;
        private const float ReorderMinInterval = 2.0f;
        private const float RowGlideSmoothTime = 0.12f;

        private class StickyOrderState
        {
            public int AnchorPct;
            public EHealthStatus AnchorStatus;
            public int CurrentPct;
            public EHealthStatus CurrentStatus;
            public int Order = -1;
            public int SeenStamp;
        }

        private readonly Dictionary<long, StickyOrderState>[] _stickyOrder =
        {
            new Dictionary<long, StickyOrderState>(),
            new Dictionary<long, StickyOrderState>()
        };
        private readonly Dictionary<long, ItemRowView>[] _rowsByKey =
        {
            new Dictionary<long, ItemRowView>(),
            new Dictionary<long, ItemRowView>()
        };
        private readonly List<StickyOrderState> _stateScratch = new List<StickyOrderState>();
        private readonly List<int> _orderScratch = new List<int>();
        private readonly List<long> _staleKeyScratch = new List<long>();

        private int _orderStamp;
        private int _layoutStamp;
        private float _nextReorderTime;
        private long _orderContextKey = long.MinValue;
        private bool _snapRowsThisLayout = true;

        private static long RowKeyOf(CombinedLogisticsItem item) => item.ItemId;

        private static long RowKeyOf(PlanetItemLocationEntry entry) =>
            ((long)entry.PlanetId << 32) | (uint)entry.Item.ItemId;

        private long ComputeOrderContextKey()
        {
            unchecked
            {
                long k = _finderModeActive ? 1 : 0;
                k = k * 486187739L + (_finderModeActive ? _finderFocusItemId : _lastTargetAstroId);
                k = k * 486187739L + (int)_config.SortOrder.Value;
                k = k * 486187739L + (int)_config.StarSystemFilterMode.Value;
                return k;
            }
        }

        /// <summary>
        /// Decides for this layout pass whether rows may be re-sorted (timer due) or must stay put (pointer hover).
        /// A context change (other target, sort mode, Finder) resets all sticky state and snaps rows without animation.
        /// </summary>
        private void BeginOrderingPass(out bool reorderDue, out bool frozen)
        {
            long ctx = ComputeOrderContextKey();
            bool contextChanged = ctx != _orderContextKey;
            if (contextChanged)
            {
                _orderContextKey = ctx;
                _stickyOrder[0].Clear();
                _stickyOrder[1].Clear();
                _snapRowsThisLayout = true;
            }

            frozen = !contextChanged && _panelHoverTracker != null && _panelHoverTracker.IsHovered;
            reorderDue = contextChanged || (!frozen && Time.unscaledTime >= _nextReorderTime);
        }

        /// <summary>
        /// Produces a calm display order from the aggregator's strictly sorted list. Returns true if a re-sort happened.
        /// </summary>
        private bool StabilizeOrder<T>(
            IList<T> sorted,
            int section,
            Func<T, long> keyOf,
            Func<T, CombinedLogisticsItem> itemOf,
            bool reorderDue,
            bool frozen,
            int critPct,
            int warnPct,
            List<T> output)
        {
            output.Clear();
            Dictionary<long, StickyOrderState> states = _stickyOrder[section];
            int stamp = ++_orderStamp;
            ESortOrder sortOrder = _config.SortOrder.Value;
            bool sticky = sortOrder != ESortOrder.ItemId;
            bool statusChanged = false;

            _stateScratch.Clear();
            for (int i = 0; i < sorted.Count; i++)
            {
                T entry = sorted[i];
                CombinedLogisticsItem item = itemOf(entry);
                long key = keyOf(entry);
                int pct = item.DisplayPercent;
                EHealthStatus status = item.GetHealthStatus(critPct, warnPct);

                if (!states.TryGetValue(key, out StickyOrderState st))
                {
                    st = new StickyOrderState { AnchorPct = pct, AnchorStatus = status };
                    states[key] = st;
                }
                else if (st.AnchorStatus != status)
                {
                    statusChanged = true;
                }
                st.CurrentPct = pct;
                st.CurrentStatus = status;
                st.SeenStamp = stamp;
                _stateScratch.Add(st);
            }

            if (states.Count > _stateScratch.Count)
            {
                _staleKeyScratch.Clear();
                foreach (KeyValuePair<long, StickyOrderState> kv in states)
                {
                    if (kv.Value.SeenStamp != stamp) _staleKeyScratch.Add(kv.Key);
                }
                for (int i = 0; i < _staleKeyScratch.Count; i++) states.Remove(_staleKeyScratch[i]);
            }

            if (!sticky)
            {
                for (int i = 0; i < sorted.Count; i++)
                {
                    _stateScratch[i].Order = i;
                    output.Add(sorted[i]);
                }
                return true;
            }

            bool reorder = !frozen && (reorderDue || statusChanged);
            if (reorder)
            {
                for (int i = 0; i < _stateScratch.Count; i++)
                {
                    StickyOrderState st = _stateScratch[i];
                    if (st.CurrentStatus != st.AnchorStatus ||
                        Mathf.Abs(st.CurrentPct - st.AnchorPct) >= ReorderHysteresisPct)
                    {
                        st.AnchorPct = st.CurrentPct;
                        st.AnchorStatus = st.CurrentStatus;
                    }
                }
            }

            _orderScratch.Clear();
            for (int i = 0; i < sorted.Count; i++) _orderScratch.Add(i);

            bool desc = sortOrder == ESortOrder.StoredAmountDesc;
            List<StickyOrderState> stateList = _stateScratch;
            _orderScratch.Sort((ia, ib) =>
            {
                StickyOrderState a = stateList[ia];
                StickyOrderState b = stateList[ib];
                if (reorder && a.AnchorPct != b.AnchorPct)
                {
                    return desc ? b.AnchorPct.CompareTo(a.AnchorPct) : a.AnchorPct.CompareTo(b.AnchorPct);
                }
                // Keep previous on-screen order; newly appearing rows go after existing ones (by aggregator rank).
                int oa = a.Order >= 0 ? a.Order : int.MaxValue;
                int ob = b.Order >= 0 ? b.Order : int.MaxValue;
                if (oa != ob) return oa.CompareTo(ob);
                return ia.CompareTo(ib);
            });

            for (int pos = 0; pos < _orderScratch.Count; pos++)
            {
                int src = _orderScratch[pos];
                _stateScratch[src].Order = pos;
                output.Add(sorted[src]);
            }
            return reorder;
        }

        private ItemRowView AcquireRow(int section, long key, out bool fresh)
        {
            Dictionary<long, ItemRowView> map = _rowsByKey[section];
            if (map.TryGetValue(key, out ItemRowView row))
            {
                row.UsedStamp = _layoutStamp;
                fresh = false;
                if (!row.RootGo.activeSelf)
                {
                    row.RootGo.SetActive(true);
                    fresh = true;
                }
                return row;
            }

            row = null;
            for (int i = 0; i < _rowPool.Count; i++)
            {
                if (!_rowPool[i].InUse)
                {
                    row = _rowPool[i];
                    break;
                }
            }
            if (row == null)
            {
                row = CreateRowWidget(_rowPool.Count);
                _rowPool.Add(row);
            }

            row.InUse = true;
            row.AnimSection = section;
            row.AnimKey = key;
            row.UsedStamp = _layoutStamp;
            map[key] = row;

            row.MuteSlideProgress = 0f;
            if (!row.RootGo.activeSelf)
            {
                row.RootGo.SetActive(true);
            }
            fresh = true;
            return row;
        }

        private void PlaceRow(ItemRowView row, float sectionBaseY, int displayIndex, int globalIndex, bool fresh)
        {
            row.SectionBaseY = sectionBaseY;
            row.TargetOffset = displayIndex * (RowHeight + RowSpacing);
            if (fresh || _snapRowsThisLayout)
            {
                row.CurrentOffset = row.TargetOffset;
                row.OffsetVelocity = 0f;
            }
            row.RootRect.anchoredPosition = new Vector2(0f, -(row.SectionBaseY + row.CurrentOffset));
            row.BgImage.color = (globalIndex % 2 == 0) ? RowEvenBgColor : RowOddBgColor;
        }

        private void ReleaseRow(ItemRowView row)
        {
            if (row.InUse)
            {
                _rowsByKey[row.AnimSection].Remove(row.AnimKey);
                row.InUse = false;
            }
            if (row.RootGo.activeSelf)
            {
                row.RootGo.SetActive(false);
            }
        }

        private void ReleaseUnusedRows()
        {
            for (int i = 0; i < _rowPool.Count; i++)
            {
                ItemRowView row = _rowPool[i];
                if (!row.InUse || row.UsedStamp != _layoutStamp)
                {
                    ReleaseRow(row);
                }
            }
        }

        private void ReleaseAllRows()
        {
            for (int i = 0; i < _rowPool.Count; i++)
            {
                ReleaseRow(_rowPool[i]);
            }
            _snapRowsThisLayout = true;
        }

        private void UpdateRowPositionAnimations()
        {
            if (_panelCollapsed || _rowPool.Count == 0)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
            {
                return;
            }

            for (int i = 0; i < _rowPool.Count; i++)
            {
                ItemRowView row = _rowPool[i];
                if (!row.InUse || row.CurrentOffset == row.TargetOffset)
                {
                    continue;
                }

                if (Mathf.Abs(row.TargetOffset - row.CurrentOffset) < 0.25f && Mathf.Abs(row.OffsetVelocity) < 2f)
                {
                    row.CurrentOffset = row.TargetOffset;
                    row.OffsetVelocity = 0f;
                }
                else
                {
                    row.CurrentOffset = Mathf.SmoothDamp(
                        row.CurrentOffset, row.TargetOffset, ref row.OffsetVelocity,
                        RowGlideSmoothTime, Mathf.Infinity, dt);
                }
                row.RootRect.anchoredPosition = new Vector2(0f, -(row.SectionBaseY + row.CurrentOffset));
            }
        }
    }
}
