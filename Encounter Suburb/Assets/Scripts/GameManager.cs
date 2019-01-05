using PathFinding;
using UnityEngine;

[RequireComponent(typeof(MenuSystem))]
public class GameManager : MonoBehaviour
{
	public LevelInfo levelInfo;
	private Level level = null;

	public GameObject playerTankPrefab;
	private Transform playerTransform;

	private MenuSystem menu;

	private void Awake()
	{
		menu = GetComponent<MenuSystem>();
	}
	
	private void Start()
	{
		menu.Show(MenuView.Main);
		menu.mainMenu_Play.onClick.AddListener(() =>
		{
			menu.Hide();
			LoadLevel(0);
		});				
	}

	private void LoadLevel(int index)
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
	}
	
	private void OnPlayerDefeat()
	{
		// Unload level
		// Store player progress to database etc.
		// Load Menu level
	}

	private void OnEnemiesDefeat()
	{
		// Save player progress
		// Unload level
		// Load next level
	}
	
	
	private void OnDrawGizmos()
	{
		if (level == null) return;

		var grid = level.grid;

		for (int y = 0; y < grid.size; y++)
		{
			for (int x = 0; x < grid.size; x++)
			{
				switch (grid.nodes[x,y].type)
				{
					case NodeType.Open: 		Gizmos.color = Color.green; 	break;
					case NodeType.Breakable:	Gizmos.color = Color.yellow;	break;
					case NodeType.Impassable:	Gizmos.color = Color.red;		break;
					case NodeType.Error: 		Gizmos.color = Color.magenta;	break;
				}
				
				Gizmos.DrawCube(grid.NodeWorldPosition(new Vector2Int(x,y)), Vector3.one * grid.scale * 0.9f); 
				
			}
		}
	}
}