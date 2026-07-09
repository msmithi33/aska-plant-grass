using HarmonyLib;
using SSSGame;
using UnityEngine;

namespace AskaGrassRestore
{
    [HarmonyPatch(typeof(Character))]
    public static class CharacterSpawnPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Character.Spawned))]
        public static void Spawned(Character __instance)
        {
            if (!__instance.IsPlayer() || __instance.GetLocalAuthorityMask() != 1)
                return; // not our own local player's character - do nothing

            if (__instance.GetComponentInChildren<GrassRestoreTool>() != null)
                return; // already attached (e.g. re-fired on respawn)

            var toolObj = new GameObject("AskaGrassRestore_Tool");
            toolObj.transform.SetParent(__instance.gameObject.transform, false);
            toolObj.transform.localPosition = new Vector3(0f, 0f, 2f);

            toolObj.AddComponent<HeightmapTool>();
            toolObj.AddComponent<GrassRestoreTool>();

            Plugin.Log.LogInfo("GrassRestoreTool attached to local player.");
        }
    }
}
