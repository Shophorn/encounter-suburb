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
	
	[Header("Camera Position Values")]
	public float cameraAngle = 50f;
	public Vector3 cameraPosRatio = new Vector3(1.0f, -0.16875f, -3.125f);

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
		PositionCamera();
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

	private void OnGUI()
	{
		var screenPosition = Input.mousePosition;
		screenPosition.x = (screenPosition.x - (Screen.width / 2)) / (Screen.width / 2);
		screenPosition.y = (screenPosition.y - (Screen.height / 2)) / (Screen.height / 2);
		
		GUI.Label(new Rect(10, 10, 200, 40), $"X: {screenPosition.x : 0.000} / {Input.mousePosition.x : 0.000}\nY: {screenPosition.y : 0.000} / {Input.mousePosition.y : 0.000}");
	}

	private void PositionCamera()
	{
		float cos = Mathf.Cos(Mathf.Deg2Rad * cameraAngle);
		float sin = Mathf.Sin(Mathf.Deg2Rad * cameraAngle);

		float size = currentLevel.map.size;
		float y = cameraPosRatio.y * size;
		float z = cameraPosRatio.z * size;

		var cameraTransform = Camera.main.transform;
		cameraTransform.position = new Vector3
		(
			x: size,
			y: cos * y - sin * z,
			z: sin * y + cos * z + size
		);

		cameraTransform.rotation = Quaternion.Euler(cameraAngle, 0, 0);
	}
}