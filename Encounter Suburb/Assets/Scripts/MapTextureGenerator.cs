using System;
using System.Collections.Generic;
using UnityEngine;

public static class MapTextureGenerator
{
	public static Texture2D Generate(Map map, int resolution)
	{
		float borderOffset = 0f;
		
		var constructionTilePositions = new List<Vector2Int>();
		var forestTilePositions = new List<Vector2Int>();
		var waterTilePositions = new List<Vector2Int>();

		
		for (int y = 0; y < map.size; y++)
		{
			for (int x = 0; x < map.size; x++)
			{
				TileType t = map.tiles[x, y];
				bool construction = t == TileType.WeakWall || t == TileType.StrongWall || t == TileType.PlayerBase;
				if (construction)
				{
					constructionTilePositions.Add(new Vector2Int(x, y));
				}
				else if (t == TileType.Woods)
				{
					forestTilePositions.Add(new Vector2Int(x, y));
				}
				else if (t == TileType.Water)
				{
					waterTilePositions.Add(new Vector2Int(x, y));
				}
			}
		}
		
		float textureScale = map.size / 3f;
		int pixelCount = resolution * resolution;

		// Allocate once
		var image = new Color32[pixelCount];
		var layer = new Color32[pixelCount];
		var mask = new float[pixelCount];

		// Base layer
		GenerateLayer(image, resolution, LevelBootstrap.groundTexture, textureScale);
		
		// Contruction tiles
		GenerateLayer(layer, resolution, LevelBootstrap.constructionTexture, textureScale);
		MaskFromPositions(mask, resolution, constructionTilePositions, map.size);
		ApplyLayerWithMask(image, layer, mask, pixelCount);
		Array.Clear(mask, 0, pixelCount);
		
		// Forest tiles
		GenerateLayer(layer, resolution, LevelBootstrap.woodsTexture, textureScale);
		MaskFromPositions(mask, resolution, forestTilePositions, map.size);
		ApplyLayerWithMask(image, layer, mask, pixelCount);
		Array.Clear(mask, 0, pixelCount);

		// Water tiles
//		GenerateLayer(layer, resolution, LevelBootstrap.waterTexture, textureScale);
		MaskFromPositions(mask, resolution, waterTilePositions, map.size);
//		ApplyLayerWithMask(image, layer, mask, pixelCount);
		ApplyMaskOnAlphaChannel(image, mask, pixelCount);
		
		var texture = new Texture2D(resolution, resolution);
		texture.SetPixels32(image);
		texture.Apply();

		return texture;
	}

	private static void ApplyMaskOnAlphaChannel(Color32[] image, float[] layer, int pixelCount)
	{
		for (int i = 0; i < pixelCount; i++)
		{
			image[i].a = (byte)(layer[i] * 255);
		}
	}

	private static void ApplyLayerWithMask(Color32[] first, Color32[] second, float[] mask, int count)
	{
		for (int i = 0; i < count; i++)
		{
			first[i] = Color32.Lerp(first[i], second[i], mask[i]);
		}
	}
	
	private static void GenerateLayer(Color32[] target, int resolution, Texture2D source, float scale)
	{
		var sourcePixels = source.GetPixels32();
		var sourceWidth = source.width;
		var sourceHeight = source.height;

		float targetToSourceY = scale * sourceHeight / resolution;
		float targetToSourceX = scale * sourceWidth / resolution;
		
		for (int y = 0, p = 0; y < resolution; y++)
		{
			int yy = (int)(targetToSourceY * y) % sourceHeight;
			
			for (int x = 0; x < resolution; x++, p++)
			{
				int xx = (int) (targetToSourceX * x) % sourceWidth;
				target[p] = sourcePixels[yy * sourceWidth + xx];
			}
		}
	}
	
	private static void MaskFromPositions(float[] mask, int resolution, List<Vector2Int> positions, float size)
	{
		for (int i = 0; i < positions.Count; i++)
		{
			int x = positions[i].x;
			int y = positions[i].y;
			
			// Make area slightly bigger so tiles merge into each other
			Vector2 uvMin = new Vector2(x - 0.1f, y - 0.1f) / size;
			Vector2 uvMax = new Vector2(x + 1.1f, y + 1.1f) / size;
				
			Vector2Int pxMin = Vector2Int.FloorToInt(uvMin * resolution);
			Vector2Int pxDelta = Vector2Int.FloorToInt((uvMax - uvMin) * resolution);

			for (int yy = 0; yy < pxDelta.y; yy++)
			{
				for (int xx = 0; xx < pxDelta.x; xx++)
				{
					int pxU = pxMin.x + xx;
					int pxV = pxMin.y + yy;

					// Skip if out of bounds
					if (pxU < 0 || pxU >= resolution || pxV < 0 || pxV >= resolution) continue;;
					
					int p = pxV * resolution + pxU;

					float u = (float) xx / pxDelta.x;
					float v = (float) yy / pxDelta.y;
						
					mask[p] += LevelBootstrap.tileMask.GetPixelBilinear(u, v).a;
					mask[p] = Mathf.Clamp01(mask[p]);
				}
			}
		}
	}
}