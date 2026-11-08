using System;

namespace Stellogistics
{
    public enum EModLanguage
    {
        English = 0,
        Chinese = 1,
        French = 2,
        German = 3,
        Japanese = 4
    }

    public static class ModLocalization
    {
        public static EModLanguage CurrentLanguage => GetLanguage();

        public static bool IsCJK
        {
            get
            {
                EModLanguage lang = CurrentLanguage;
                return lang == EModLanguage.Chinese || lang == EModLanguage.Japanese;
            }
        }

        public static EModLanguage GetLanguage()
        {
            int lcid = Localization.CurrentLanguageLCID;
            return lcid switch
            {
                Localization.LCID_ZHCN => EModLanguage.Chinese,
                Localization.LCID_FRFR => EModLanguage.French,
                Localization.LCID_DEDE => EModLanguage.German,
                Localization.LCID_JAJA => EModLanguage.Japanese,
                _ => EModLanguage.English
            };
        }

        public static string Pick(string en, string zh, string fr, string de, string ja)
        {
            return CurrentLanguage switch
            {
                EModLanguage.Chinese => zh,
                EModLanguage.French => fr,
                EModLanguage.German => de,
                EModLanguage.Japanese => ja,
                _ => en
            };
        }

        // --- Header Buttons & Main Controls ---
        public static string BtnFinder => Pick("Finder", "查找", "Chercher", "Finder", "検索");
        public static string BtnBack => Pick("← Back", "← 返回", "← Retour", "← Zurück", "← 戻る");
        public static string BtnAll => Pick("← All", "← 全部", "← Tout", "← Alle", "← 全て");

        public static string FilterBtnLabel(bool open) =>
            open
                ? Pick("Filter ▴", "筛选 ▴", "Filtre ▴", "Filter ▴", "フィルタ ▴")
                : Pick("Filter ▾", "筛选 ▾", "Filtre ▾", "Filter ▾", "フィルタ ▾");

        public static string OptionsBtnLabel(bool open) =>
            open
                ? Pick("Options ▴", "选项 ▴", "Options ▴", "Optionen ▴", "設定 ▴")
                : Pick("Options ▾", "选项 ▾", "Options ▾", "Optionen ▾", "設定 ▾");

        public static string SortBtnLabel(ESortOrder order) => order switch
        {
            ESortOrder.ShortageFirst => Pick("Sort: Alert", "排序: 警报", "Tri: Alerte", "Sort: Alarm", "順: 警告"),
            ESortOrder.ItemId => Pick("Sort: ID", "排序: ID", "Tri: ID", "Sort: ID", "順: ID"),
            _ => Pick("Sort: Pct ↓", "排序: 比例↓", "Tri: % ↓", "Sort: % ↓", "順: 割合↓")
        };

        public static string StarFilterBtnLabel(EStarFilterMode mode) => mode switch
        {
            EStarFilterMode.InterstellarSmart => Pick("Ext Only", "仅星际", "Ext. seul.", "Nur Ext.", "星間のみ"),
            EStarFilterMode.NetOutIntraSystem => Pick("Netted", "净差值", "Net", "Netto", "純収支"),
            _ => Pick("All Slots", "全部槽位", "Tous empl.", "Alle Slots", "全スロット")
        };

        public static string StarFilterSubtitleMode(EStarFilterMode mode) => mode switch
        {
            EStarFilterMode.InterstellarSmart => Pick("Interstellar", "星际贸易", "Interstellaire", "Interstellar", "星間輸送"),
            EStarFilterMode.NetOutIntraSystem => Pick("Netted", "星系净值", "Compensation", "Saldiert", "星系内相殺"),
            _ => Pick("All Remote", "全部星际槽", "Tous distants", "Alle Remote", "全リモート")
        };

        // --- Subtitles & Header Info ---
        public static string UnknownPlanet => Pick("Unknown Planet", "未知行星", "Planète inconnue", "Unbekannter Planet", "未知の惑星");
        public static string UnknownSystem => Pick("Unknown System", "未知星系", "Système inconnu", "Unbekanntes System", "未知の星系");

