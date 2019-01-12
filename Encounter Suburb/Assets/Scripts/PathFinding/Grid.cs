using System.Runtime.Remoting.Messaging;
using UnityEngine;

namespace PathFinding
{
	public class Grid
	{
		public readonly int size;
		
		public readonly Node[,] nodes;
	
		// public only for GameManager Gizmos
		public const int resolution = 4;
		private const int blurKernelExtents = 4;
		private const int edgeNodesNumber = 1;

		
		public Grid(Map map)
		{
			// Add nodes to edges to prevent navigation there, these will be of NodeType.Impassable
			size = map.size * resolution + 2 * edgeNodesNumber;
			nodes = new Node[size, size];

			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					int mapX = Mathf.FloorToInt((x - edgeNodesNumber) / (float)resolution);
					int mapY = Mathf.FloorToInt((y - edgeNodesNumber) / (float)resolution);

					bool outOfMap = mapX < 0 || mapX >= map.size || mapY < 0 || mapY >= map.size;

					int breakWallsPenalty;
					int driveAroundPenalty;
					NodeType type;

					if (outOfMap)
					{
						breakWallsPenalty = Node.maxMovePenalty;
						driveAroundPenalty = Node.maxMovePenalty;
						type = NodeType.Impassable;
					}
					else
					{
						TileType tile = map.tiles[mapX, mapY];
						breakWallsPenalty = Node.GetPenalty(tile, true);
						driveAroundPenalty = Node.GetPenalty(tile, false);
						type = Node.TypeFromMapTile(tile);
					}

					nodes[x, y] = new Node(x, y, type, breakWallsPenalty, driveAroundPenalty);
				}
			}
			BlurNodeWeights();
		}
		
		private void BlurNodeWeights()
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();
			
			const int kernelSize = 1 + 2 * blurKernelExtents;
			const int kernelArea = kernelSize * kernelSize;

			// Horizontal pass
			var breakWallsHorizontalPass = new int [size * size];
			var driveAroundHorizontalPass = new int[size * size];
			for (int y = 0; y < size; y++)
			{
				// First column, do full kernel
				for (int n = -blurKernelExtents; n <= blurKernelExtents; n++)
				{
					int xx = Mathf.Clamp(n, 0, size - 1);
					breakWallsHorizontalPass[y * size] += nodes[xx, y].preferBreakWallsPenaltyRaw;
					driveAroundHorizontalPass[y * size] += nodes[xx, y].preferDriveAroundPenaltyRaw;
				}
				
				// Other columns, just remove last, and add next
				for (int x = 1; x < size; x++)
				{
					// Node indices
					int removeIndex = Mathf.Max(x - blurKernelExtents - 1, 0);
					int addIndex = Mathf.Min(x + blurKernelExtents, size - 1);

					Node remove = nodes[removeIndex, y];
					Node add = nodes[addIndex, y];
					
					// Pass indices
					int currentIndex = x + y * size;
					int lastIndex = x - 1 + y * size;
											
					breakWallsHorizontalPass [currentIndex] =
						breakWallsHorizontalPass [lastIndex]
						- remove.preferBreakWallsPenaltyRaw
						+ add.preferBreakWallsPenaltyRaw;
							
					driveAroundHorizontalPass [currentIndex] =
						driveAroundHorizontalPass [lastIndex]
						- remove.preferDriveAroundPenaltyRaw
						+ add.preferDriveAroundPenaltyRaw;
				}
			}

			// Vertical pass
			var breakWallsVerticalPass = new float [size * size];
			var driveAroundVerticalPass = new float [size * size];
			for (int x = 0; x < size; x++)
			{
				// First row, full kernel
				for (int n = -blurKernelExtents; n <= blurKernelExtents; n++)
				{
					int yy = Mathf.Clamp(n, 0, size - 1);
					breakWallsVerticalPass[x] += breakWallsHorizontalPass[x + yy * size];
					driveAroundVerticalPass[x] += driveAroundHorizontalPass[x + yy * size];
				}
				
				// Other rows, subtract last, add next
				for (int y = 1; y < size; y++)
				{
					// All pass indices
					int currentIndex = x + size * y;
					int lastIndex = x + size * (y - 1); 
					int removeIndex =  x + size * Mathf.Max(y - blurKernelExtents - 1, 0);
					int addIndex = x + size * Mathf.Min(y + blurKernelExtents, size - 1);
					
					breakWallsVerticalPass [currentIndex] =
						breakWallsVerticalPass [lastIndex]
						- breakWallsHorizontalPass [removeIndex]
						+ breakWallsHorizontalPass [addIndex];

					driveAroundVerticalPass	[currentIndex] =
						driveAroundVerticalPass	[lastIndex]
						- driveAroundHorizontalPass	[removeIndex]
						+ driveAroundHorizontalPass	[addIndex];
				}
			}

			// Write results
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					if (nodes[x, y].type == NodeType.Impassable) continue;
					nodes[x, y].preferBreakWallsPenalty = Mathf.RoundToInt(breakWallsVerticalPass[x + y * size] / kernelArea);
					nodes[x, y].preferDriveAroundPenalty = Mathf.RoundToInt(driveAroundVerticalPass[x + y * size] / kernelArea);
				}
			}
		
			Debug.Log($"Blurring path took {sw.ElapsedTicks} ticks, {sw.ElapsedMilliseconds} ms");
		}
