using System;
using System.Collections;
using UnityEngine;

using Grid = PathFinding.Grid;

[Serializable]
public class Level
{
	public Map map;
	public int count;
	public Material material;
	
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

	public void Clear()
	{	
		UnityEngine.Object.Destroy(mapObject);
		UnityEngine.Object.Destroy(mapMesh);
		UnityEngine.Object.Destroy(mapTexture);

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

		var gridOffset = Vector3.one * 0.5f * Map.SCALE;
		var gridScale = new Vector3(Map.SCALE, 0f, Map.SCALE);

		var brickPositions = map.GridPositions(TileType.WeakWall);
		for (int i = 0; i < brickPositions.Length; i++)
		{
			var point = Vector3.Scale(new Vector3(brickPositions[i].x, 0, brickPositions[i].y), gridScale);
			var block = GameObject.Instantiate(Bootstrap.brickBlockPrefab, point + gridOffset, Quaternion.identity, mapObject.transform);

			int x = brickPositions[i].x;
			int y = brickPositions[i].y;
			block.GetComponent<Breakable>().OnBreak += () => grid.OnBreakableBreak(x, y);
		}
		
		var concretePositions = map.GridPositions(TileType.StrongWall);
		for (int i = 0; i < concretePositions.Length; i++)
		{
			var point = Vector3.Scale(new Vector3(concretePositions[i].x, 0, concretePositions[i].y), gridScale);
			var block = GameObject.Instantiate(Bootstrap.concreteBlockPrefab, point + gridOffset, Quaternion.identity, mapObject.transform);

			int x = concretePositions[i].x;
			int y = concretePositions[i].y;
			block.GetComponent<Breakable>().OnBreak += () => grid.OnBreakableBreak(x, y);
		}
		
		var bushPositions = map.TilePositions(TileType.Woods);
		for (int i = 0; i < bushPositions.Length; i++)
		{
			GameObject.Instantiate(Bootstrap.bushPrefab, bushPositions[i] + gridOffset, Quaternion.identity, mapObject.transform);
		}

		var waterPositions = map.TilePositions(TileType.Water);
		for (int i = 0; i < waterPositions.Length; i++)
		{
			GameObject.Instantiate(Bootstrap.waterPrefab, waterPositions[i] + gridOffset, Quaternion.identity, mapObject.transform);
		}

		enemySpawnPoints = map.EnemySpawnPoints();
		for (int i = 0; i < enemySpawnPoints.Length; i++)
		{
			GameObject.Instantiate(Bootstrap.enemySpawnPrefab, enemySpawnPoints[i], Quaternion.identity, mapObject.transform);
		}

		var basePosition = map.BasePosition();
		enemyController.playerBasePosition = basePosition;
		var playerBase = GameObject.Instantiate(Bootstrap.basePrefab, basePosition, Quaternion.identity, mapObject.transform);
		playerBase.GetComponent<Breakable>().OnBreak += () => OnPlayerDefeat?.Invoke();
	}
}
