using Assets.Scripts.TreeHandling;
using UnityEngine;

public class TerrainTreeData
{
    public Texture2D TreeTexture;
    public QuadTree<TreeNode> QuadTree;
    public TreeInstance[] OriginalTreeInstances;
    public Terrain Terrain;
    public FireHandler Handler;
}
