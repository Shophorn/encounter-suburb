using System;
using System.Collections;
using UnityEngine;

using Object = UnityEngine.Object;
using Grid = PathFinding.Grid;
using Random = System.Random;

[Serializable]
public class Level
{
	private struct SpawnWave
	{
		public TankType[] spawnings;
	}

	public Map map;
	private SpawnWave[] spawnWaves;

	private static readonly Vector3 gridOffset = Vector3.one * 0.5f;

	private Vector3[] enemySpawnPoints;

	public Action victoryCallback;
	public Action defeatCallback;
	
	private int enemyKilledCount = 0;
	private int enemySpawnedCount = 0;

	public Grid grid;

	// These need to be destroyed on unload, hence IDisposable
	private GameObject mapObject = null;
	private Mesh mapMesh = null;
	private Texture2D mapTexture = null;
	

	public EnemyTankControllerSystem enemyController;
	
	public Breakable playerBaseBreakable { get; private set; }

	// Pseudo random generator
	private readonly Random random;
	
	public Level(Texture2D mapTexture)
	{
		this.mapTexture = mapTexture;
		map = Map.FromTexture(mapTexture);
		random = new Random(mapTexture.name.GetHashCode());
	}

	
	public IEnumerator Spawn()
	{
		enemyController.OnTankDestroyed += () => enemyKilledCount++;
		var unitDelay = new WaitForSeconds(3);

		for (int w = 0; w < spawnWaves.Length; w++)
		{
			for (int u = 0; u < spawnWaves[w].spawnings.Length; u++)
			{
				int pointIndex = (w + u) % enemySpawnPoints.Length;

				enemyController.Spawn(spawnWaves[w].spawnings[u], enemySpawnPoints[pointIndex]);
				enemySpawnedCount++;
				
				yield return unitDelay;
			}
			
			// Wait for wave to be killed
			while (enemyKilledCount < enemySpawnedCount)
				yield return null;
		}
		
		// This means player has killed everyone
		victoryCallback();
	}

	public void Unload()
	{
		Object.Destroy(mapObject);
		Object.Destroy(mapMesh);
		Object.Destroy(mapTexture);
	}

