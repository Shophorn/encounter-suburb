using System.IO;
using PathFinding;
using UnityEngine;
using Grid = PathFinding.Grid;
using Path = System.IO.Path;


[RequireComponent(typeof(MenuSystem), typeof(BackGroundMusic))]
public class GameManager : MonoBehaviour
{
	private Texture2D[] maps;
	public Material defaultMaterial;
	private int currentLevelIndex = -1;
	private int nextLevelIndex => currentLevelIndex + 1;
	private Level currentLevel = null;
	
	public PlayerTankController playerTankPrefab;
	private PlayerTankController playerController;

	private MenuSystem menuSystem;

	public EnemyTankControllerSystem enemyController;
	
	[Header("Camera Position Values")]
	private const float cameraAngle = 50f;
	private readonly Vector3 cameraPosRatio = new Vector3(1.0f, -0.16875f, -3.125f);

	private BackGroundMusic backGroundMusic;

	[SerializeField] private PlayerHpBar playerHpBar;
	
	public int skipToLevelIndex = -1;
	
	private void Awake()
	{
		menuSystem = GetComponent<MenuSystem>();
		backGroundMusic = GetComponent<BackGroundMusic>();
	}

	private void Start()
	{
		enemyController.enabled = false;
		menuSystem.mainMenu_Play.onClick.AddListener(() =>
		{
			menuSystem.Hide();
			LoadFirstLevel();
		});

		menuSystem.levelComplete_Menu.onClick.AddListener(ShowMainMenu);
		menuSystem.gameComplete_Menu.onClick.AddListener(ShowMainMenu);
		menuSystem.gameOver_Menu.onClick.AddListener(ShowMainMenu);
		
		menuSystem.levelComplete_Next.onClick.AddListener(() =>
		{
			backGroundMusic.Play(backGroundMusic.None);
			menuSystem.Hide();
			menuSystem.ShowLevelStartInfo(maps[nextLevelIndex].name, nextLevelIndex + 1, LoadNextLevel);
		});
		
		menuSystem.mainMenu_Exit.onClick.AddListener(ExitGame);

		ShowMainMenu();

		maps = MapLoader.Load();
	}

	private void LoadFirstLevel()
	{
		currentLevelIndex = -1;

		if (skipToLevelIndex >= 0 && skipToLevelIndex < maps.Length)
		{
			currentLevelIndex = skipToLevelIndex - 1;
		}
		backGroundMusic.Play(backGroundMusic.None);

		menuSystem.ShowLevelStartInfo(maps[0].name, 1, LoadNextLevel);
	}
	
	private void LoadNextLevel()
	{
		currentLevelIndex++;
		currentLevel = new Level(maps[currentLevelIndex])
		{
			victoryCallback = OnEnemiesDefeat,
			defeatCallback = OnPlayerDefeat,
			enemyController = enemyController,
			material = defaultMaterial
		};

		currentLevel.BuildMap();
		PositionCamera();
		PathFinder.CreateInstance(currentLevel.grid);
		
		var playerPosition = currentLevel.map.PlayerSpawnPoint();
		playerController = Instantiate(playerTankPrefab, playerPosition, Quaternion.identity);
		playerController.GetComponent<Breakable>().OnBreak += OnPlayerDefeat;
			
		enemyController.playerTransform = playerController.transform;

		// TODO: read these from map
		int hunterCount = 20;
		int pummelCount = 20;
		
		enemyController.Begin(hunterCount, pummelCount);
		StartCoroutine(currentLevel.Spawn());

		backGroundMusic.Play(backGroundMusic.Game);
		
		playerHpBar.SetPlayer(playerController.tankBreakable);
		playerHpBar.SetBase(currentLevel.playerBaseBreakable);
	}

	private void UnloadLevel()
	{
		enemyController.Stop();

		Destroy(playerController.gameObject);
		playerController = null;
		
		currentLevel.Dispose();
		currentLevel = null;

		PathFinder.DeleteInstance();
	}

	private void OnPlayerDefeat()
	{
		backGroundMusic.Play(backGroundMusic.Defeat);
		
		menuSystem.ShowEndStatus("DEFEAT", () =>
		{
			UnloadLevel();
			menuSystem.Show(MenuView.GameOver);
		});
	}

	private void OnEnemiesDefeat()
	{
		backGroundMusic.Play(backGroundMusic.Victory);
		
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

	private void ShowMainMenu()
	{
		backGroundMusic.Play(backGroundMusic.Menu);
		menuSystem.Show(MenuView.Main);
	}

	public static bool[,] debugPath = null;

	public static void SetDebugPath(bool[,] path)
	{
		Debug.Log("debug path set");
		debugPath = path;
	}
	
	private void OnDrawGizmosSelected()
	{
		if (currentLevel?.grid == null || !Application.isPlaying) return;

		Grid grid = currentLevel.grid;
		for (int y = 0; y < grid.size; y++)
		{
			for (int x = 0; x < grid.size; x++)
			{
				float value = grid.nodes[x, y].preferDriveAroundPenalty / (float)Node.maxMovePenalty;
				if (value < 0)
				{
					value = 1f;
				}
				
				value = 1f - value;
				float red = grid.nodes[x, y].type == NodeType.Impassable ? 1f : value;
				Gizmos.color = new Color(red, value, value, 0.8f);

				if (debugPath[x, y])
				{
					Gizmos.color = new Color(red, value, 0f, 1f);
				}
				
				Gizmos.DrawCube(grid.GridToWorld(x, y), Vector3.one * (0.9f / Grid.resolution));
			}
		}
	}

	private static void ExitGame()
	{
	#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
	#else
		Application.Quit();
	#endif
	}
	
	private void PositionCamera()
	{
		float cos = Mathf.Cos(Mathf.Deg2Rad * cameraAngle);
		float sin = Mathf.Sin(Mathf.Deg2Rad * cameraAngle);

		float size = currentLevel.map.size * 0.5f;
		float x = cameraPosRatio.x * size;
		float y = cameraPosRatio.y * size;
		float z = cameraPosRatio.z * size;

		var cameraTransform = Camera.main.transform;
		cameraTransform.position = new Vector3
		(
			x: x,
			y: cos * y - sin * z,
			z: sin * y + cos * z + size
		);

		cameraTransform.rotation = Quaternion.Euler(cameraAngle, 0, 0);
	}
	
	


}