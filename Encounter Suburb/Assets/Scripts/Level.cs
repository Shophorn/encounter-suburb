using System;
using System.Collections;
using UnityEngine;

using Object = UnityEngine.Object;
using Grid = PathFinding.Grid;

[Serializable]
public class Level : IDisposable
{
	public Map map;
	public int count;
	public Material material;
	
	private static readonly Vector3 gridOffset = Vector3.one * 0.5f;
	
	private Vector3[] enemySpawnPoints;

	public event Action OnPlayerDefeat;
	public event Action OnEnemiesDefeat;

	private int enemyKilledCount = 0;
	private int enemySpawnedCount = 0;

	public Grid grid;

	// These need to be destroyed on unload
	private GameObject mapObject = null;
	private Mesh mapMesh = null;
	private Texture mapTexture = null;

	public EnemyTankControllerSystem enemyController;
	
	public IEnumerator Spawn()
	{
		enemyController.OnTankDestroyed += OnTankDestroyed;
		
		var delay = new WaitForSeconds(3);
		for (int i = 0; i < count; i++)
		{
			int index = i % enemySpawnPoints.Length;
			
			enemyController.Spawn(enemySpawnPoints[index]);
			enemySpawnedCount++;
			
			yield return delay;
		}
	}

	private void OnTankDestroyed()
	{
		enemyKilledCount++;
		if (enemySpawnedCount == count && enemyKilledCount == count)
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
		var playerBase = Object.Instantiate(Bootstrap.basePrefab, basePosition, Quaternion.identity, mapObject.transform);
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
