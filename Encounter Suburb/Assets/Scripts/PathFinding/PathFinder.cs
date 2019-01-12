using System.Collections.Generic;
using UnityEngine;

namespace PathFinding
{
	public class PathFinder
	{
		private const int alignedCost = 10;
		private const int diagonalCost = 14;

		private Grid grid;

		public static PathFinder instance { get; private set; }

		public static void CreateInstance(Grid grid)
		{
			instance = new PathFinder();
			instance.grid = grid;
			
			GameManager.SetDebugPath(new bool[grid.size, grid.size]);
		}

		public static void DeleteInstance()
		{
			instance = null;
		}
		
		public Path FindPath(Vector3 startPosition, Vector3 endPosition, bool preferBreakThings)
		{
			Node start = grid.NodeFromWorldPoint(startPosition);
			Node end = grid.NodeFromWorldPoint(endPosition);

			var openSet = new Heap<Node>(grid.size * grid.size);
			var closedSet = new HashSet<Node>();
			openSet.Add(start);

			while (openSet.Count > 0)
			{
				var current = openSet.PopFirst();
				closedSet.Add(current);

				if (current == end)
				{
					return BuildPathFromNodes(start, end);
				}

				var neighbours = grid.GetNodeNeighbours(current);
				for (int i = 0; i < neighbours.Length; i++)
				{
//					bool walkable = neighbours[i].type == NodeType.Open || neighbours[i].type == NodeType.Breakable;
//					if (!walkable || closedSet.Contains(neighbours[i]))
					if (neighbours[i].type == NodeType.Impassable || closedSet.Contains(neighbours[i]))
					{
						continue;
					}

					int penalty = preferBreakThings
						? neighbours[i].preferBreakWallsPenalty
						: neighbours[i].preferDriveAroundPenalty;
					
					int newMovementCost = current.gCost + Distance(current, neighbours[i]) + penalty;
					if (newMovementCost < neighbours[i].gCost || !openSet.Contains(neighbours[i]))
					{
						neighbours[i].gCost = newMovementCost;
						neighbours[i].hCost = Distance(neighbours[i], end);
						neighbours[i].parent = current;
						
						if (!openSet.Contains(neighbours[i]))
							openSet.Add(neighbours[i]);
						else
							openSet.RefreshItem(neighbours[i]);
					}
				}
			}

			return null;
		}

		private Path BuildPathFromNodes(Node start, Node end)
		{
			// Retrace
			var path = new List<Node>();
			var current = end;

			GameManager.debugPath = new bool[grid.size, grid.size];
			
			while (current != start)
			{
				path.Add(current);
				current = current.parent;

				GameManager.debugPath[current.gridPosition.x, current.gridPosition.y] = true;
			}
			
			// Simplify
			var wayPoints = new List<Vector3>();
			var directions = new List<Vector2>();
			var oldDirection = Vector2.zero;

			for (int i = 1; i < path.Count; i++)
			{
				var newDirection = new Vector2(path[i - 1].gridPosition.x - path[i].gridPosition.x, path[i - 1].gridPosition.y - path[i].gridPosition.y);
				if (newDirection != oldDirection)
				{
					wayPoints.Add(grid.GridToWorld(path[i - 1].gridPosition));
					directions.Add(oldDirection);
				}
				oldDirection = newDirection;
			}
			
			// Build
			wayPoints.Reverse();
			directions.Reverse();
			return new Path
			{
				points = wayPoints.ToArray(),
				directions = directions.ToArray()
			};
		}
		
		private static int Distance(Node a, Node b)
		{
			int dx = Mathf.Abs(a.gridPosition.x - b.gridPosition.x);
			int dy = Mathf.Abs(a.gridPosition.y - b.gridPosition.y);

			int max = Mathf.Max(dx, dy);
			int min = Mathf.Min(dx, dy);

			return min * diagonalCost + (max - min) * alignedCost;
		}
	}
}