        public static string StarSystemTitle(string starName) => CurrentLanguage switch
        {
            EModLanguage.Chinese => $"{starName} 星系",
            EModLanguage.French => $"Système {starName}",
            EModLanguage.German => $"{starName}-System",
            EModLanguage.Japanese => $"{starName} 星系",
            _ => $"{starName} System"
        };

        public static string PlanetWord => Pick("Planet", "行星", "Planète", "Planet", "惑星");
        public static string SystemWord => Pick("System", "星系", "Système", "System", "星系");
        public static string PlanetsCount(int count) => CurrentLanguage switch
        {
            EModLanguage.Chinese => $"{count} 颗行星",
            EModLanguage.French => $"{count} Planètes",
            EModLanguage.German => $"{count} Planeten",
            EModLanguage.Japanese => $"{count} 惑星",
            _ => $"{count} Planets"
        };

        public static string ColonizedPlanetsCount(int colonized, int total) => CurrentLanguage switch
        {
            EModLanguage.Chinese => $"{colonized}/{total} 颗行星",
            EModLanguage.French => $"{colonized}/{total} Planètes",
            EModLanguage.German => $"{colonized}/{total} Planeten",
            EModLanguage.Japanese => $"{colonized}/{total} 惑星",
            _ => $"{colonized}/{total} Planets"
        };

        public static string PausedSuffix =>
            Pick(
                " • <color=#FFD24C>⏸ Paused</color>",
                " • <color=#FFD24C>⏸ 已暂停</color>",
                " • <color=#FFD24C>⏸ En pause</color>",
                " • <color=#FFD24C>⏸ Pausiert</color>",
                " • <color=#FFD24C>⏸ 一時停止</color>"
            );

        public static string FleetInfoLine(int shownShips, int totalShips, bool showIdle, int warpers, int filteredOutCount)
        {
            string status = showIdle
                ? Pick("idle", "空闲", "inactifs", "inaktiv", "待機中")
                : Pick("on route", "航行中", "en route", "unterwegs", "航行中");
            string vesselsWord = Pick("Vessels", "运输船", "Vaisseaux", "Schiffe", "輸送船");
            string warpersWord = Pick("Warpers", "翘曲器", "Distorsions", "Warper", "ワーパー");
            string filterNote = filteredOutCount > 0
                ? CurrentLanguage switch
                {
                    EModLanguage.Chinese => $"  •  已隐藏 {filteredOutCount}",
                    EModLanguage.French => $"  •  {filteredOutCount} filtré(s)",
                    EModLanguage.German => $"  •  {filteredOutCount} gefiltert",
                    EModLanguage.Japanese => $"  •  {filteredOutCount} 非表示",
                    _ => $"  •  {filteredOutCount} filtered"
                }
                : "";
            return $"{vesselsWord}: {shownShips}/{totalShips} {status}  •  {warpersWord}: {CombinedLogisticsItem.FormatAmount(warpers)}{filterNote}";
        }

        public static string FinderHeaderTitle(string itemName) =>
            $"{Pick("Finder", "查找", "Chercher", "Finder", "検索")}: {itemName}";

