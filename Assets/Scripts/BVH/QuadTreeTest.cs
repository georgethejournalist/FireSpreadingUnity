using UnityEngine;
using System.Collections;

public class QuadTreeTest : MonoBehaviour
{
    public class TestObject : IQuadTreeObject
    {
        private Vector3 m_vPosition;
        public TestObject(Vector3 position)
        {
            m_vPosition = position;
        }
        public Vector2 GetPosition()
        {
            //Ignore the Y position, Quad-trees operate on a 2D plane.
            return new Vector2(m_vPosition.x, m_vPosition.z);
        }
    }
    QuadTree<TestObject> quadTree;
    void OnEnable()
    {
        quadTree = new QuadTree<TestObject>(10, new Rect(-1000, -1000, 2000, 2000));
        for (int i = 0; i < 1000; i++)
        {
            TestObject newObject = new TestObject(new Vector3(Random.Range(-900, 900), 0, Random.Range(-900, 900)));
            quadTree.Insert(newObject);
        }
    }
    void OnDrawGizmos()
    {
        if (quadTree != null)
        {
            quadTree.DrawDebug();
        }
    }
}