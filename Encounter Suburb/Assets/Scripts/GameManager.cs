using System.Collections.Generic;
using PathFinding;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public LevelInfo levelInfo;
	private Level level = null;

	public GameObject playerTankPrefab;
	private Transform playerTransform;

//	private Vector3 testSpawnPoint;
//	private Vector3 testBasePosition;
	
	private void Start()
	{
		// Create Map
		// Spawn player
		// Start Spawning enemies

		level = levelInfo.Level();
		
		level.BuildMap();
		PathFinder.grid = level.grid;
		var playerPosition = level.map.PlayerSpawnPoint();
		playerTransform = Instantiate(playerTankPrefab, (Vector3) playerPosition, Quaternion.identity).transform;

		EnemyTankControllerSystem.playerTransform = playerTransform;
		StartCoroutine(level.Spawn());

		level.OnEnemiesDefeat += () => Debug.Log("Enemies defeated");
		level.OnPlayerDefeat += () => Debug.Log("Player Defeated");

//		testSpawnPoint = level.map.EnemySpawnPoints()[0];
//		testBasePosition = level.map.BasePosition();
	}

//	private List<Node> nodesPath = new List<Node>();
//	public Vector3[] wayPoints;
//	private void Update()
//	{
//		PathFinder.gm = this;
//		PathFinder.grid = level.grid;
//
////		var target = Vector3.Distance(testSpawnPoint, playerTransform.position) < 15f
////			? playerTransform.position
////			: testBasePosition;
////		
////		nodesPath = PathFinder.FindPath(testSpawnPoint, target, false);
//
//		if (Input.GetButton("Jump"))
//		{
//			PathRequestManager.RequestPath(testSpawnPoint, playerTransform.position, OnPathUpdate);
//		}
//	}
//
//	private void OnPathUpdate(Path path)
//	{
//		if (path != null)
//		{
//			nodesPath = path.nodes;
//			wayPoints = path.points.ToArray();
//		}
//		else
//		{
//			nodesPath = null;
//			wayPoints = new Vector3[0];
//		}
//	}
	
//	
//	private void OnDrawGizmos()
//	{
//		if (level == null) return;
//
//		var grid = level.grid;
//
//		var playerNode = grid.NodeIndexFromWorldPoint(playerTransform.position);
//		
//		for (int y = 0; y < grid.size; y++)
//		{
//			for (int x = 0; x < grid.size; x++)
//			{
//				/*
//				switch (grid.nodes[x,y].type)
//				{
//					case NodeType.Open: 		Gizmos.color = Color.green; 	break;
//					case NodeType.Breakable:	Gizmos.color = Color.yellow;	break;
//					case NodeType.Impassable:	Gizmos.color = Color.red;		break;
//					case NodeType.Error: 		Gizmos.color = Color.magenta;	break;
//				}
//
//
//				if (x == playerNode.x && y == playerNode.y)
//				{
//					Gizmos.color = Color.white;
//				}
//				*/
//				if (nodesPath != null && nodesPath.Contains(grid.nodes[x, y]))
//				{
//					Gizmos.color = Color.white;
//					Gizmos.DrawCube(new Vector3(x + 0.5f,0,y + 0.5f) * Map.SCALE, Vector3.one * 0.9f * Map.SCALE);
//				}
//				
//			}
//		}
//	}
}