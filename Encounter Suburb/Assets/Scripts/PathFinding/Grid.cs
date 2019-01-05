using Boo.Lang;
using UnityEngine;

namespace PathFinding
{
	public class Grid
	{
		public int size;
		public float scale;
		private float halfScale;
		
		public Node[,] nodes;

		private const int resolutionMultiplier = 3;
		
		public Grid(Map map)
		{
			size = map.size * resolutionMultiplier;
			scale = Map.SCALE / resolutionMultiplier;
			halfScale = scale * 0.5f;
			nodes = new Node[size, size];

			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					nodes[x, y] = new Node
					{
						type = Node.FromMapTile(map.tiles[x / resolutionMultiplier, y / resolutionMultiplier]),
						gridPosition = new Vector2Int(x, y)
					};
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
		
		public Vector2Int NodeIndexFromWorldPoint(Vector3 worldPoint)
		{
			int x = (int) (worldPoint.x / scale);
			int y = (int) (worldPoint.z / scale);
			
			x = Mathf.Clamp(x, 0, size - 1);
			y = Mathf.Clamp(y, 0, size - 1);
			
			return new Vector2Int(x, y);
		}

		public Node NodeFromWorldPoint(Vector3 worldPoint)
		{
			int x = (int) (worldPoint.x / scale);
			int y = (int) (worldPoint.z / scale);

			x = Mathf.Clamp(x, 0, size - 1);
			y = Mathf.Clamp(y, 0, size - 1);
			
			return nodes[x, y];
		}
		
		
		public void OnBreakableBreak(int x, int y)
		{
			nodes[x, y].type = NodeType.Open;
		}

		public Vector3 NodeWorldPosition(Vector2Int point)
		{
			return new Vector3(point.x * scale + halfScale, 0f, point.y * scale + halfScale);
		}
	}
}