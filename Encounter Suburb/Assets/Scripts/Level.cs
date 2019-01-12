using System;
using System.Collections;
using UnityEngine;

using Object = UnityEngine.Object;
using Grid = PathFinding.Grid;
using Random = System.Random;

[Serializable]
public class Level : IDisposable
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
	private bool allSpawned;

	public Grid grid;

	// These need to be destroyed on unload
	private GameObject mapObject = null;
	private Mesh mapMesh = null;
	private Texture2D mapTexture = null;

	public EnemyTankControllerSystem enemyController;
	
	public Breakable playerBaseBreakable { get; private set; }

	// Pseudo random generator
	private readonly Random random;
	private float randomFloat => (float) random.NextDouble();
	
	public Level(Texture2D mapTexture)
	{
		this.mapTexture = mapTexture;
		map = Map.FromTexture(mapTexture);
		random = new Random(mapTexture.name.GetHashCode());
	}

	public IEnumerator Spawn()
	{
		enemyController.OnTankDestroyed += OnTankDestroyed;

		var unitDelay = new WaitForSeconds(3);
		var waveDelay = new WaitForSeconds(5);

		for (int w = 0; w < spawnWaves.Length; w++)
		{
			for (int u = 0; u < spawnWaves[w].spawnings.Length; u++)
			{
				int pointIndex = (w + u) % enemySpawnPoints.Length;

				enemyController.Spawn(spawnWaves[w].spawnings[u], enemySpawnPoints[pointIndex]);
				enemySpawnedCount++;
				
				Debug.Log("Spawned Unit");
				
				if (u < spawnWaves[w].spawnings.Length -1)
					yield return unitDelay;
			}
			
			Debug.Log("Spawned Wave");
			
			if (w >= spawnWaves.Length - 1)
				yield return waveDelay;
		}

		allSpawned = true;
	}

	private void OnTankDestroyed()
	{
		enemyKilledCount++;
		
		if (allSpawned && enemySpawnedCount == enemyKilledCount)
		{
			victoryCallback();
		}
	}

	public void Dispose()
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
		SpawnProps(TileType.Woods, LevelBootstrap.bushPrefab);

		enemySpawnPoints = map.EnemySpawnPoints();
		RandomizeArray(enemySpawnPoints, random);
		
		var basePosition = map.BasePosition();
		enemyController.playerBasePosition = basePosition;
		playerBaseBreakable =
			Object.Instantiate(LevelBootstrap.playerBasePrefab, basePosition, Quaternion.identity, mapObject.transform);
		playerBaseBreakable.OnBreak += defeatCallback;

		BuildCollidersOnEdges();
	}

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
			block.GetComponent<ColourVariator>()?.Apply(randomFloat);
			
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
			var obj = Object.Instantiate(prefab, positions[i] + gridOffset, Quaternion.identity, mapObject.transform);
			obj.GetComponent<ColourVariator>()?.Apply(randomFloat);
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