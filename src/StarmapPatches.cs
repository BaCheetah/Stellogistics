using HarmonyLib;
using UnityEngine;

namespace Stellogistics
{
    public static class StarmapPatches
    {
        private static UIStarmapLogisticsPanel _panelInstance;

        public static bool IsPointerOverPanel()
        {
            return _panelInstance != null && _panelInstance.IsPointerOverUI();
        }

        public static UIStarmapLogisticsPanel EnsurePanel(UIStarmap starmap)
        {
            if (_panelInstance != null)
            {
                return _panelInstance;
            }

            UIGame uiGame = UIRoot.instance?.uiGame;
            if (uiGame == null || starmap == null)
            {
                return null;
            }

            Transform parentTransform = starmap.transform;

            GameObject panelGo = new GameObject("Stellogistics_StarmapPanel", typeof(RectTransform));
            panelGo.transform.SetParent(parentTransform, false);
            panelGo.SetActive(false);

            _panelInstance = panelGo.AddComponent<UIStarmapLogisticsPanel>();
            _panelInstance.Initialize(uiGame, StellogisticsPlugin.ModConfig);

            StellogisticsPlugin.Log?.LogInfo("Initialized Stellogistics Starmap sidebar panel.");
            return _panelInstance;
        }

        public static void DestroyPanel()
        {
            if (_panelInstance != null)
            {
                if (_panelInstance.gameObject != null)
                {
                    Object.Destroy(_panelInstance.gameObject);
                }
                _panelInstance = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap._OnOpen))]
        public static void UIStarmap_OnOpen_Postfix(UIStarmap __instance)
        {
            UIStarmapLogisticsPanel panel = EnsurePanel(__instance);
            if (panel != null)
            {
                panel.OnStarmapOpened();
                panel.OnStarmapUpdate(__instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap._OnUpdate))]
        public static void UIStarmap_OnUpdate_Postfix(UIStarmap __instance)
        {
            UIStarmapLogisticsPanel panel = EnsurePanel(__instance);
            if (panel != null)
            {
                panel.OnStarmapUpdate(__instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap._OnClose))]
        public static void UIStarmap_OnClose_Postfix()
        {
            if (_panelInstance != null && _panelInstance.gameObject.activeSelf)
            {
                _panelInstance.gameObject.SetActive(false);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap._OnDestroy))]
        public static void UIStarmap_OnDestroy_Postfix()
        {
            DestroyPanel();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIPlanetDetail), nameof(UIPlanetDetail.OnPlanetDataSet))]
        public static void UIPlanetDetail_OnPlanetDataSet_Postfix()
        {
            if (_panelInstance != null)
            {
                _panelInstance.RequestImmediateRefresh();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarDetail), nameof(UIStarDetail.OnStarDataSet))]
        public static void UIStarDetail_OnStarDataSet_Postfix()
        {
            if (_panelInstance != null)
            {
                _panelInstance.RequestImmediateRefresh();
            }
        }

        // Prevent Starmap camera zooming while the mouse pointer is inside the Stellogistics UI
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFInput), nameof(VFInput.UpdateGameStates))]
        public static void VFInput_UpdateGameStates_Postfix()
        {
            if (IsPointerOverPanel())
            {
                VFInput.inScrollView = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFInput), nameof(VFInput._cameraZoomIn), MethodType.Getter)]
        public static void VFInput_CameraZoomIn_Postfix(ref float __result)
        {
            if (IsPointerOverPanel())
            {
                __result = 0f;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFInput), nameof(VFInput._cameraZoomOut), MethodType.Getter)]
        public static void VFInput_CameraZoomOut_Postfix(ref float __result)
        {
            if (IsPointerOverPanel())
            {
                __result = 0f;
            }
        }
    }
}
