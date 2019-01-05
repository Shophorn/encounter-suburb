using PathFinding;
using UnityEngine;

[RequireComponent(typeof(MenuSystem))]
public class GameManager : MonoBehaviour
{
	public LevelInfo levelInfo;
	private Level level = null;

	public LevelInfo[] levelInfos;
	private Level[] levels;
	private int currentLevelIndex = 0;
	
	public GameObject playerTankPrefab;
	private Transform playerTransform;

	private MenuSystem menuSystem;

	public EnemyTankControllerSystem enemyController;
	
	private void Awake()
	{
		menuSystem = GetComponent<MenuSystem>();
	}
	
	private void Start()
	{
		enemyController.enabled = false;
		menuSystem.Show(MenuView.Main);
		menuSystem.mainMenu_Play.onClick.AddListener(() =>
		{
			menuSystem.Hide();
			LoadLevel(0);
		});				
		
		menuSystem.gameComplete_Menu.onClick.AddListener(() => menuSystem.Show(MenuView.Main));
		menuSystem.gameOver_Menu.onClick.AddListener(() => menuSystem.Show(MenuView.Main));

	}

	private void LoadLevel(int index)
	{
		// Create Map
		// Spawn player
		// Start Spawning enemies
		
		level = levelInfo.Level();
		level.enemyController = enemyController;
		
		level.BuildMap();
		PathFinder.grid = level.grid;
		var playerPosition = level.map.PlayerSpawnPoint();
		playerTransform = Instantiate(playerTankPrefab, (Vector3) playerPosition, Quaternion.identity).transform;

		enemyController.playerTransform = playerTransform;
		enemyController.enabled = true;
		StartCoroutine(level.Spawn());

		level.OnEnemiesDefeat += OnEnemiesDefeat;
		level.OnPlayerDefeat += OnPlayerDefeat;
	}
	
	private void OnPlayerDefeat()
	{
		// Unload level
		// Store player progress to database etc.
		// Load Menu level
		
		menuSystem.Show(MenuView.GameOver);
		UnloadLevel();

	}

	private void OnEnemiesDefeat()
	{
		// Save player progress
		// Unload level
		// Load next level
		menuSystem.Show(MenuView.LevelComplete);
		UnloadLevel();
	}

	private void UnloadLevel()
	{
		enemyController.Clear();
		enemyController.enabled = false;
		
		Destroy(playerTransform.gameObject);
		playerTransform = null;
		level.Clear();
		level = null;
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