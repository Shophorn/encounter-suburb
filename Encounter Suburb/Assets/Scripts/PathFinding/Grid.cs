using System;
using System.Collections.Generic;
using UnityEngine;

namespace PathFinding
{
	public class Grid
	{
		public readonly int size;
		
		public readonly Node[,] nodes;
	
		// public only for GameManager Gizmos
		public const int resolution = 3;
		private const int blurKernelExtents = 5;
		private const int edgeNodesNumber = 8;

		
		public Grid(Map map)
		{
//			size = map.size * resolution;
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
			return;
			
			const int kernelSize = 1 + 2 * blurKernelExtents;
			const int kernelArea = kernelSize * kernelSize;
			
			// Horizontal Pass
			var horizontalBlurPass = new Vector2Int[size, size];
			for (int y = 0; y < size; y++)
			{
				for (int x = -blurKernelExtents; x <= blurKernelExtents; x++)
				{
					int xx = Mathf.Clamp(x, 0, size - 1);
					horizontalBlurPass[0, y].x += nodes[xx, y].preferBreakWallsPenaltyRaw;
					horizontalBlurPass[0, y].y += nodes[xx, y].preferDriveAroundPenaltyRaw;
				}
				
				for (int x = 1; x < size; x++)
				{
					Node remove = nodes[Mathf.Clamp(x - blurKernelExtents, 0, size - 1), y];
					Node add = nodes[Mathf.Clamp(x + blurKernelExtents, 0, size - 1), y];
					var previous = horizontalBlurPass[x - 1, y];
					
					horizontalBlurPass[x, y].x = previous.x - remove.preferBreakWallsPenaltyRaw + add.preferBreakWallsPenaltyRaw;
					horizontalBlurPass[x, y].y = previous.y - remove.preferDriveAroundPenaltyRaw + add.preferDriveAroundPenaltyRaw;
				}
			}

			// Vertical Pass
			var verticalBlurPass = new Vector2Int[size, size];
			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					for (int yy = y - blurKernelExtents; yy <= y + blurKernelExtents; yy++)
					{
						int yyy = Mathf.Clamp(yy, 0, size - 1);
						verticalBlurPass[x, y] += horizontalBlurPass[x, yyy];
					}
				}
			}

			// Write Results
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					// Impassable can stay where they are
					if (nodes[x, y].type == NodeType.Impassable) continue;
					
					nodes[x, y].preferBreakWallsPenalty = Mathf.RoundToInt((float)verticalBlurPass[x, y].x / kernelArea);
					nodes[x, y].preferDriveAroundPenalty = Mathf.RoundToInt((float)verticalBlurPass[x, y].y / kernelArea);
				}
			}
		}

		public Node[] GetNodeNeighbours(Node node)
		{
			var point = node.gridPosition;
			var neighbours = new List<Node>();

			for (int y = point.y - 1; y <= point.y + 1; y++)
			{
//				bool yInRange = 0 <= y && y < size;
//				if (!yInRange) continue;
				
				for (int x = point.x - 1; x <= point.x + 1; x++)
				{
//					bool xInRange = 0 <= x && x < size;
//					if (!xInRange) continue;
//					
					if (x == point.x && y == point.y) continue;

					neighbours.Add(nodes[x, y]);
				}
			}
			
			return neighbours.ToArray();
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