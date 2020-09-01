using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts
{
    public class TreeNode : IQuadTreeObject
    {
        public Vector2 Position;
        public int TreeIndex;
        public byte TreeType;

        public Vector2 GetPosition() => Position;
    }

    public class TreeManager : MonoBehaviour
    {
        public bool DrawQuadTreeForSelectedTerrain = false;
        public float SelectionRange = 1.5f;

        public const int UnityTextureBorder = 1;
        
        public int IndexOfLivePrototype = 0;
        public int IndexOfBurningPrototype = 1;
        public int IndexOfDeadPrototype = 2;

        private Dictionary<Terrain, Texture2D> _terrainsToTreeTexs = new Dictionary<Terrain, Texture2D>();

        private TreeInstance[] _originalTrees;

        private Dictionary<Terrain, QuadTree<TreeNode>> _terrainsToQuadTrees = new Dictionary<Terrain, QuadTree<TreeNode>>();

        private Vector3 _lastCheckedPosition;

        void Start()
        {
            var terrains = FindObjectsOfType<Terrain>();
            if (terrains.Length == 0)
            {
                Debug.Log("No terrain tiles found, fire manager not initialized");
                return;
            }

            foreach (var terrain in terrains)
            {
                var tex = GetTerrainTreesToTexture(terrain);

                SaveTexture(tex, $"{Application.dataPath}/Terrains/TreeSplatmaps");
                
                _terrainsToTreeTexs.Add(terrain, tex);

                var data = terrain.terrainData;
                var size = data.size;
                var position = terrain.gameObject.transform.position;
                var rect = new Rect(position.x, position.z, size.x, size.z);

                var bounds = data.bounds;
                var resolution = data.heightmapResolution;

                var quadTree = new QuadTree<TreeNode>(5, rect);

                for (int i = 0; i < data.treeInstances.Length; i++)
                {
                    var treeInstance = data.treeInstances[i];

                    var localSpace = treeInstance.position;
                    var worldSpace = localSpace * resolution;

                    var twoDimension = new Vector2(worldSpace.x + position.x, worldSpace.z + position.z);

                    var treeNode = new TreeNode
                    {
                        Position = twoDimension,
                        TreeIndex = i,
                        TreeType = (byte) treeInstance.prototypeIndex
                    };

                    quadTree.Insert(treeNode);
                }

                _terrainsToQuadTrees.Add(terrain, quadTree);

                // TODO remove after testing - solution with added colliders
                // create capsule collider for every terrain tree
                //for (int i = 0; i < terrain.terrainData.treeInstances.Length; i++)
                //{
                //    TreeInstance treeInstance = terrain.terrainData.treeInstances[i];

                //    GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                //    var collider = capsule.GetComponent<Collider>();
                //    var renderer = capsule.GetComponent<Renderer>();



                //    CapsuleCollider capsuleCollider = collider as CapsuleCollider;
                //    capsuleCollider.center = new Vector3(0, 5, 0);
                //    capsuleCollider.height = 10;

                //    DestroyableTree tree = capsule.AddComponent<DestroyableTree>();
                //    tree.terrainIndex = i;

                //    capsule.transform.position = Vector3.Scale(treeInstance.position, terrain.terrainData.size);
                //    capsule.tag = "Tree";
                //    capsule.transform.parent = terrain.transform;
                //    renderer.enabled = false;
                //}

            }

            
        }

        public void RemoveTreeUnderCursor(Vector3 worldPosition)
        {
            int treeInstanceIndex = FindTreeInstanceThroughBVH(worldPosition, out var terrain, out var node);
            if (treeInstanceIndex == -1)
            {
                Debug.LogWarning("Wanted to remove a tree under cursor but none was found");
                return;
            }

            // allocations everywhere, optimize...
            //var treeInstancesCopy = terrain.terrainData.treeInstances.ToList();
            //treeInstancesCopy.RemoveAt(treeInstanceIndex);

            // TODO think of a better solution...
            // we don't want to remove the instance, otherwise we'd invalidate the indices
            var treeInstance = terrain.terrainData.GetTreeInstance(treeInstanceIndex);
            treeInstance.heightScale = 0;

            // by changing the heightScale to zero, the tree is effectively hidden and we can keep the indices...
            terrain.terrainData.SetTreeInstance(treeInstanceIndex, treeInstance);
            

            //terrain.terrainData.SetTreeInstances(treeInstancesCopy.ToArray(), false);

            // removing it from the quadTree is fair, this way it will not be found through BVH checks
            var quadTree = _terrainsToQuadTrees[terrain];
            quadTree.Remove(node);
        }

        public void ToggleTreeStateUnderCursor(Vector3 worldPosition)
        {
            int treeInstanceIndex = FindTreeInstanceThroughBVH(worldPosition, out var terrain, out var node);
            if (treeInstanceIndex == -1)
            {
                Debug.LogWarning("Wanted to remove a tree under cursor but none was found");
                return;
            }

            var treeInstance = terrain.terrainData.GetTreeInstance(treeInstanceIndex);
            
            // if live, start burning; if dead or burning, turn back to life
            treeInstance.prototypeIndex = treeInstance.prototypeIndex == IndexOfLivePrototype ? IndexOfBurningPrototype : IndexOfLivePrototype;

            terrain.terrainData.SetTreeInstance(treeInstanceIndex, treeInstance);
        }

        public int FindTreeInstanceThroughBVH(Vector3 worldPosition, out Terrain ownerTerrain, out TreeNode node)
        {
            node = null;
            ownerTerrain = null;

            foreach (var pair in _terrainsToQuadTrees)
            {
                var terrain = pair.Key;
                var local = TerrainUtils.WorldPositionToTerrain(worldPosition, terrain, out bool belongsToTile);
                if (!belongsToTile)
                {
                    continue;
                }

                ownerTerrain = terrain;

                var quadTree = pair.Value;

                var rect = new Rect(worldPosition.x, worldPosition.z, SelectionRange, SelectionRange);

                var nodes = quadTree.RetrieveObjectsInArea(rect);
                _lastCheckedPosition = worldPosition;

                if (!nodes.Any())
                {
                    Debug.Log("Found no tree nodes around the specified area through BVH check");
                    return -1;
                }

                if (nodes.Count == 1)
                {
                    node = nodes[0];
                    Debug.Log($"Found exactly one tree node around the specified area through BVH check. Index: {node.TreeIndex}");
                    return nodes[0].TreeIndex;
                }
                else
                {
                    Debug.Log($"Found multiple tree nodes around the specified area through BVH check. Count: {nodes.Count}");
                    // TODO find the closest through raw distance and return it
                    return -1;
                }
            }

            return -1;
        }

        public int FindTreeInstanceIndexAtPosition(Vector3 worldPosition)
        {
            var terrains = FindObjectsOfType<Terrain>();
            if (terrains.Length == 0)
            {
                Debug.Log("No terrain tiles found, fire manager not initialized");
                return -1;
            }

            foreach (var terrain in terrains)
            {
                // find if world position is in this terrain

                // find the local position?
                var local = TerrainUtils.WorldPositionToTerrain(worldPosition, terrain, out var belongsToTile);
                if (!belongsToTile)
                {
                    continue;
                }

                // find the pixel in the tex?
                var tex = _terrainsToTreeTexs[terrain];
                var result = tex.GetPixel((int)local.x, (int)local.z);
                if (result == Color.green)
                {
                    Debug.Log("It's a tree, mate");
                }

            }

            return -1;
        }

        public Texture2D GetTerrainTreesToTexture(Terrain terrain)
        {
            var data = terrain.terrainData;
            var resolution = data.heightmapResolution;
            var size = data.size;
            var texelSize = (size.x + UnityTextureBorder) / resolution;

            var trees = data.treeInstances;

            var tex = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
            tex.name = $"{terrain.name}-TreeTexture";

            // clear the texture
            Color32 resetColor = new Color32(255, 255, 255, 0);
            Color32[] resetColorArray = tex.GetPixels32();

            for (int i = 0; i < resetColorArray.Length; i++)
            {
                resetColorArray[i] = resetColor;
            }

            tex.SetPixels32(resetColorArray);

            //var pixels = tex.GetPixels(0, 0, resolution, resolution);
            foreach (var tree in trees)
            {
                var pos = tree.position;
                var texX = (int)(pos.x * resolution);
                var texY = (int)(pos.z * resolution);

                //int oneD = (texY * resolution) + texX;

                //pixels[oneD] = Color.red;
                tex.SetPixel(texX, texY, Color.green);
            }

            return tex;
        }

        public void SaveTextureWithDialog(Texture2D tex)
        {
            string path = EditorUtility.SaveFolderPanel("Choose a directory to save the alpha maps:", "", "");
            path = path.Replace(Application.dataPath, "Assets");

            SaveTexture(tex, path);
        }

        public void SaveTexture(Texture2D tex, string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                Debug.LogError("Trying to save texture to undefined path.");
                return;
            }

            var pngData = tex.EncodeToPNG();
            if (pngData == null)
            {
                Debug.LogError("Could not convert " + tex.name + " to png. Skipping saving texture.");
                return;
            }
            
            File.WriteAllBytes(path + "/" + tex.name + ".png", pngData);
        }

        void OnDrawGizmos()
        {
            if (DrawQuadTreeForSelectedTerrain)
            {
                var terrain = Selection.activeGameObject?.GetComponent<Terrain>();

                if (terrain != null)
                {
                    _terrainsToQuadTrees[terrain].DrawDebug();
                }
            }

            if (_lastCheckedPosition != Vector3.zero)
            {
                var prevColor = Gizmos.color;
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(_lastCheckedPosition, new Vector3(SelectionRange, SelectionRange, SelectionRange));
                Gizmos.color = prevColor;
            }
        }
    }
}
