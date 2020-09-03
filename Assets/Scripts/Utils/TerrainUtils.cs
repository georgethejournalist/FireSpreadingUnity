using UnityEngine;

namespace Assets.Scripts.Utils
{
    public static class TerrainUtils
    {
        public const int UnityTextureBorder = 1;

        public static Vector3 WorldPositionToTerrain(Vector3 worldPosition, Terrain terrain, out bool belongsToTile)
        {
            var terrainPos = terrain.gameObject.transform.position;
            var size = terrain.terrainData.size;
            // TODO what about here? The heightmap here will be one pixel larger!
            var resolution = terrain.terrainData.heightmapResolution - UnityTextureBorder;

            var offset = worldPosition - terrainPos;
            var norm = new Vector3(offset.x / size.x, offset.y / size.y, offset.z / size.z);

            var result = norm * resolution;

            belongsToTile = norm.x >= 0 && norm.x <= 1 && norm.z >= 0 && norm.z <= 1;

            return result;
        }

        public static void MassPlaceTreesOfPrototype(TerrainData terrainData, int numberOfTrees, int prototypeIndex, Color color)
        {
            int length = terrainData.treePrototypes.Length;
            if (length == 0)
            {
                Debug.Log((object)"Can't place trees because no prototypes are defined");
                return;
            }

            if (prototypeIndex >= length)
            {
                Debug.Log("Can't place trees - invalid prototype desired");
                return;
            }

            //Undo.RegisterCompleteObjectUndo((UnityEngine.Object)terrainData, "Mass Place Trees Of Prototype");

            TreeInstance[] treeInstances = new TreeInstance[numberOfTrees];
            int num = 0;
            while (num < treeInstances.Length)
            {
                TreeInstance treeInstance = new TreeInstance();
                treeInstance.position = new Vector3(UnityEngine.Random.value, 0.0f, UnityEngine.Random.value);
                    
                if (terrainData.GetSteepness(treeInstance.position.x, treeInstance.position.z) < 30.0)
                {
                    treeInstance.color = color;
                    treeInstance.lightmapColor = Color.white;
                    treeInstance.prototypeIndex = prototypeIndex;
                    treeInstance.heightScale = 1;
                    treeInstance.widthScale = 1;
                    treeInstance.rotation = 1;
                    treeInstances[num++] = treeInstance;
                }
            }

            terrainData.SetTreeInstances(treeInstances, true);
        }
    }

}
