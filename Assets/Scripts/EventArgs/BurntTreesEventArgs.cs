using System;
using System.Collections.Generic;
using UnityEngine;

public class BurntTreesEventArgs : EventArgs
{
    public List<Vector2> BurntTrees;

    public BurntTreesEventArgs()
    {
        BurntTrees = new List<Vector2>();
    }
}