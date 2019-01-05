using System;
using UnityEngine;

namespace PathFinding
{
	public enum NodeType
	{
		Open, Breakable, Impassable,
		Error
	}
	
	public class Node : IHeapItem<Node>
	{
		public NodeType type;
		
		public int gCost;
		public int hCost;
		public int fCost => gCost + hCost;

		public Node parent;
		
		public Vector2Int gridPosition;

		public static NodeType FromMapTile(TileType tileType)
		{
			switch (tileType)
			{
				case TileType.Ground:
				case TileType.Ice:
				case TileType.Woods:
				case TileType.EnemySpawn:
				case TileType.PlayerSpawn:
					return NodeType.Open;
				
				case TileType.WeakWall:
				case TileType.StrongWall:
				case TileType.PlayerBase:
					return NodeType.Breakable;
				
				case TileType.Water:
					return NodeType.Impassable;
				
			}
			return NodeType.Error;
		}

		int IComparable<Node>.CompareTo(Node other)
		{
			int compare = fCost.CompareTo(other.fCost);
			if (compare == 0)
			{
				compare = hCost.CompareTo(other.hCost);
			}
			return -compare;
		}

		int IHeapItem<Node>.heapIndex { get; set; }
	}
}