        public static string FinderSubtitle(int expCount, int impCount, bool onlyBottlenecks, int checkedItemCount, int ignoredMatches, int totalIgnored, bool isPaused)
        {
            string ignoredSuffix = ignoredMatches > 0
                ? CurrentLanguage switch
                {
                    EModLanguage.Chinese => $" • {ignoredMatches} 已忽略",
                    EModLanguage.French => $" • {ignoredMatches} ignoré(s)",
                    EModLanguage.German => $" • {ignoredMatches} ignoriert",
                    EModLanguage.Japanese => $" • {ignoredMatches} 除外",
                    _ => $" • {ignoredMatches} Ignored"
                }
                : (totalIgnored > 0
                    ? CurrentLanguage switch
                    {
                        EModLanguage.Chinese => $" • {totalIgnored} 已筛选",
                        EModLanguage.French => $" • {totalIgnored} filtré(s)",
                        EModLanguage.German => $" • {totalIgnored} gefiltert",
                        EModLanguage.Japanese => $" • {totalIgnored} フィルタ済",
                        _ => $" • {totalIgnored} Filtered"
                    }
                    : "");

            string paused = isPaused ? PausedSuffix : "";
            if (onlyBottlenecks && checkedItemCount != 1)
            {
                return CurrentLanguage switch
                {
                    EModLanguage.Chinese => $"全星区扫描 • {expCount} 出口 / {impCount} 进口警报{ignoredSuffix}{paused}",
                    EModLanguage.French => $"Scan galactique • {expCount} Exp. / {impCount} Imp. alertes{ignoredSuffix}{paused}",
                    EModLanguage.German => $"Galaxie-Scan • {expCount} Export- / {impCount} Import-Alarme{ignoredSuffix}{paused}",
                    EModLanguage.Japanese => $"銀河スキャン • 輸出 {expCount} / 輸入 {impCount} 警告{ignoredSuffix}{paused}",
                    _ => $"Galaxy Scan • {expCount} Export / {impCount} Import Alerts{ignoredSuffix}{paused}"
                };
            }

            return CurrentLanguage switch
            {
                EModLanguage.Chinese => $"全星区扫描 • {expCount} 出口 • {impCount} 进口{ignoredSuffix}{paused}",
                EModLanguage.French => $"Scan galactique • {expCount} Export • {impCount} Import{ignoredSuffix}{paused}",
                EModLanguage.German => $"Galaxie-Scan • {expCount} Export • {impCount} Import{ignoredSuffix}{paused}",
                EModLanguage.Japanese => $"銀河スキャン • 輸出 {expCount} • 輸入 {impCount}{ignoredSuffix}{paused}",
                _ => $"Galaxy Scan • {expCount} Exporting • {impCount} Importing{ignoredSuffix}{paused}"
            };
        }

        public static string FinderHintLine =>
            Pick(
                "Click row: pin planet • Icon: sources • ✕: hide",
                "点击行: 定位行星 • 图标: 配方原料 • ✕: 隐藏",
                "Clic ligne: cibler • Icône: ingrédients • ✕: masquer",
                "Zeile: Planet anpinnen • Icon: Zutaten • ✕: ausblenden",
                "行クリック: 惑星へ • アイコン: 原料 • ✕: 非表示"
            );

        // --- Section Headers & Alerts ---
        public static string SectionExports => Pick("EXPORTS", "出口 (供应)", "EXPORTATIONS", "EXPORTE", "輸出 (供給)");
        public static string SectionImports => Pick("IMPORTS", "进口 (需求)", "IMPORTATIONS", "IMPORTE", "輸入 (需要)");
        public static string SectionExportingPlanets => Pick("EXPORTING PLANETS", "出口行星 (供应)", "PLANÈTES EXPORTATRICES", "EXPORTIERENDE PLANETEN", "輸出惑星 (供給)");
        public static string SectionImportingPlanets => Pick("IMPORTING PLANETS", "进口行星 (需求)", "PLANÈTES IMPORTATRICES", "IMPORTIERENDE PLANETEN", "輸入惑星 (需要)");

        public static string AlertEmpty => Pick("EMPTY", "空仓", "VIDE", "LEER", "枯渇");
        public static string AlertStarved => Pick("STARVED", "断供", "AFFAMÉ", "MANGEL", "不足");
        public static string AlertLow => Pick("LOW", "偏低", "BAS", "NIEDRIG", "低下");

        public static string RowStatusTag(EHealthStatus status, ELogisticStorage logic, long storedCount)
        {
            if (status == EHealthStatus.Critical)
            {
                if (storedCount == 0)
                {
                    return logic == ELogisticStorage.Supply
                        ? Pick("EMPTY ", "空仓 ", "VIDE ", "LEER ", "枯渇 ")
                        : Pick("STARVED ", "断供 ", "AFFAMÉ ", "MANGEL ", "不足 ");
                }
                return Pick("LOW ", "偏低 ", "BAS ", "NIEDRIG ", "低下 ");
            }
            if (status == EHealthStatus.Warning)
            {
                return Pick("LOW ", "偏低 ", "BAS ", "NIEDRIG ", "低下 ");
            }
            return "";
        }

