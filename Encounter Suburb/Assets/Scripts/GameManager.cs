using PathFinding;
using UnityEngine;
using Grid = PathFinding.Grid;

[RequireComponent(typeof(MenuSystem))]
public class GameManager : MonoBehaviour
{
	private Texture2D[] maps;
	public Material defaultMaterial;
	private int currentLevelIndex = -1;
	private int nextLevelIndex => currentLevelIndex + 1;
	private Level currentLevel = null;
	
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
		menuSystem.mainMenu_Play.onClick.AddListener(() =>
		{
			menuSystem.Hide();
			LoadFirstLevel();
		});

		menuSystem.levelComplete_Menu.onClick.AddListener(() => menuSystem.Show(MenuView.Main));
		menuSystem.gameComplete_Menu.onClick.AddListener(() => menuSystem.Show(MenuView.Main));
		menuSystem.gameOver_Menu.onClick.AddListener(() => menuSystem.Show(MenuView.Main));
		
		menuSystem.levelComplete_Next.onClick.AddListener(() =>
		{
			menuSystem.Hide();
			menuSystem.ShowLevelStartInfo(maps[nextLevelIndex].name, nextLevelIndex + 1, LoadNextLevel);
		});

		menuSystem.Show(MenuView.Main);
		
		LoadMaps();
	}

	private void LoadMaps()
	{
		maps = Resources.LoadAll<Texture2D>("Maps");

		foreach (var map in maps)
		{
			var array = map.name.Split('_');
			var nameString = array[1];
			for (int i = 2; i < array.Length; i++)
			{
				nameString += " " + array[i];
			}

			map.name = nameString;
		}
	}

	private void LoadFirstLevel()
	{
		currentLevelIndex = -1;
		menuSystem.ShowLevelStartInfo(maps[0].name, 1, LoadNextLevel);
	}
	
	private void LoadNextLevel()
	{
		// Create Map
		// Spawn player
		// Start Spawning enemies
		currentLevelIndex++;
		currentLevel = new Level
		{
			map = Map.FromTexture(maps[currentLevelIndex]),
			count = 5,
			enemyController = enemyController,
			material = defaultMaterial
		};

		currentLevel.BuildMap();
		PathFinder.CreateInstance(currentLevel.grid);
		
		var playerPosition = currentLevel.map.PlayerSpawnPoint();
		playerTransform = Instantiate(playerTankPrefab, (Vector3) playerPosition, Quaternion.identity).transform;

		enemyController.playerTransform = playerTransform;
		enemyController.Begin(20);
		StartCoroutine(currentLevel.Spawn());

		currentLevel.OnEnemiesDefeat += OnEnemiesDefeat;
		currentLevel.OnPlayerDefeat += OnPlayerDefeat;
	}

	private void UnloadLevel()
	{
		enemyController.Stop();

		Destroy(playerTransform.gameObject);
		playerTransform = null;
		
		currentLevel.Clear();
		currentLevel = null;

		PathFinder.ClearInstance();
	}

	private void OnPlayerDefeat()
	{
		// Unload level
		// Store player progress to database etc.
		// Load Menu level
		menuSystem.ShowEndStatus("DEFEAT", () =>
		{
			UnloadLevel();
			menuSystem.Show(MenuView.GameOver);
		});
	}

	private void OnEnemiesDefeat()
	{
		// Save player progress
		// Unload level
		// Load next level
	
		menuSystem.ShowEndStatus("VICTORY!", () =>
		{
			UnloadLevel();
			if (currentLevelIndex == maps.Length - 1)
			{
				menuSystem.Show(MenuView.GameComplete);
			}
			else
			{
				menuSystem.Show(MenuView.LevelComplete);
			}
		});
	}

	private void OnDrawGizmos()
	{
		if (currentLevel == null) return;

		Grid grid = currentLevel.grid;
		for (int y = 0; y < grid.size; y++)
		{
			for (int x = 0; x < grid.size; x++)
			{
				float value = grid.nodes[x, y].preferDriveAroundPenalty / 10f;
				if (value < 0)
				{
					value = 1f;
				}
				
				value = 1f - value;
				
				Gizmos.color = new Color(value, value, value, 1);
				Gizmos.DrawCube(grid.NodeWorldPosition(new Vector2Int(x, y)), Vector3.one * 2f / 3f);
			}
		}
	}
}