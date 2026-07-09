using SSSGame;
using UnityEngine;

namespace AskaGrassRestore
{
    public class GrassRestoreTool : MonoBehaviour
    {
        private HeightmapTool _heightmapTool;
        private readonly TerraformingToolOperation _operation = TerraformingToolOperation.PAINT;

        private void Start()
        {
            _heightmapTool = GetComponent<HeightmapTool>();
            if (_heightmapTool == null)
                Plugin.Log.LogError("GrassRestoreTool: HeightmapTool component missing!");
        }

        private void Update()
        {
            if (!Plugin.ConfigEnabled.Value || _heightmapTool == null)
                return;

            if (!Input.GetKeyDown(Plugin.ConfigKey.Value))
                return;

            Vector3 position = transform.position;

            // Terrain type alone doesn't exclude buildings - the ground under a
            // structure is still typed DIRT/NATURAL. Skip painting entirely if any
            // placed Structure overlaps the paint radius.
            foreach (var overlap in Physics.OverlapSphere(position, Plugin.ConfigRadius.Value))
            {
                if (overlap.GetComponentInParent<Structure>() != null)
                {
                    Plugin.Log.LogInfo($"Skipped grass restore at {position}: a structure is in range.");
                    return;
                }
            }

            _heightmapTool.radius = Plugin.ConfigRadius.Value;
            _heightmapTool.clearVegetation = false;
            _heightmapTool.setTerrainType = true;
            _heightmapTool.terrainType = TerraformingMap.TerrainType.NATURAL;

            // Only repaint cells that are currently trampled DIRT, so ROAD/PATH/BEDROCK
            // (and already-NATURAL ground) are left alone within the radius.
            _heightmapTool.requiresTerrainType = true;
            _heightmapTool.requiredTerrainType = TerraformingMap.TerrainType.DIRT;

            _heightmapTool.Run(_operation, position);
            _heightmapTool.PaintHere();

            Plugin.Log.LogInfo($"Grass restored at {position} (radius {Plugin.ConfigRadius.Value}).");
        }
    }
}
