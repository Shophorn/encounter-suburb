using System;
using System.Collections;
using UnityEngine;

using Object = UnityEngine.Object;
using Grid = PathFinding.Grid;

[Serializable]
public class Level : IDisposable
{
	private struct SpawnWave
	{
		public int hunterCount;
		public int pummelCount;
		public int heavyCount;
	}

	public Map map;
//	private int count;
	private SpawnWave[] waves;
	public Material material;

	private static readonly Vector3 gridOffset = Vector3.one * 0.5f;

	private Vector3[] enemySpawnPoints;

	public event Action OnPlayerDefeat;
	public event Action OnEnemiesDefeat;

	private int enemyKilledCount = 0;
	private int enemySpawnedCount = 0;
	private bool allSpawned;

	public Grid grid;

	// These need to be destroyed on unload
	private GameObject mapObject = null;
	private Mesh mapMesh = null;
	private Texture mapTexture = null;

	public EnemyTankControllerSystem enemyController;

	public Level(Texture2D mapTexture)
	{
		map = Map.FromTexture(mapTexture);
		waves = SpawnWavesFromMapTexture(mapTexture);
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

				waves[waveCount] = new SpawnWave
				{
					hunterCount = pixels[index].r,
					pummelCount = pixels[index].g,
					heavyCount = pixels[index].b
				};
				
				waveCount++;
			}
		}

		if (waveCount == 0)
		{
			return new[] {
				new SpawnWave { hunterCount = 5, pummelCount = 5, heavyCount = 1 }
			};
		}

		var returnArray = new SpawnWave[waveCount];
		Array.Copy(waves, returnArray, waveCount);
		return returnArray;
	}

	public IEnumerator Spawn()
	{
		enemyController.OnTankDestroyed += OnTankDestroyed;

		var unitDelay = new WaitForSeconds(2);
		var waveDelay = new WaitForSeconds(4);

		for (int w = 0; w < waves.Length; w++)
		{
			for (int u = 0; u < waves[w].hunterCount; u++)
			{
				int pointIndex = (w + u) % enemySpawnPoints.Length;
				
				enemyController.Spawn(enemySpawnPoints[pointIndex]);
				enemySpawnedCount++;
				
				if (u < waves[w].hunterCount -1)
					yield return unitDelay;
			}
			
			if (w < waves.Length - 1)
				yield return waveDelay;
		}

		allSpawned = true;
		Debug.Log($"All Spawned {enemySpawnedCount}");
	}

	private void OnTankDestroyed()
	{
		enemyKilledCount++;
		
		Debug.Log($"Enemy Destroyed {enemyKilledCount} / {enemySpawnedCount}");
		
		if (allSpawned && enemySpawnedCount == enemyKilledCount)
		{
			OnEnemiesDefeat();
		}
	}

	public void Dispose()
	{
		Object.Destroy(mapObject);
		Object.Destroy(mapMesh);
		Object.Destroy(mapTexture);

		OnPlayerDefeat = null;
		OnEnemiesDefeat = null;
	}

	public void BuildMap()
	{
		mapObject = new GameObject("Map", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
		mapObject.layer = LayerMask.NameToLayer("Ground");

		grid = new Grid(map);

		mapMesh = map.BuildMesh();
		mapObject.GetComponent<MeshFilter>().mesh = mapMesh;
		mapObject.GetComponent<MeshRenderer>().material = material;

		mapTexture = map.CreateTexture(512);
		material.mainTexture = mapTexture;

		var collider = mapObject.GetComponent<MeshCollider>();
		collider.inflateMesh = true;
		collider.sharedMesh = mapMesh;

		SpawnBreakables(TileType.WeakWall, Bootstrap.brickBlockPrefab);
		SpawnBreakables(TileType.StrongWall, Bootstrap.concreteBlockPrefab);

		SpawnProps(TileType.Water, Bootstrap.waterPrefab);
		SpawnProps(TileType.Woods, Bootstrap.bushPrefab);

		enemySpawnPoints = map.EnemySpawnPoints();

		var basePosition = map.BasePosition();
		enemyController.playerBasePosition = basePosition;
		var playerBase =
			Object.Instantiate(Bootstrap.basePrefab, basePosition, Quaternion.identity, mapObject.transform);
		playerBase.GetComponent<Breakable>().OnBreak += () => OnPlayerDefeat?.Invoke();
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
			block.OnBreak += () => grid.OnBreakableBreak(x, y);
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


}