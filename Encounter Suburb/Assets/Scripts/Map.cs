using System;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
	Ground,			// 100
	WeakWall,		// 110
	StrongWall,		// 010
	Water,			// 011
	Ice,			// 001
	Woods,			// 101
	
	PlayerSpawn,	// 000
	EnemySpawn,		// 111
	BasePosition	// hhh
}

[Serializable]
public class Map
{
	private static readonly Color[] tileColors =
	{
		new Color(0.23f, 0.13f, 0.02f), 
		new Color(0.61f, 0.1f, 0.07f), 
		new Color(0.48f, 0.48f, 0.48f), 
		new Color(0.18f, 0.22f, 1f), 
		new Color(0.86f, 0.93f, 1f), 
		new Color(0.15f, 0.66f, 0.09f), 
		
		Color.black,
		Color.white, 
		Color.grey,
	};
	
	public const float SCALE = 2f;
	
	public int size;
	public TileType [,] tiles;

	public static Map MockMap(int size = 32)
	{
		Map map = new Map {size = size, tiles = new TileType[size, size]};

		int y = size / 2;
		for (int x = 0; x < size; x++)
		{
			map.tiles[x, y - 3] = TileType.Woods;
			map.tiles[x, y - 2] = TileType.Woods;
			map.tiles[x, y - 1] = TileType.WeakWall;
			map.tiles[x, y] = TileType.WeakWall;
			map.tiles[x, y + 1] = TileType.StrongWall;
			map.tiles[x, y + 2] = TileType.StrongWall;
		}

		map.tiles[3, 3] = TileType.Water;
		map.tiles[3, 4] = TileType.Water;
		map.tiles[4, 3] = TileType.Water;
		map.tiles[4, 4] = TileType.Water;
		map.tiles[size - 5, 3] = TileType.Water;
		map.tiles[size - 4, 3] = TileType.Water;
		map.tiles[size - 5, 4] = TileType.Water;
		map.tiles[size - 4, 4] = TileType.Water;
		map.tiles[3, size - 5] = TileType.Water;
		map.tiles[3, size - 4] = TileType.Water;
		map.tiles[4, size - 4] = TileType.Water;
		map.tiles[4, size - 5] = TileType.Water;
		map.tiles[size - 5, size - 5] = TileType.Water;
		map.tiles[size - 4, size - 5] = TileType.Water;
		map.tiles[size - 5, size - 4] = TileType.Water;
		map.tiles[size - 4, size - 4] = TileType.Water;

		map.tiles[0, size - 2] = TileType.EnemySpawn;
		map.tiles[size -2, size - 2] = TileType.EnemySpawn;

		map.tiles[size / 2 - 1, size / 4 - 1] = TileType.PlayerSpawn;

		for (int yy = 0; yy < 4; yy++)
		{
			for (int xx = 0; xx < 4; xx++)
			{
				map.tiles[xx + size / 2 - 2, yy] = TileType.StrongWall;
			}
		}

		map.tiles[size / 2 - 1, 1] = TileType.BasePosition;
		map.tiles[size / 2, 1] = TileType.BasePosition;
		map.tiles[size / 2 - 1, 2] = TileType.BasePosition;
		map.tiles[size / 2, 2] = TileType.BasePosition;
		
		return map;
	}

	public Mesh BuildMesh()
	{
		int vertexCount = (size + 1) * (size + 1);
		var vertices = new Vector3[vertexCount];
		var uvs = new Vector2[vertexCount];
		
		for (int v = 0, y = 0; y < size + 1; y++)
		{
			for (int x = 0; x < size + 1; x++, v++)
			{
				vertices[v] = new Vector3(SCALE * x, 0, SCALE * y);
				uvs [v] = new Vector2((float)x/size, (float)y / size);
			}
		}

		int triangleCount = size * size * 6;
		var triangles = new int[triangleCount];

		for (int t = 0, y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++, t += 6)
			{
				int v = y * (size + 1) + x;

				triangles[t] = v;
				triangles[t + 1] = triangles[t + 4] = v + size + 1;
				triangles[t + 2] = triangles[t + 3] = v + 1;
				triangles[t + 5] = v + size + 2;
			}
		}

		Mesh mesh = new Mesh
		{
			vertices = vertices,
			triangles = triangles,
			uv = uvs
		};
		mesh.RecalculateNormals();

		return mesh;
	}


	public Texture2D CreateTexture(int resolution)
	{
		var pixels = new Color32[resolution * resolution];
		for (int p = 0, y = 0; y < resolution; y++)
		{
			int yy = (int) (((float) y / resolution) * size);
			
			for (int x = 0; x < resolution; x++, p++)
			{
				int xx = (int) (((float) x / resolution) * size);

				pixels[p] = tileColors[(int) tiles[xx, yy]];
			}
		}
		
		var texture = new Texture2D(resolution, resolution);
		texture.SetPixels32(pixels);
		texture.filterMode = FilterMode.Point;
		texture.Apply();
		return texture;
	}

	public Vector2Int[] GridPositions(TileType type)
	{
		var points = new List<Vector2Int>();
		
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				if (tiles[x, y] == type)
				{
					points.Add(new Vector2Int(x, y));
				}
			}
		}

		return points.ToArray();
	}
	
	public Vector3[] TilePositions(TileType type)
	{
		var points = new List<Vector3>();

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				if (tiles[x, y] == type)
				{
					points.Add(new Vector3(x * SCALE, 0, y * SCALE));
				}
			}
		}

		return points.ToArray();
	}

	public Vector3[] EnemySpawnPoints()
	{
		var points = new List<Vector3>();

		for (int y = 0; y < size; y += 2)
		{
			for (int x = 0; x < size; x += 2)
			{
				if (tiles[x, y] == TileType.EnemySpawn)
				{
					points.Add(new Vector3((x + 1) * SCALE, 0, (y + 1) * SCALE));
				}
			}
		}

		return points.ToArray();
	}

	public Vector3 PlayerSpawnPoint()
	{
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				if (tiles[x, y] == TileType.PlayerSpawn)
				{
					return new Vector3(x + 1, 0, y + 1) * SCALE;
				}
			}
		}
		return new Vector3(size / 2, 0, size / 2) * SCALE;
	}

	public Vector3 BasePosition()
	{
		var baseTilePositions = new List<Vector3>();

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				if (tiles[x, y] == TileType.BasePosition)
				{
					baseTilePositions.Add(new Vector3(x, 0, y) * SCALE);
				}
			}
		}

		if (baseTilePositions.Count == 0)
		{
			return Vector3.one * -1;
		}

		var point = Vector3.zero;
		for (int i = 0; i < baseTilePositions.Count; i++)
		{
			point += baseTilePositions[i];
		}

		point /= baseTilePositions.Count;
		point += new Vector3(SCALE * 0.5f, 0, SCALE * 0.5f);

		return point;
	}
}