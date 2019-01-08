using System;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
	Ground,			// 110
	Woods,			// 010
	Ice,			// 011
	Water,			// 001
	WeakWall,		// 101
	StrongWall,		// 100
	
	PlayerSpawn,	// 000
	EnemySpawn,		// 111
	PlayerBase	// hhh
}

[Serializable]
public class Map
{
	private static readonly Color[] tileTextureColors =
	{
		new Color(0.23f, 0.13f, 0.02f), 
		new Color(0.15f, 0.66f, 0.09f), 
		new Color(0.86f, 0.93f, 1f), 
		new Color(0.18f, 0.22f, 1f), 
		new Color(0.61f, 0.1f, 0.07f), 
		new Color(0.48f, 0.48f, 0.48f), 
		
		Color.black,
		Color.white, 
		Color.grey,
	};

	private static Dictionary<Color32, TileType> imageToTileMap = new Dictionary<Color32, TileType> 
	{
		{ new Color32(255, 255, 0, 255), 	TileType.Ground 	 },
		{ new Color32(0, 255, 0, 255), 		TileType.Woods		 },
		{ new Color32(0, 255, 255, 255), 	TileType.Ice 		 },
		{ new Color32(0, 0, 255, 255), 		TileType.Water		 },
		{ new Color32(255, 0, 255, 255), 	TileType.WeakWall    },
		{ new Color32(255, 0, 0, 255), 		TileType.StrongWall  },
		
		{ new Color32(0, 0, 0, 255),		TileType.PlayerSpawn },
		{ new Color32(255, 255, 255, 255),	TileType.EnemySpawn  },
		{ new Color32(128, 128, 128, 255),	TileType.PlayerBase  }
	};
	
	public int size;
	public TileType [,] tiles;

	public static Map FromTexture(Texture2D texture)
	{
		int width = texture.width;
		int height = texture.height;
		int size = Mathf.Min(width, height);
		var tiles = new TileType[size, size];

		var pixels = texture.GetPixels32();
		
		for (int h = 0; h < size; h++)
		{
			for (int w = 0; w < size; w++)
			{
				int p = h * width + w;
				tiles[w, h] = imageToTileMap[pixels[p]];
			}
		}

		return new Map
		{
			size = size,
			tiles = tiles
		};
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
				vertices[v] = new Vector3(x, 0, y);
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

				var tile = tiles[xx, yy];
				if (tile == TileType.EnemySpawn || tile == TileType.PlayerSpawn)
				{
					tile = TileType.Ground;
				}
				
				pixels[p] = tileTextureColors[(int)tile];
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
					points.Add(new Vector3(x, 0, y));
				}
			}
		}

		return points.ToArray();
	}

	public Vector3[] EnemySpawnPoints()
	{
		var points = new List<Vector3>();

		for (int y = 0; y < size - 1; y++)
		{
			for (int x = 0; x < size - 1; x++)
			{
				if (tiles[x, y] == TileType.EnemySpawn && tiles[x + 1, y + 1] == TileType.EnemySpawn)
				{
					points.Add(new Vector3(x + 1, 0, y + 1));
				}
			}
		}

		return points.ToArray();
	}

	public Vector3 PlayerSpawnPoint()
	{
		for (int y = 0; y < size - 1; y++)
		{
			for (int x = 0; x < size - 1; x++)
			{
				if (tiles[x, y] == TileType.PlayerSpawn)
				{
					return new Vector3(x + 1, 0, y + 1);
				}
			}
		}
		return new Vector3(size / 2, 0, size / 2);
	}

	
	
	public Vector3 BasePosition()
	{
		var baseTilePositions = new List<Vector3>();

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				if (tiles[x, y] == TileType.PlayerBase)
				{
					baseTilePositions.Add(new Vector3(x, 0, y));
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
		point += new Vector3(0.5f, 0, 0.5f);

		return point;
	}
}