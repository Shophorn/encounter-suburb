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
		
		public Grid(Map map)
		{
			size = map.size * resolution;
			nodes = new Node[size, size];

			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					// Add variables, use constructor
					nodes[x, y] = new Node
					(
						x, y,
						map.tiles[x / resolution, y / resolution]
					);
				}
			}
			
//			for (int y = 0; y < size; y++)
//			{
//				for (int x = 0; x < size; x++)
//				{
//					var a = nodes[x, y].preferBreakWallsPenaltyRaw;
//					var b = nodes[x, y].preferDriveAroundPenaltyRaw;
//					
//					Debug.Log($"{a} : {b}");
//				}
//			}
			
			BlurNodeWeights();
		}

		private void BlurNodeWeights()
		{
			Debug.Log("Blur nodes");
			
			const int kernelExtents = 3;
			const int kernelSize = 1 + 2 * kernelExtents;
			const int kernelArea = kernelSize * kernelSize;
			
			// Horizontal Pass
			var horizontalBlurPass = new Vector2Int[size, size];
			for (int y = 0; y < size; y++)
			{
				for (int x = -kernelExtents; x <= kernelExtents; x++)
				{
					int xx = Mathf.Clamp(x, 0, size - 1);
					horizontalBlurPass[0, y].x += nodes[xx, y].preferBreakWallsPenaltyRaw;
					horizontalBlurPass[0, y].y += nodes[xx, y].preferDriveAroundPenaltyRaw;
				}
				
				for (int x = 1; x < size; x++)
				{
					Node remove = nodes[Mathf.Clamp(x - kernelExtents, 0, size - 1), y];
					Node add = nodes[Mathf.Clamp(x + kernelExtents, 0, size - 1), y];
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
					for (int yy = y - kernelExtents; yy <= y + kernelExtents; yy++)
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
				bool yInRange = 0 <= y && y < size;
				if (!yInRange) continue;
				
				for (int x = point.x - 1; x <= point.x + 1; x++)
				{
					bool xInRange = 0 <= x && x < size;
					if (!xInRange) continue;
					
					if (x == point.x && y == point.y) continue;

					neighbours.Add(nodes[x, y]);
				}
			}
			
			return neighbours.ToArray();
		}
		
//		public Vector2Int NodeIndexFromWorldPoint(Vector3 worldPoint)
//		{
//			int x = (int) (worldPoint.x);
//			int y = (int) (worldPoint.z);
//			
//			x = Mathf.Clamp(x, 0, size - 1);
//			y = Mathf.Clamp(y, 0, size - 1);
//			
//			return new Vector2Int(x, y);
//		}

		public Node NodeFromWorldPoint(Vector3 worldPoint)
		{
			worldPoint *= 3;
			
			int x = (int) (worldPoint.x);
			int y = (int) (worldPoint.z);
			
			x = Mathf.Clamp(x, 0, size - 1);
			y = Mathf.Clamp(y, 0, size - 1);
			
			return nodes[x, y];
		}
		
		
		public void OnBreakableBreak(int x, int y)
		{
			x *= resolution;
			y *= resolution;

			for (int j = 0; j < resolution; j++)
			{
				for (int i = 0; i < resolution; i++)
				{
					nodes[x + i, y + j] = new Node(x + i, y + j, TileType.Ground);
				}
			}
			BlurNodeWeights();
		}

		public Vector3 NodeWorldPosition(Vector2Int point)
		{
			return NodeWorldPosition(point.x, point.y);
		}

		public Vector3 NodeWorldPosition(int x, int y)
		{
			return new Vector3(x + 0.5f, 0f, y + 0.5f) / resolution;
		}
	}
}