        public static string HubBadge(ELogisticStorage logic, string counterMax)
        {
            string counterRole = logic == ELogisticStorage.Supply
                ? Pick("Imp", "进", "Imp", "Imp", "輸")
                : Pick("Exp", "出", "Exp", "Exp", "出");
            string hubWord = Pick("Hub", "中转", "Hub", "Hub", "ハブ");
            return $"<color=#5CE1FF>⇄ {hubWord} ({counterRole} {counterMax})</color>";
        }

        // --- Empty States & Finder Titles ---
        public static string EmptyNoActiveSlots =>
            Pick(
                "No active Interplanetary Logistics slots.",
                "无活动的星际物流槽位。",
                "Aucun emplacement logistique interplanétaire actif.",
                "Keine aktiven interplanetaren Logistik-Slots.",
                "有効な星間物流スロットがありません。"
            );

        public static string EmptyFilteredOut(int count) => CurrentLanguage switch
        {
            EModLanguage.Chinese => $"{count} 项物品已被筛选隐藏。",
            EModLanguage.French => $"{count} objet(s) masqué(s) par le filtre.",
            EModLanguage.German => $"{count} Item(s) durch Filter ausgeblendet.",
            EModLanguage.Japanese => $"{count} 個のアイテムがフィルタで非表示です。",
            _ => $"{count} item(s) hidden by Filter."
        };

        public static string EmptyNoMatchingRemoteSlots =>
            Pick(
                "No matching remote supply/demand slots.",
                "没有匹配的星际供应/需求槽位。",
                "Aucun emplacement d'offre/demande distante correspondant.",
                "Keine passenden Remote-Angebots-/Nachfrage-Slots.",
                "一致するリモート供給/需要スロットがありません。"
            );

        public static string EmptyNoIlsBuilt =>
            Pick(
                "No Interplanetary Logistics Stations built.",
                "未建造星际物流运输站。",
                "Aucune station logistique interplanétaire construite.",
                "Keine Interplanetaren Logistikstationen gebaut.",
                "星間物流ステーションが建設されていません。"
            );

        public static string EmptyNoItemsChecked =>
            Pick(
                "No items checked — click any item(s) in the Picker to locate.",
                "未勾选任何物品 — 请在选择器中点击物品进行定位。",
                "Aucun objet coché — cliquez sur des objets dans le sélecteur.",
                "Keine Items ausgewählt — im Picker anklicken zum Suchen.",
                "アイテム未選択 — ピッカーでアイテムを選択してください。"
            );

        public static string EmptyPlanetsIgnored(int count) => CurrentLanguage switch
        {
            EModLanguage.Chinese => $"{count} 颗匹配行星已被行星筛选隐藏（筛选 ▾ → 自定义选择器 → 行星）。",
            EModLanguage.French => $"{count} planète(s) masquée(s) par le filtre (Filtre ▾ → Sélecteur → Planètes).",
            EModLanguage.German => $"{count} passende(r) Planet(en) durch Planetenfilter ausgeblendet (Filter ▾ → Picker → Planeten).",
            EModLanguage.Japanese => $"{count} 個の一致惑星が惑星フィルタで非表示です（フィルタ ▾ → ピッカー → 惑星）。",
            _ => $"{count} matching planet(s) hidden by Planet Filter (Filter ▾ → Custom Picker → Planets)."
        };

        public static string EmptyAllHealthy(int warnPct) => CurrentLanguage switch
        {
            EModLanguage.Chinese => $"所有已勾选的星际槽位均状态良好 (>{warnPct}%)！",
            EModLanguage.French => $"Tous les emplacements cochés sont sains (>{warnPct}%) !",
            EModLanguage.German => $"Alle ausgewählten Remote-Slots sind im grünen Bereich (>{warnPct}%)!",
            EModLanguage.Japanese => $"選択された全リモートスロットが正常 (>{warnPct}%) です！",
            _ => $"All checked remote slots are Healthy (>{warnPct}%)!"
        };

