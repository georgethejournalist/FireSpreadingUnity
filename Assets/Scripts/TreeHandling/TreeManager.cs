using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Utils;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.TreeHandling
{
    public class TreeManager : MonoBehaviour
    {
        public bool DrawQuadTreeForSelectedTerrain = false;
        public bool DrawLastCheckedPosition = false;

        public float SelectionRange = 6.5f;

        public int IndexOfLivePrototype = 0;
        public int IndexOfBurningPrototype = 1;

        public int IndexOfDeadPrototype = 2;

        public Dictionary<Terrain, TerrainTreeData> _terrainToTreeData = new Dictionary<Terrain, TerrainTreeData>();

        private Vector3 _lastCheckedPosition = Vector3.zero;
        private bool _globalState = false;

        private Dictionary<FireHandler, TerrainTreeData> _handlersToTreeData = new Dictionary<FireHandler, TerrainTreeData>();
        private int _handlerWithRenderer = 0;

        public Dictionary<FireHandler, TerrainTreeData> FireHandlers => _handlersToTreeData;

        public void Init()
        {
            var terrains = FindObjectsOfType<Terrain>();
            if (terrains.Length == 0)
            {
                Debug.Log("No terrain tiles found, fire manager not initialized");
                return;
            }

            foreach (var terrain in terrains)
            {
                var treeData = PrepareTerrainTreeData(terrain);
                _terrainToTreeData.Add(terrain, treeData);

                var fireHandler = this.gameObject.AddComponent<FireHandler>();
                fireHandler.Init(treeData);

                _handlersToTreeData.Add(fireHandler, treeData);
                treeData.Handler = fireHandler;

                fireHandler.BurntTreesAdded += OnNewBurntTreesReported;
            }
        }

        public void SetRendererToNextHandler(Renderer renderer)
        {
            _handlerWithRenderer++;
            if (_handlerWithRenderer >= _handlersToTreeData.Count)
            {
                _handlerWithRenderer = 0;
            }

            for (int index = 0; index < _handlersToTreeData.Count; index++)
            {
                var dataPair = _handlersToTreeData.ElementAt(index);
                var handler = dataPair.Key;
                if (index == _handlerWithRenderer)
                {
                    handler.Renderer = renderer;
                    continue;
                }

                handler.Renderer = null;
            }
        }

        public void GlobalFireSimulationStart()
        {
            foreach (var handlerPair in _handlersToTreeData)
            {
                handlerPair.Key.StartSimulation();
            }
        }

        public void GlobalFireSimulationStop()
        {
            foreach (var handlerPair in _handlersToTreeData)
            {
                handlerPair.Key.StopSimulation();
            }
        }

        public void ToggleGlobalFireSimulation()
        {
            _globalState = !_globalState;

            if (_globalState)
            {
                GlobalFireSimulationStart();
            }
            else
            {
                GlobalFireSimulationStop();
            }
        }

        public void LocalFireSimulationStart(Terrain terrain)
        {
            if (_terrainToTreeData.ContainsKey(terrain))
            {
                var data = _terrainToTreeData[terrain];
                data.Handler.StartSimulation();
            }
        }



        public void LocalFireSimulationStop(Terrain terrain)
        {
            if (_terrainToTreeData.ContainsKey(terrain))
            {
                var data = _terrainToTreeData[terrain];
                data.Handler.StopSimulation();
            }
        }

        public void SetGlobalWindSpeed(int windSpeed)
        {
            foreach (var handlerPair in _handlersToTreeData)
            {
                handlerPair.Key.WindSpeed = windSpeed;
            }
        }

        public void SetGlobalWindDirection(int windDirection)
        {
            foreach (var handlerPair in _handlersToTreeData)
            {
                handlerPair.Key.WindDir = windDirection;
            }
        }

        public void SetGlobalNaturalSpread(int naturalSpreadValue)
        {
            foreach (var handlerPair in _handlersToTreeData)
            {
                handlerPair.Key.NaturalFireSpreadSpeed = naturalSpreadValue;
            }
        }

        public void SetGlobalSimulationStepTime(float value)
        {
            foreach (var handlerPair in _handlersToTreeData)
            {
                handlerPair.Key.StepTime = value;
            }
        }

        public void SetLocalWindSpeed(int windSpeed, Terrain terrain)
        {
            if (_terrainToTreeData.ContainsKey(terrain))
            {
                var data = _terrainToTreeData[terrain];
                data.Handler.WindSpeed = windSpeed;
            }
        }

        public void SetLocalWindDirection(int windDirection, Terrain terrain)
        {
            if (_terrainToTreeData.ContainsKey(terrain))
            {
                var data = _terrainToTreeData[terrain];
                data.Handler.WindDir = windDirection;
            }
        }

        private TerrainTreeData PrepareTerrainTreeData(Terrain terrain)
        {
            var tex = GetTerrainTreesToTexture(terrain);

#if DEBUG
            TextureUtils.SaveTexture(tex, $"{Application.dataPath}/Resources/TreeSplatmaps");
#endif
            var treeData = new TerrainTreeData();
            treeData.TreeTexture = tex;
            treeData.Terrain = terrain;

            var data = terrain.terrainData;
            var size = data.size;
            var position = terrain.gameObject.transform.position;
            var rect = new Rect(position.x, position.z, size.x, size.z);

            var bounds = data.bounds;
            var resolution = data.heightmapResolution;

            var quadTree = new QuadTree<TreeNode>(5, rect);
            treeData.QuadTree = quadTree;

            // copying for posterity
            treeData.OriginalTreeInstances = data.treeInstances.ToArray();

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

            return treeData;
        }

        public void GlobalGenerateTrees(int count = 10000)
        {
            for (var index = _terrainToTreeData.Count - 1; index >= 0; index--)
            {
                var dataPair = _terrainToTreeData.ElementAt(index);
                var terrain = dataPair.Key;
                var treeData = dataPair.Value;

                var handler = treeData.Handler;
                var quadTree = treeData.QuadTree;
                quadTree.Clear();

                TerrainUtils.MassPlaceTreesOfPrototype(terrain.terrainData, count, IndexOfLivePrototype, Color.green);
                terrain.Flush();

                var regenTreeData = PrepareTerrainTreeData(terrain);
                regenTreeData.Handler = handler;

                _terrainToTreeData[terrain] = regenTreeData;
                _handlersToTreeData[handler] = regenTreeData;

                handler.ReInit(regenTreeData);
            }
        }

        

        public void GlobalClearAllTrees()
        {
            for (var index = _terrainToTreeData.Count - 1; index >= 0; index--)
            {
                var dataPair = _terrainToTreeData.ElementAt(index);
                var terrain = dataPair.Key;
                var treeData = dataPair.Value;
                terrain.terrainData.SetTreeInstances(new TreeInstance[0], false);
                terrain.Flush();

                var quadTree = treeData.QuadTree;
                quadTree.Clear();

                var handler = treeData.Handler;
                var originalTreeInstances = treeData.OriginalTreeInstances;

                var regenTreeData = PrepareTerrainTreeData(terrain);
                regenTreeData.OriginalTreeInstances = originalTreeInstances;
                regenTreeData.Handler = handler;

                _terrainToTreeData[terrain] = regenTreeData;
                _handlersToTreeData[handler] = regenTreeData;

                handler.ReInit(regenTreeData);
            }


            //foreach (var dataPair in _terrainToTreeData)
            //{
            //    var terrain = dataPair.Key;
            //    var treeData = dataPair.Value;
            //    terrain.terrainData.SetTreeInstances(new TreeInstance[0], false);
            //    treeData.QuadTree.Clear();
            //    var handler = treeData.Handler;

            //    var regenTreeData = PrepareTerrainTreeData(terrain);
            //    _terrainToTreeData[terrain] = regenTreeData;
            //    _handlersToTreeData[handler] = regenTreeData;

            //    handler.ReInit(treeData);
            //}
        }

        public void LocalClearAllTrees(Terrain terrain)
        {
            if (terrain == null)
            {
                Debug.LogError("Needs valid terrain instance to clear trees");
                return;
            }

            terrain.terrainData.SetTreeInstances(new TreeInstance[0], false);

            var treeData = _terrainToTreeData[terrain];
            treeData.QuadTree.Clear();
            treeData.Handler.ResetSimulation();
            terrain.Flush();
        }


        public void PlaceTreeUnderCursor(Vector3 worldPosition, Terrain terrain)
        {
            if (terrain == null)
            {
                Debug.LogError("Tried to place a tree but the terrain was null");
                return;
            }

            var localSpace = TerrainUtils.WorldPositionToTerrain(worldPosition, terrain, out var belongsToTile);
            var normalizedLocalSpace = localSpace / (terrain.terrainData.heightmapResolution - TerrainUtils.UnityTextureBorder);

            var treeInstance = new TreeInstance();
            treeInstance.position = normalizedLocalSpace;
            treeInstance.color = Color.green;
            treeInstance.lightmapColor = Color.green;
            treeInstance.prototypeIndex = IndexOfLivePrototype;
            treeInstance.heightScale = 1;
            treeInstance.rotation = 1;
            treeInstance.widthScale = 1;
            

            var treeInstancesCopy = terrain.terrainData.treeInstances.ToArray();
            var length = treeInstancesCopy.Length;

            Array.Resize(ref treeInstancesCopy, length + 1);
            treeInstancesCopy[length] = treeInstance;

            terrain.terrainData.SetTreeInstances(treeInstancesCopy, false);

            var treeData = _terrainToTreeData[terrain];
            var treeNode = new TreeNode()
            {
                Position = new Vector2(worldPosition.x, worldPosition.z),
                TreeIndex = length,
                TreeType = (byte)IndexOfLivePrototype
            };

            var quadTree = treeData.QuadTree;
            quadTree.Insert(treeNode);

            var handler = treeData.Handler;
            handler.MarkTreeLive(terrain.terrainData.GetTreeInstance(length));

            terrain.Flush();
        }

        public void RemoveTreeUnderCursor(Vector3 worldPosition, Terrain terrain)
        {
            List<TreeNode> nodes = new List<TreeNode>();
            bool foundAny = FindAllTreeInstancesAroundPosition(worldPosition, SelectionRange, terrain, ref nodes, false);

            if (!foundAny)
            {
                Debug.LogWarning("Wanted to remove a tree under cursor but none was found");
                return;
            }

            var node = nodes.First();
            var treeInstanceIndex = node.TreeIndex;

            // we don't want to remove the instance, otherwise we'd invalidate the indices
            var treeInstance = terrain.terrainData.GetTreeInstance(treeInstanceIndex);
            treeInstance.heightScale = 0;

            // by changing the heightScale to zero, the tree is effectively hidden and we can keep the indices...
            terrain.terrainData.SetTreeInstance(treeInstanceIndex, treeInstance);

            if (!_terrainToTreeData.ContainsKey(terrain))
            {
                Debug.LogWarning("The terrain in question does not have a tree data (quadtree, texture etc.) ready - perhaps it's been created during runtime? Be sure to call PrepareTerrainTreeData for such tiles.");
                return;
            }

            // removing it from the quadTree is fair, this way it will not be found through BVH checks
            var treeData = _terrainToTreeData[terrain];
            var quadTree = treeData.QuadTree;
            quadTree.Remove(node);

            terrain.Flush();
        }

        public void ToggleTreeStateUnderCursor(Vector3 worldPosition)
        {
            int treeInstanceIndex = FindTreeInstanceThroughBVH(worldPosition, out var terrain, out var node);
            if (treeInstanceIndex == -1)
            {
                Debug.LogWarning("Wanted to remove a tree under cursor but none was found");
                return;
            }

            // adjusting just one tree instance through SetInstance() would not work, as Unity throws when this is used for changing prototypes
            // all we can do is make a copy, adjust that and pass it in
            var treeInstancesCopy = terrain.terrainData.treeInstances.ToArray();
            
            var treeInstance = treeInstancesCopy[treeInstanceIndex];
            treeInstance.prototypeIndex = treeInstance.prototypeIndex == IndexOfLivePrototype ? IndexOfBurningPrototype : IndexOfLivePrototype;
            treeInstancesCopy[treeInstanceIndex] = treeInstance;

            var treeData = _terrainToTreeData[terrain];
            // propagate the change to the fire handler
            if (treeInstance.prototypeIndex == IndexOfLivePrototype)
            {
                treeData.Handler.MarkTreeLive(treeInstance);
            }
            else
            {
                treeData.Handler.MarkTreeOnFire(treeInstance);
            }

            terrain.terrainData.SetTreeInstances(treeInstancesCopy, false);
            terrain.Flush();
        }

        

        public bool FindAllTreeInstancesAroundPosition(Vector3 worldPosition, float selectionRange, Terrain terrain, ref List<TreeNode> nodes, bool clear = true)
        {
            if (clear)
            {
                nodes.Clear();
            }

            TerrainUtils.WorldPositionToTerrain(worldPosition, terrain, out var belongsToTile);

            if (!belongsToTile)
            {
                return false;
            }

            var treeData = _terrainToTreeData[terrain];
            var quadTree = treeData.QuadTree;

            var rect = new Rect(worldPosition.x, worldPosition.z, selectionRange, selectionRange);

            quadTree.RetrieveObjectsInAreaNoAlloc(rect, ref nodes);

            if (!nodes.Any())
            {
                Debug.Log("No nodes found in area");
                return false;
            }

            return true;
        }

        public int FindTreeInstanceInTerrain(Vector3 worldPosition, Terrain terrain, out TreeNode node)
        {
            node = null;
            TerrainUtils.WorldPositionToTerrain(worldPosition, terrain, out var belongsToTile);

            if (!belongsToTile)
            {
                return -1;
            }

            var treeData = _terrainToTreeData[terrain];
            var quadTree = treeData.QuadTree;

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
                var twoDimWorldPos = new Vector2(worldPosition.x, worldPosition.z);
                float minDist = Single.MaxValue;
                foreach (var treeNode in nodes)
                {
                    var squaredDistance = (twoDimWorldPos - treeNode.Position).SqrMagnitude();
                    if (squaredDistance >= minDist)
                    {
                        node = treeNode;
                        minDist = squaredDistance;
                    }
                }

                return -1;
            }
        }

        public int FindTreeInstanceThroughBVH(Vector3 worldPosition, out Terrain ownerTerrain, out TreeNode node)
        {
            node = null;
            ownerTerrain = null;

            foreach (var pair in _terrainToTreeData)
            {
                var terrain = pair.Key;
                var local = TerrainUtils.WorldPositionToTerrain(worldPosition, terrain, out bool belongsToTile);
                if (!belongsToTile)
                {
                    continue;
                }

                ownerTerrain = terrain;

                var quadTree = pair.Value.QuadTree;

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
                    var twoDimWorldPos = new Vector2(worldPosition.x, worldPosition.z);
                    float minDist = Single.MaxValue;
                    foreach (var treeNode in nodes)
                    {
                        var squaredDistance = (twoDimWorldPos - treeNode.Position).SqrMagnitude();
                        if (squaredDistance >= minDist)
                        {
                            node = treeNode;
                            minDist = squaredDistance;
                        }
                    }

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
                var treeData = _terrainToTreeData[terrain];
                var tex = treeData.TreeTexture;

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
            var resolution = data.heightmapResolution - TerrainUtils.UnityTextureBorder;

            var trees = data.treeInstances;

            var tex = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
            tex.name = $"{terrain.name}-TreeTexture";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;

            tex.ClearTextureToColor();

            foreach (var tree in trees)
            {
                // local tree position
                var pos = tree.position;
                var texX = (int)(pos.x * resolution);
                var texY = (int)(pos.z * resolution);

                Color color = tree.prototypeIndex == IndexOfLivePrototype ? Color.green : Color.red;

                tex.SetPixel(texX, texY, color);
            }

            tex.Apply();

            return tex;
        }

        private void OnNewBurntTreesReported(object sender, BurntTreesEventArgs e)
        {
            var handler = sender as FireHandler;
            if (handler == null)
            {
                Debug.LogError("New Burnt Trees reported not handling event from FireHandler?");
                return;
            }

            var treeData = _handlersToTreeData[handler];
            var treeNodes = new List<TreeNode>();
            var resolution = treeData.Terrain.terrainData.heightmapResolution - TerrainUtils.UnityTextureBorder;
            foreach (var burntTreePos in e.BurntTrees)
            {
                var localSpace = burntTreePos;
                var worldSpace = new Vector3(localSpace.x * resolution, 0, localSpace.y * resolution);

                var terrainPosition = treeData.Terrain.transform.position;

                var offsetPosition = worldSpace + terrainPosition;

                bool foundAny = FindAllTreeInstancesAroundPosition(offsetPosition, SelectionRange * 2, treeData.Terrain, ref treeNodes, false);
            }

            ToggleTreeNodes(treeNodes, IndexOfBurningPrototype, treeData.Terrain);
        }

        private void ToggleTreeNodes(List<TreeNode> nodes, int prototypeIndex, Terrain terrain)
        {
            var treeInstancesCopy = terrain.terrainData.treeInstances.ToArray();

            foreach (var node in nodes)
            {
                var index = node.TreeIndex;

                var treeInstance = treeInstancesCopy[index];
                treeInstance.prototypeIndex = prototypeIndex;
                treeInstancesCopy[index] = treeInstance;
            }

            terrain.terrainData.SetTreeInstances(treeInstancesCopy, false);
            terrain.Flush();
        }

        private void ToggleTreeInstances(List<int> instanceIndices, int prototypeIndex, Terrain terrain)
        {
            var treeInstancesCopy = terrain.terrainData.treeInstances.ToArray();

            foreach (var index in instanceIndices)
            {
                var treeInstance = treeInstancesCopy[index];
                treeInstance.prototypeIndex = prototypeIndex;
                treeInstancesCopy[index] = treeInstance;
            }

            terrain.terrainData.SetTreeInstances(treeInstancesCopy, false);
            terrain.Flush();
        }

        public void CleanUp()
        {
            Debug.Log("TreeManager cleaning up changes to tree instances");
            foreach (var dataPair in _terrainToTreeData)
            {
                var terrain = dataPair.Key;
                var treeData = dataPair.Value;

                var data = terrain.terrainData;
                data.SetTreeInstances(treeData.OriginalTreeInstances, false);
                terrain.Flush();
            }
        }

        void OnApplicationQuit()
        {
            CleanUp();
        }

        void OnDrawGizmos()
        {
#if DEBUG
            //if (DrawQuadTreeForSelectedTerrain)
            //{
            //    var terrain = Selection.activeGameObject?.GetComponent<Terrain>();

            //    if (terrain != null && _terrainToTreeData.ContainsKey(terrain))
            //    {
            //        var treeData = _terrainToTreeData[terrain];
            //        treeData.QuadTree?.DrawDebug();
            //    }
            //}
#endif
            if (DrawLastCheckedPosition && _lastCheckedPosition != Vector3.zero)
            {
                var prevColor = Gizmos.color;
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(_lastCheckedPosition, new Vector3(SelectionRange, SelectionRange, SelectionRange));
                Gizmos.color = prevColor;
            }
        }

        public void SetRandomTreesOnFire()
        {
            int treeAmount = 5;
            //Random.InitState(DateTime.UtcNow.Millisecond);
            Random.InitState(15);
            
            foreach (var dataPair in _terrainToTreeData)
            {
                var terrain = dataPair.Key;
                var data = terrain.terrainData;
                var treeData = dataPair.Value;
                var handler = treeData.Handler;

                var indices = new List<int>();
                var length = data.treeInstanceCount;
                for (int i = 0; i < treeAmount; i++)
                {
                    var randomIndex = Random.Range(0, length);
                    indices.Add(randomIndex);
                    var instance = data.GetTreeInstance(randomIndex);
                    handler.MarkTreeOnFire(instance);
                }

                ToggleTreeInstances(indices, IndexOfBurningPrototype, terrain);
            }
        }

        
    }
}