	public void BuildMap()
	{
		spawnWaves = SpawnWavesFromMapTexture(mapTexture);
		
		for (int i = 0; i < spawnWaves.Length; i++)
		{
			RandomizeArray(spawnWaves[i].spawnings, random);
		}
		
		mapObject = new GameObject("Map", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
		mapObject.layer = LayerMask.NameToLayer("Ground");

		grid = new Grid(map);

		mapMesh = map.BuildMesh();
		mapObject.GetComponent<MeshFilter>().mesh = mapMesh;
		
		var material = new Material(LevelBootstrap.mapMaterial);
		mapObject.GetComponent<MeshRenderer>().material = material;

		mapTexture = MapTextureGenerator.Generate(map, 2048);
		material.mainTexture = mapTexture;

		var collider = mapObject.GetComponent<MeshCollider>();
		collider.inflateMesh = true;
		collider.sharedMesh = mapMesh;

		SpawnBreakables(TileType.WeakWall, LevelBootstrap.brickBlockPrefab);
		SpawnBreakables(TileType.StrongWall, LevelBootstrap.concreteBlockPrefab);

		SpawnProps(TileType.Water, LevelBootstrap.waterPrefab);
		SpawnRandomProps(TileType.Woods, LevelBootstrap.RandomTree);

		enemySpawnPoints = map.EnemySpawnPoints();
		RandomizeArray(enemySpawnPoints, random);
		
		var basePosition = map.BasePosition();
		enemyController.playerBasePosition = basePosition;
		playerBaseBreakable =
			Object.Instantiate(LevelBootstrap.playerBasePrefab, basePosition, Quaternion.identity, mapObject.transform);
		playerBaseBreakable.OnBreak += defeatCallback;

		BuildCollidersOnEdges();

		var backgroundColor = LevelBootstrap.RandomSkyColor(random);
		Camera.main.backgroundColor = backgroundColor;
		RenderSettings.ambientLight = backgroundColor;

	} // build map

	private void BuildCollidersOnEdges()
	{
		var colliders = new GameObject("Colliders");
		colliders.transform.parent = mapObject.transform;
		colliders.layer = LayerMask.NameToLayer("EdgeColliders");
		
		float size = map.size;
		float half = map.size * 0.5f;
		
		// South
		var southCollider = colliders.AddComponent<BoxCollider>();
		southCollider.size = new Vector3(size + 2f, 1f, 1f);
		southCollider.center = new Vector3(half, 0.5f, -0.5f);
		
		// North
		var northCollider = colliders.AddComponent<BoxCollider>();
		northCollider.size = new Vector3(size + 2f, 1f, 1f);
		northCollider.center = new Vector3(half, 0.5f, size + 0.5f);
		
		// East
		var eastCollider = colliders.AddComponent<BoxCollider>();
		eastCollider.size = new Vector3(1f, 1f, size);
		eastCollider.center = new Vector3(-0.5f, 0.5f, half);
		
		// West
		var westCollider = colliders.AddComponent<BoxCollider>();
		westCollider.size = new Vector3(1f, 1f, size);
		westCollider.center = new Vector3(size + 0.5f, 0.5f, half);
	}

	private void SpawnBreakables(TileType type, Breakable prefab)
	{
		var positions = map.GridPositions(type);
		for (int i = 0; i < positions.Length; i++)
		{
			var point = new Vector3(positions[i].x, 0, positions[i].y);
			var block = Object.Instantiate(prefab, point + gridOffset, Quaternion.identity, mapObject.transform);
			
			int x = positions[i].x;
			int y = positions[i].y;
			block.OnBreak += () => grid.OnMapObstacleBreak(x, y);
		}
	}

	private void SpawnProps(TileType type, GameObject prefab)
	{
		var positions = map.TilePositions(type);
		for (int i = 0; i < positions.Length; i++)
		{
			Object.Instantiate(prefab, positions[i] + gridOffset, Quaternion.identity, mapObject.transform);
		}
	}

	private void SpawnRandomProps(TileType type, Func<Random, GameObject> getter)
	{
		var positions = map.TilePositions(type);
		for (int i = 0; i < positions.Length; i++)
		{
			var prefab = getter(random);
			Object.Instantiate(prefab, positions[i] + gridOffset, Quaternion.identity, mapObject.transform);
		}
	}

	private static SpawnWave[] SpawnWavesFromMapTexture(Texture2D texture)
	{
		int width = texture.width;
		int height = texture.height;
		
		int w = width - height;
		var waves = new SpawnWave[w * height];
		int waveCount = 0;

		var pixels = texture.GetPixels32();

		int startX = width - w;
		for (int x = startX; x < width; x++)
		{
			for (int y = height - 1; y >= 0; y--)
			{
				int index = y * width + x;

				if (pixels[index].a != 255) continue;

				int hunterCount = pixels[index].r;
				int pummelCount = pixels[index].g;
				int heavyCount = pixels[index].b;

				int ii = 0;

				var spawnings = new TankType[hunterCount + pummelCount + heavyCount];
				for (int i = 0; i < hunterCount; i++)
				{
					spawnings[i] = TankType.Hunter;
				}

				for (int i = hunterCount; i < hunterCount + pummelCount; i++)
				{
					spawnings[i] = TankType.Pummel;
				}

				for (int i = hunterCount + pummelCount; i < hunterCount + pummelCount + heavyCount; i++)
				{
					spawnings[i] = TankType.Heavy;
				}
				
				waves[waveCount] = new SpawnWave
				{
					spawnings = spawnings
				};
				
				waveCount++;
			}
		}

		if (waveCount == 0)
		{
			return new[]
			{
				new SpawnWave {spawnings = new TankType[] {TankType.Hunter}}
			};
		}
		
		var returnArray = new SpawnWave[waveCount];
		Array.Copy(waves, returnArray, waveCount);
		return returnArray;
	}


	public int[] GetEnemyCounts()
	{
		int hunterCount = 0;
		int pummelCount = 0;
		int heavyCount = 0;

		for (int i = 0; i < spawnWaves.Length; i++)
		{
			for (int j = 0; j < spawnWaves[i].spawnings.Length; j++)
			{
				switch (spawnWaves[i].spawnings[j])
				{
					case TankType.Hunter: hunterCount++; break;
					case TankType.Pummel: pummelCount++; break;
					case TankType.Heavy: heavyCount++; break;
				}
			}
		}

		return new int[3] {hunterCount, pummelCount, heavyCount};
	}

	private static void RandomizeArray<T>(T[] array, Random random)
	{
		int count = array.Length;
		for (int i = 0; i < count - 1; i++)
		{
			int newIndex = (random.Next() % (count - i)) + i;

			T temp = array[i];
			array[i] = array[newIndex];
			array[newIndex] = temp;
		}
	}
}