        public static string EmptyNoPlanetTradesItem(string itemName) => CurrentLanguage switch
        {
            EModLanguage.Chinese => $"没有任何行星出口或进口 {itemName}。",
            EModLanguage.French => $"Aucune planète n'exporte ou n'importe {itemName}.",
            EModLanguage.German => $"Kein Planet exportiert oder importiert {itemName}.",
            EModLanguage.Japanese => $"{itemName} を輸出入している惑星はありません。",
            _ => $"No planet exports or imports {itemName}."
        };

        public static string FinderSummaryNoItemsChecked =>
            Pick("No Items Checked", "未选择物品", "Aucun objet coché", "Keine Items gewählt", "アイテム未選択");

        public static string FinderSummaryBottlenecksCount(int count) => CurrentLanguage switch
        {
            EModLanguage.Chinese => $"瓶颈 ({count} 项物品)",
            EModLanguage.French => $"Goulots ({count} objets)",
            EModLanguage.German => $"Engpässe ({count} Items)",
            EModLanguage.Japanese => $"ボトルネック ({count} 項目)",
            _ => $"Bottlenecks ({count} Items)"
        };

        public static string FinderSummaryBottlenecks =>
            Pick("Bottlenecks", "瓶颈检测", "Goulots d'étranglement", "Engpässe", "ボトルネック");

        public static string FinderSummaryItemsCount(int count) => CurrentLanguage switch
        {
            EModLanguage.Chinese => $"{count} 项物品",
            EModLanguage.French => $"{count} objets",
            EModLanguage.German => $"{count} Items",
            EModLanguage.Japanese => $"{count} アイテム",
            _ => $"{count} Items"
        };

        public static string FinderSummaryAllItems =>
            Pick("All Items", "全部物品", "Tous les objets", "Alle Items", "全アイテム");

        // --- Filter Dropdown Menu (OptionsMenu) ---
        public static string FilterMenuTitle =>
            Pick("ITEM FILTER", "物品筛选", "FILTRE D'OBJETS", "ITEM-FILTER", "アイテムフィルタ");

        public static string FilterTargetScope(bool hasCustomFilter) => hasCustomFilter
            ? Pick("Global Filter (Active across Starmap)", "全局筛选 (星图生效中)", "Filtre global (Actif sur la carte)", "Globaler Filter (Aktiv auf Sternkarte)", "全体フィルタ (星図で有効)")
            : Pick("Global Filter (All Shown)", "全局筛选 (显示全部)", "Filtre global (Tout affiché)", "Globaler Filter (Alle sichtbar)", "全体フィルタ (全て表示)");

        public static string CategoryLabel(EItemCategoryFilter cat) => cat switch
        {
            EItemCategoryFilter.RawResources => Pick("Raw Resources (Ores, Gas)", "原始资源 (矿石、气体)", "Ressources brutes (Minerais, Gaz)", "Rohstoffe (Erze, Gas)", "原資源 (鉱石・ガス)"),
            EItemCategoryFilter.Materials => Pick("Materials & Ingots", "基础材料与锭", "Matériaux & Lingots", "Materialien & Barren", "素材・インゴット"),
            EItemCategoryFilter.Components => Pick("Components & Parts", "组件与零件", "Composants & Pièces", "Komponenten & Teile", "部品・コンポーネント"),
            EItemCategoryFilter.Buildings => Pick("Buildings & Structures", "建筑与设施", "Bâtiments & Structures", "Gebäude & Strukturen", "建物・施設"),
            EItemCategoryFilter.ConsumablesAndPods => Pick("Fuel, Warpers & Vessels", "燃料、翘曲器与运输船", "Carburant, Distorsions & Vaisseaux", "Treibstoff, Warper & Schiffe", "燃料・ワーパー・輸送船"),
            EItemCategoryFilter.Matrices => Pick("Science Matrices", "研究矩阵 (糖块)", "Matrices scientifiques", "Forschungsmatrizen", "マトリックス"),
            _ => cat.ToString()
        };

