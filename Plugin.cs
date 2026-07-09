using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace AskaPlantGrass
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BasePlugin
    {
        public const string PluginGuid = "smithio.aska.plantgrass";
        public const string PluginName = "Aska Plant Grass";
        public const string PluginVersion = "0.1.0";

        internal static new ManualLogSource Log;

        internal static ConfigEntry<bool> ConfigEnabled;
        internal static ConfigEntry<KeyCode> ConfigKey;

        public override void Load()
        {
            Log = base.Log;

            ConfigEnabled = Config.Bind("PlantGrass", "Enabled", true,
                "Master on/off switch for the grass-restore tool.");
            ConfigKey = Config.Bind("PlantGrass", "Key", KeyCode.RightBracket,
                "Key that paints grass at your feet.");

            ClassInjector.RegisterTypeInIl2Cpp<PlantGrassTool>();
            Harmony.CreateAndPatchAll(typeof(CharacterSpawnPatch));

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }
    }
}
