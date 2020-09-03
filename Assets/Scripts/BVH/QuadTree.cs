using System.Collections.Generic;
using UnityEngine;

/*
Quadtree by Just a Pixel (Danny Goodayle) - http://www.justapixel.co.uk
Copyright (c) 2015
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

//Any object that you insert into the tree must implement this interface
public interface IQuadTreeObject
{
	Vector2 GetPosition();
}
public class QuadTree<T> where T : IQuadTreeObject
{
	private int _maxObjectCount;
	private List<T> _storedObjects;
	private Rect _bounds;
	private QuadTree<T>[] _cells;

	// Cache for GC Alloc.
	private List<T> _returnedObjects;
	private List<T> _cellObjects;

    public QuadTree(
        int maxSize,
        Bounds bounds)
    {
		_bounds = new Rect(bounds.min.x, bounds.min.z, bounds.size.x, bounds.size.z);
        _maxObjectCount = maxSize;
		_cells = new QuadTree<T>[4];
		_storedObjects = new List<T>(maxSize);
    }

	public QuadTree(int maxSize, Rect bounds)
	{
		_bounds = bounds;
		_maxObjectCount = maxSize;
		_cells = new QuadTree<T>[4];
		_storedObjects = new List<T>(maxSize);
	}
	public void Insert(T objectToInsert)
	{

		if (_cells[0] != null)
		{
			int iCell = GetCellToInsertObject(objectToInsert.GetPosition());
			if (iCell > -1)
			{
				_cells[iCell].Insert(objectToInsert);
			}
			return;
		}
		_storedObjects.Add(objectToInsert);
		//Objects exceed the maximum count
		if (_storedObjects.Count > _maxObjectCount)
		{
			//Split the quad into 4 sections
			if (_cells[0] == null)
			{
				float subWidth = (_bounds.width / 2f);
				float subHeight = (_bounds.height / 2f);
				float x = _bounds.x;
				float y = _bounds.y;
				_cells[0] = new QuadTree<T>(_maxObjectCount, new Rect(x + subWidth, y, subWidth, subHeight));
				_cells[1] = new QuadTree<T>(_maxObjectCount, new Rect(x, y, subWidth, subHeight));
				_cells[2] = new QuadTree<T>(_maxObjectCount, new Rect(x, y + subHeight, subWidth, subHeight));
				_cells[3] = new QuadTree<T>(_maxObjectCount, new Rect(x + subWidth, y + subHeight, subWidth, subHeight));
			}
			//Reallocate this quads objects into its children
			int i = _storedObjects.Count - 1; ;
			while (i >= 0)
			{
				T storedObj = _storedObjects[i];
				int iCell = GetCellToInsertObject(storedObj.GetPosition());
				if (iCell > -1)
				{
					_cells[iCell].Insert(storedObj);
				}
				_storedObjects.RemoveAt(i);
				i--;
			}
		}
	}
	public void Remove(T objectToRemove)
	{
		if (ContainsLocation(objectToRemove.GetPosition()))
		{
			_storedObjects.Remove(objectToRemove);
			if (_cells[0] != null)
			{
				for (int i = 0; i < 4; i++)
				{
					_cells[i].Remove(objectToRemove);
				}
			}
		}
	}
	public List<T> RetrieveObjectsInArea(Rect area)
	{
		if (_returnedObjects == null)
			_returnedObjects = new List<T>();

		_returnedObjects.Clear();

		if (AreRectsOverlappping(_bounds, area))
		{
			for (int i = 0; i < _storedObjects.Count; i++)
			{
				if (_storedObjects[i] != null && area.Contains(_storedObjects[i].GetPosition()))
				{
					_returnedObjects.Add(_storedObjects[i]);
				}
			}
			if (_cells[0] != null)
			{
				for (int i = 0; i < 4; i++)
				{
					_cells[i].RetrieveObjectsInAreaNoAlloc(area, ref _returnedObjects);
				}
			}
		}
		return _returnedObjects;
	}

	public void RetrieveObjectsInAreaNoAlloc(Rect area, ref List<T> results)
	{
		if (AreRectsOverlappping(_bounds, area))
		{
			for (int i = 0; i < _storedObjects.Count; i++)
			{
				if (_storedObjects[i] != null && area.Contains(_storedObjects[i].GetPosition()))
				{
					results.Add(_storedObjects[i]);
				}
			}
			if (_cells[0] != null)
			{
				for (int i = 0; i < 4; i++)
				{
					_cells[i].RetrieveObjectsInAreaNoAlloc(area, ref results);
				}
			}
		}
	}

	public void Clear()
	{
		_storedObjects.Clear();

		for (int i = 0; i < _cells.Length; i++)
		{
			if (_cells[i] != null)
			{
				_cells[i].Clear();
				_cells[i] = null;
			}
		}
	}
	public bool ContainsLocation(Vector2 location)
	{
		return _bounds.Contains(location);
	}
	private int GetCellToInsertObject(Vector2 location)
	{
		for (int i = 0; i < 4; i++)
		{
			if (_cells[i].ContainsLocation(location))
			{
				return i;
			}
		}
		return -1;
	}
	bool IsValueInRange(float value, float min, float max)
	{ return (value >= min) && (value <= max); }

	bool AreRectsOverlappping(Rect A, Rect B)
	{
		bool xOverlap = IsValueInRange(A.x, B.x, B.x + B.width) ||
						IsValueInRange(B.x, A.x, A.x + A.width);

		bool yOverlap = IsValueInRange(A.y, B.y, B.y + B.height) ||
						IsValueInRange(B.y, A.y, A.y + A.height);

		return xOverlap && yOverlap;
	}
	public void DrawDebug()
	{
		Gizmos.DrawLine(new Vector3(_bounds.x, 0, _bounds.y), new Vector3(_bounds.x, 0, _bounds.y + _bounds.height));
		Gizmos.DrawLine(new Vector3(_bounds.x, 0, _bounds.y), new Vector3(_bounds.x + _bounds.width, 0, _bounds.y));
		Gizmos.DrawLine(new Vector3(_bounds.x + _bounds.width, 0, _bounds.y), new Vector3(_bounds.x + _bounds.width, 0, _bounds.y + _bounds.height));
		Gizmos.DrawLine(new Vector3(_bounds.x, 0, _bounds.y + _bounds.height), new Vector3(_bounds.x + _bounds.width, 0, _bounds.y + _bounds.height));
		if (_cells[0] != null)
		{
			for (int i = 0; i < _cells.Length; i++)
			{
				if (_cells[i] != null)
				{
					_cells[i].DrawDebug();
				}
			}
		}
	}
}