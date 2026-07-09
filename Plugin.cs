using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace AskaGrassRestore
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BasePlugin
    {
        public const string PluginGuid = "smithio.aska.plantgrass";
        public const string PluginName = "Aska Grass Restore";
        public const string PluginVersion = "0.1.0";

        internal static new ManualLogSource Log;

        internal static ConfigEntry<bool> ConfigEnabled;
        internal static ConfigEntry<KeyCode> ConfigKey;
        internal static ConfigEntry<float> ConfigRadius;

        public override void Load()
        {
            Log = base.Log;

            ConfigEnabled = Config.Bind("GrassRestore", "Enabled", true,
                "Master on/off switch for the grass-restore tool.");
            ConfigKey = Config.Bind("GrassRestore", "Key", KeyCode.RightBracket,
                "Key that paints grass at your feet.");
            ConfigRadius = Config.Bind("GrassRestore", "Radius", 1f,
                "Radius (meters) of the paint effect.");

            ClassInjector.RegisterTypeInIl2Cpp<GrassRestoreTool>();
            Harmony.CreateAndPatchAll(typeof(CharacterSpawnPatch));

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }
    }
}
