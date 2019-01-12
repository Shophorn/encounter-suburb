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
		public readonly NodeType type;
		
		public int gCost;
		public int hCost;
		public int fCost => gCost + hCost;

		public Node parent;

		public const int maxMovePenalty = 100;
		
		public readonly int preferBreakWallsPenaltyRaw;
		public readonly int preferDriveAroundPenaltyRaw;

		public int preferBreakWallsPenalty;
		public int preferDriveAroundPenalty;
		
		public Vector2Int gridPosition;
		
		public Node(int x, int y, NodeType type, int breakWallsPenalty, int driveAroundPenalty)
		{
			gridPosition = new Vector2Int(x, y);
			this.type = type;

			// What?
			preferBreakWallsPenaltyRaw = preferBreakWallsPenalty = breakWallsPenalty;
			preferDriveAroundPenaltyRaw = preferDriveAroundPenalty = driveAroundPenalty;
		}

		public static NodeType TypeFromMapTile(TileType tileType)
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

		public static int GetPenalty(TileType tileType, bool preferBreakWalls)
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
					return preferBreakWalls ? maxMovePenalty / 10 : maxMovePenalty / 2;
				
				case TileType.StrongWall:
					return preferBreakWalls ? maxMovePenalty / 5 : maxMovePenalty;
					
				case TileType.Water:
					return maxMovePenalty;
				
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