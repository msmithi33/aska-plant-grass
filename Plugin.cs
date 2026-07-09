using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace AskaGrassRestore
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BasePlugin
    {
        public const string PluginGuid = "smithio.aska.plantgrass";
        public const string PluginName = "Aska Grass Restore";
        public const string PluginVersion = "0.1.0";

        internal static new ManualLogSource Log;

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }
    }
}
