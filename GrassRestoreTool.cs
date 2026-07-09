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

            _heightmapTool.radius = Plugin.ConfigRadius.Value;
            _heightmapTool.clearVegetation = false;
            _heightmapTool.setTerrainType = true;
            _heightmapTool.terrainType = TerraformingMap.TerrainType.NATURAL;

            _heightmapTool.Run(_operation, position);
            _heightmapTool.PaintHere();

            Plugin.Log.LogInfo($"Grass restored at {position} (radius {Plugin.ConfigRadius.Value}).");
        }
    }
}
