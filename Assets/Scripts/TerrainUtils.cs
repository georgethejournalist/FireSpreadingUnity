using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public static class TerrainUtils
    {
        public static Vector3 WorldPositionToTerrain(Vector3 worldPosition, Terrain terrain, out bool belongsToTile)
        {
            var terrainPos = terrain.gameObject.transform.position;
            var size = terrain.terrainData.size;
            var resolution = terrain.terrainData.heightmapResolution;

            var offset = worldPosition - terrainPos;
            var norm = new Vector3(offset.x / size.x, offset.y / size.y, offset.z / size.z);

            var result = norm * resolution;

            belongsToTile = norm.x >= 0 && norm.x <= 1 && norm.z >= 0 && norm.z <= 1;

            return result;
        }
    }
}
