using SSSGame;
using UnityEngine;

namespace AskaPlantGrass
{
    public class PlantGrassTool : MonoBehaviour
    {
        private const float RadiusMeters = 1f;

        private HeightmapTool _heightmapTool;
        private readonly TerraformingToolOperation _operation = TerraformingToolOperation.PAINT;

        private void Start()
        {
            _heightmapTool = GetComponent<HeightmapTool>();
            if (_heightmapTool == null)
                Plugin.Log.LogError("PlantGrassTool: HeightmapTool component missing!");
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
            // placed Structure's actual footprint overlaps the paint radius.
            // Only consider colliders tagged as the structure's ground footprint
            // (Structure.c_FootprintTag) - a Structure can carry other, much larger
            // colliders (interaction range, heat/warmth radius, etc.) that aren't
            // its physical footprint and would otherwise cause false positives far
            // outside the building's actual extent.
            foreach (var overlap in Physics.OverlapSphere(position, RadiusMeters))
            {
                if (!overlap.CompareTag(Structure.c_FootprintTag))
                    continue;

                var structure = overlap.GetComponentInParent<Structure>();
                if (structure != null)
                {
                    Plugin.Log.LogInfo(
                        $"Skipped grass restore at {position}: blocked by structure '{structure.GetName()}' " +
                        $"(footprint collider '{overlap.gameObject.name}' at {overlap.transform.position}, " +
                        $"{Vector3.Distance(position, overlap.transform.position):F2}m away).");
                    return;
                }
            }

            _heightmapTool.radius = RadiusMeters;
            _heightmapTool.clearVegetation = false;
            _heightmapTool.setTerrainType = true;
            _heightmapTool.terrainType = TerraformingMap.TerrainType.NATURAL;

            // Only repaint cells that are currently trampled DIRT, so ROAD/PATH/BEDROCK
            // (and already-NATURAL ground) are left alone within the radius.
            _heightmapTool.requiresTerrainType = true;
            _heightmapTool.requiredTerrainType = TerraformingMap.TerrainType.DIRT;

            _heightmapTool.Run(_operation, position);
            _heightmapTool.PaintHere();

            Plugin.Log.LogInfo($"Grass restored at {position}.");
        }
    }
}
