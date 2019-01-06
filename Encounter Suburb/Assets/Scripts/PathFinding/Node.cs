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

		public int preferBreakWallsPenalty;
		public int preferDriveAroundPenalty;
		
		public Vector2Int gridPosition;
		
		// Create constructor
		public Node(int x, int y, TileType tileType)
		{
			gridPosition = new Vector2Int(x, y);
			type = FromMapTile(tileType);
			preferBreakWallsPenalty = GetPenalty(tileType, true);
			preferDriveAroundPenalty = GetPenalty(tileType, false);
		}


		private static NodeType FromMapTile(TileType tileType)
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

		private static int GetPenalty(TileType tileType, bool preferBreakWalls)
		{
			switch (tileType)
			{
				case TileType.Ground:
				case TileType.Ice:
				case TileType.Woods:
				case TileType.EnemySpawn:
				case TileType.PlayerSpawn:
					return 0;
				
				case TileType.WeakWall:
				case TileType.PlayerBase:
					return preferBreakWalls ? 2 : 5;
				
				case TileType.StrongWall:
					return preferBreakWalls ? 4 : 10;
					
				case TileType.Water:
					return 20;
				
			}

			return -1;
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