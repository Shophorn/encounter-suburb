using PathFinding;
using UnityEngine;

[RequireComponent(typeof(MenuSystem))]
public class GameManager : MonoBehaviour
{
	public Texture2D[] maps;
	public Material defaultMaterial;
	private int currentLevelIndex = -1;
	private Level currentLevel;
	
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
			LoadNextLevel();
		});

		menuSystem.Show(MenuView.Main);
	}

	private void LoadFirstLevel()
	{
		Debug.Log("Load First Level");
		
		currentLevelIndex = -1;
		LoadNextLevel();
	}
	
	private void LoadNextLevel()
	{
		Debug.Log("Load Level");
		
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
		PathFinder.grid = currentLevel.grid;
		
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
	}

	private void OnPlayerDefeat()
	{
		// Unload level
		// Store player progress to database etc.
		// Load Menu level

		UnloadLevel();
		menuSystem.Show(MenuView.GameOver);
	}

	private void OnEnemiesDefeat()
	{
		// Save player progress
		// Unload level
		// Load next level
	
		UnloadLevel();

		if (currentLevelIndex == maps.Length - 1)
		{
			menuSystem.Show(MenuView.GameComplete);
		}
		else
		{
			menuSystem.Show(MenuView.LevelComplete);
		}
		
	}
}