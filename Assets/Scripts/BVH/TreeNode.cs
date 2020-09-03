using UnityEngine;


public class TreeNode : IQuadTreeObject
{
    public Vector2 Position;
    public int TreeIndex;
    public byte TreeType;

    public Vector2 GetPosition() => Position;
}
