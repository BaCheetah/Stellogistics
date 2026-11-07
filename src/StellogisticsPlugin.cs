using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace Stellogistics
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public class StellogisticsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.stellogistics.dsp";
        public const string PluginName = "Stellogistics";
        public const string PluginVersion = "2.1.1";

        public static ManualLogSource Log { get; private set; }
        public static StellogisticsConfig ModConfig { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            ModConfig = new StellogisticsConfig(Config);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(StarmapPatches));

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            StarmapPatches.DestroyPanel();
            _harmony?.UnpatchSelf();
        }
    }
}