        public static string ExcludeRowLabel(EExcludeMode mode, int warnPct) => mode switch
        {
            EExcludeMode.Full100 => Pick("Exclude 100% Entries", "排除 100% 满仓项", "Exclure entrées à 100%", "100%-Einträge ausblenden", "100% 満杯を除外"),
            EExcludeMode.Tapped90 => Pick("Exclude ≥90% (Tapped)", "排除 ≥90% (满仓/在用)", "Exclure ≥90% (Plein)", "≥90% ausblenden (Voll)", "≥90% を除外 (満杯)"),
            _ => CurrentLanguage switch
            {
                EModLanguage.Chinese => $"排除 >{warnPct}% (健康)",
                EModLanguage.French => $"Exclure >{warnPct}% (Sain)",
                EModLanguage.German => $">{warnPct}% ausblenden (Gesund)",
                EModLanguage.Japanese => $">{warnPct}% を除外 (正常)",
                _ => $"Exclude >{warnPct}% (Healthy)"
            }
        };

        public static string IgnoreTargetInFinderLabel(bool isStarSystem, int totalIgnored)
        {
            string countSuffix = totalIgnored > 0
                ? CurrentLanguage switch
                {
                    EModLanguage.Chinese => $" (已忽略 {totalIgnored})",
                    EModLanguage.French => $" ({totalIgnored} ignoré(s))",
                    EModLanguage.German => $" ({totalIgnored} ignoriert)",
                    EModLanguage.Japanese => $" ({totalIgnored} 除外中)",
                    _ => $" ({totalIgnored} ignored)"
                }
                : "";

            return CurrentLanguage switch
            {
                EModLanguage.Chinese => $"在查找中忽略此{(isStarSystem ? "星系" : "行星")}{countSuffix}",
                EModLanguage.French => $"Ignorer {(isStarSystem ? "ce système" : "cette planète")}{countSuffix}",
                EModLanguage.German => $"{(isStarSystem ? "Sternsystem" : "Planet")} im Finder ignorieren{countSuffix}",
                EModLanguage.Japanese => $"検索でこの{(isStarSystem ? "星系" : "惑星")}を除外{countSuffix}",
                _ => $"Ignore {(isStarSystem ? "Star System" : "Planet")} in Finder{countSuffix}"
            };
        }

        public static string BtnResetFilters =>
            Pick("Reset Filters", "重置筛选", "Réinitialiser", "Filter zurücks.", "フィルタ初期化");

        public static string BtnCustomPicker =>
            Pick("Custom Picker", "自定义选择器", "Sélecteur perso.", "Item-Picker", "カスタム選択");

        // --- General Options Dropdown Menu ---
        public static string GeneralOptionsMenuTitle =>
            Pick("OPTIONS — DISPLAY & UI", "选项 — 显示与界面", "OPTIONS — AFFICHAGE & UI", "OPTIONEN — ANZEIGE & UI", "設定 — 表示とUI");

        public static string OptShowInTransitBar =>
            Pick("Show +In-Transit Bar (Hatched)", "显示在途货物条 (斜纹)", "Barre en transit (Hachurée)", "+In-Transit-Balken anzeigen", "輸送中バーを表示 (斜線)");

        public static string OptIncludeOrbitalCollectors =>
            Pick("Include Orbital Collectors (OC)", "包含轨道采集器 (OC)", "Inclure collecteurs orbitaux (OC)", "Orbitalkollektoren (OC) einbeziehen", "軌道コレクター (OC) を含める");

        public static string OptShowOnHover =>
            Pick("Switch to Planet / System on Hover", "悬停时切换行星 / 星系", "Changer de cible au survol", "Bei Hover Planet / System wechseln", "ホバーで惑星 / 星系を切替");

        public static string RefreshRateHeader =>
            Pick("REFRESH RATE", "刷新频率", "FRÉQUENCE D'ACTUALISATION", "AKTUALISIERUNGSRATE", "更新頻度");