/*
		private void BlurNodeWeights()
		{
			BetterBlurNodeWeights();
			return;
			
			var sw = System.Diagnostics.Stopwatch.StartNew();
			
			const int kernelSize = 1 + 2 * blurKernelExtents;
			const int kernelArea = kernelSize * kernelSize;


			// Horizontal pass
			var breakWallsHorizontalPass = new int [size, size];
			var driveAroundHorizontalPass = new int[size, size];
			for (int y = 0; y < size; y++)
			{
				// First column, do full kernel
				for (int n = -blurKernelExtents; n <= blurKernelExtents; n++)
				{
					int xx = Mathf.Clamp(n, 0, size - 1);
					breakWallsHorizontalPass[0, y] += nodes[xx, y].preferBreakWallsPenaltyRaw;
					driveAroundHorizontalPass[0, y] += nodes[xx, y].preferDriveAroundPenaltyRaw;
				}
				
				// Other columns, just remove last, and add next
				for (int x = 1; x < size; x++)
				{
					int removeIndex = Mathf.Max(x - blurKernelExtents - 1, 0);
					int addIndex = Mathf.Min(x + blurKernelExtents, size - 1);

					Node remove = nodes[removeIndex, y];
					Node add = nodes[addIndex, y];
					
					breakWallsHorizontalPass[x, y] =
						breakWallsHorizontalPass[x - 1, y]
						- remove.preferBreakWallsPenaltyRaw
						+ add.preferBreakWallsPenaltyRaw;
							
					driveAroundHorizontalPass[x, y] =
						driveAroundHorizontalPass[x - 1, y]
						- remove.preferDriveAroundPenaltyRaw
						+ add.preferDriveAroundPenaltyRaw;
				}
			}

			// Vertical pass
			var breakWallsVerticalPass = new float [size, size];
			var driveAroundVerticalPass = new float [size, size];
			for (int x = 0; x < size; x++)
			{
				// First row, full kernel
				for (int n = -blurKernelExtents; n <= blurKernelExtents; n++)
				{
					int yy = Mathf.Clamp(n, 0, size - 1);
					breakWallsVerticalPass[x, 0] += breakWallsHorizontalPass[x, yy];
					driveAroundVerticalPass[x, 0] += driveAroundHorizontalPass[x, yy];
				}
				
				// Other rows, subtract last, add next
				for (int y = 1; y < size; y++)
				{
					int removeIndex = Mathf.Max(y - blurKernelExtents - 1, 0);
					int addIndex = Mathf.Min(y + blurKernelExtents, size - 1);
					
					breakWallsVerticalPass[x, y] =
						breakWallsVerticalPass[x, y - 1]
						- breakWallsHorizontalPass[x, removeIndex]
						+ breakWallsHorizontalPass[x, addIndex];

					driveAroundVerticalPass[x, y] =
						driveAroundVerticalPass[x, y - 1]
						- driveAroundHorizontalPass[x, removeIndex]
						+ driveAroundHorizontalPass[x, addIndex];
				}
			}

			// Write results
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					if (nodes[x, y].type == NodeType.Impassable) continue;
					nodes[x, y].preferBreakWallsPenalty = Mathf.RoundToInt(breakWallsVerticalPass[x, y] / kernelArea);
					nodes[x, y].preferDriveAroundPenalty = Mathf.RoundToInt(driveAroundVerticalPass[x, y] / kernelArea);
				}
			}
		
			Debug.Log($"Blurring path took {sw.ElapsedTicks} ticks, {sw.ElapsedMilliseconds} ms");
		}
*/
		public int GetNodeNeighboursNonAlloc(Node node, Node[] neighbours)
		{
			int x = node.gridPosition.x;
			int y = node.gridPosition.y;
			
			// Add all nodes in plus shape
			neighbours[0] = nodes[x - 1, y];
			neighbours[1] = nodes[x + 1, y];
			neighbours[2] = nodes[x, y - 1];
			neighbours[3] = nodes[x, y + 1];
			
			//Add diagonals only if there is also non-diagonal route available
			bool
				west = neighbours[0].type != NodeType.Impassable,
				east = neighbours[1].type != NodeType.Impassable,
				south = neighbours[2].type != NodeType.Impassable,
				north = neighbours[3].type != NodeType.Impassable;

			int count = 4;
			if (west || south) neighbours[ count++ ] = nodes[x - 1, y - 1];
			if (west || north) neighbours[ count++ ] = nodes[x - 1, y + 1];
			if (east || north) neighbours[ count++ ] = nodes[x + 1, y + 1];
			if (east || south) neighbours[ count++ ] = nodes[x + 1, y - 1];

			return count;
		}

		private int WorldToGrid(float point)
		{
			return Mathf.FloorToInt(point * resolution) + edgeNodesNumber;
		}

		public Node NodeFromWorldPoint(Vector3 worldPoint)
		{
			int x = WorldToGrid(worldPoint.x);
			int y = WorldToGrid(worldPoint.z);
			
			x = Mathf.Clamp(x, 0, size - 1);
			y = Mathf.Clamp(y, 0, size - 1);
			
			return nodes[x, y];
		}
		
		
		public void OnMapObstacleBreak(int mapX, int mapY)
		{
			int startX = WorldToGrid(mapX);
			int startY = WorldToGrid(mapY);

			int endX = startX + resolution;
			int endY = startY + resolution;
			
			NodeType type = Node.TypeFromMapTile(TileType.Ground);
			int breakWallsPenalty = Node.GetPenalty(TileType.Ground, true);
			int driveAroundPenalty = Node.GetPenalty(TileType.Ground, false);

			for (int y = startY; y < endY; y++)
			{
				for (int x = startX; x < endX; x++)
				{
					nodes[x, y] = new Node(x, y, type, breakWallsPenalty, driveAroundPenalty);
				}
			}
			BlurNodeWeights();
		}

		public Vector3 GridToWorld(Vector2Int point)
		{
			return GridToWorld(point.x, point.y);
		}

		public Vector3 GridToWorld(int x, int y)
		{
			// Add edge nodes number to account edges, and add half to move to centre of node
			return new Vector3(
				x: (x - edgeNodesNumber + 0.5f) / resolution,
				y: 0f,
				z: (y - edgeNodesNumber + 0.5f) / resolution
			);
		}
	}
}