        public static string RefreshRateLabel(ERefreshRate rate) => rate switch
        {
            ERefreshRate.Fast => Pick("Live", "实时", "Direct", "Live", "リアルタイム"),
            ERefreshRate.Medium => Pick("Medium", "中速", "Moyen", "Mittel", "標準"),
            ERefreshRate.Slow => Pick("Slow", "慢速", "Lent", "Langsam", "低速"),
            _ => Pick("Paused", "暂停", "Pause", "Pause", "一時停止")
        };

        public static string BtnResetUiPosAndSize =>
            Pick("Reset UI Position & Size", "重置界面位置与大小", "Réinit. position et taille UI", "UI-Position & Größe zurücksetzen", "UIの位置とサイズをリセット");

        // --- Custom Item, Building & Planet Picker ---
        public static string PickerWindowTitle =>
            Pick(
                "ITEM, BUILDING & PLANET PICKER (Click to toggle)",
                "物品、建筑与行星选择器 (点击切换显示/隐藏)",
                "SÉLECTEUR D'OBJETS, BÂTIMENTS & PLANÈTES (Cliquer pour basculer)",
                "ITEM-, GEBÄUDE- & PLANETEN-PICKER (Klicken zum Umschalten)",
                "アイテム・建物・惑星ピッカー (クリックで表示/非表示)"
            );

        public static string TabItems => Pick("Items", "物品", "Objets", "Items", "アイテム");
        public static string TabBuildings => Pick("Buildings", "建筑", "Bâtiments", "Gebäude", "建物");
        public static string TabPlanets(int ignoredCount)
        {
            string baseWord = Pick("Planets", "行星", "Planètes", "Planeten", "惑星");
            return ignoredCount > 0 ? $"{baseWord} (-{ignoredCount})" : baseWord;
        }

        public static string BtnCheckAll => Pick("✓ Check All", "✓ 全选", "✓ Tout cocher", "✓ Alle an", "✓ 全て選択");
        public static string BtnUncheckAll => Pick("✕ Uncheck All", "✕ 全不选", "✕ Tout décocher", "✕ Alle aus", "✕ 全て解除");
        public static string BtnShowAllPlanets => Pick("✓ Show All Planets", "✓ 显示全部行星", "✓ Afficher tout", "✓ Alle Planeten", "✓ 全惑星を表示");

        public static string PickerHoverDefaultItems =>
            Pick(
                "Bright = Shown • Dimmed = Hidden",
                "高亮 = 显示 • 暗淡 = 隐藏",
                "Brillant = Affiché • Assombri = Masqué",
                "Hell = Sichtbar • Gedimmt = Ausgeblendet",
                "明るい = 表示 • 暗い = 非表示"
            );

        public static string PickerHoverDefaultPlanets =>
            Pick(
                "Toggle Star / Planet in Finder",
                "点击切换星系 / 行星在查找中的启用状态",
                "Activer/désactiver système ou planète",
                "Sternsystem / Planet im Finder umschalten",
                "検索での星系 / 惑星の有効・無効を切替"
            );

        public static string PickerHoveredItemStatus(string itemName, bool shown) => shown
            ? $"{itemName} ({Pick("Shown", "显示", "Affiché", "Sichtbar", "表示")})"
            : $"{itemName} ({Pick("Hidden", "隐藏", "Masqué", "Ausgeblendet", "非表示")})";

        public static string PickerPlanetsEmpty =>
            Pick(
                "No planets with Interplanetary Logistics Stations found.",
                "未发现建有星际物流运输站的行星。",
                "Aucune planète avec station logistique interplanétaire.",
                "Keine Planeten mit Interplanetaren Logistikstationen gefunden.",
                "星間物流ステーションがある惑星が見つかりません。"
            );

        public static string IgnoredWord => Pick("Ignored", "已忽略", "Ignoré", "Ignoriert", "除外中");
        public static string ZeroIlsIgnored => $"0 ILS ({IgnoredWord})";
    }
}
