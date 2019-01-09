using PathFinding;
using UnityEngine;
using Grid = PathFinding.Grid;

[RequireComponent(typeof(MenuSystem), typeof(BackGroundMusic))]
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
	
	// Shouldn't really mess with these, values are found working by experiment
	[Header("Camera Position Values")]
	public float cameraAngle = 50f;
	public Vector3 cameraPosRatio = new Vector3(1.0f, -0.16875f, -3.125f);

	private BackGroundMusic backGroundMusic;
	
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
		
		LoadMaps();
	}

	private void LoadMaps()
	{
		maps = Resources.LoadAll<Texture2D>("Maps");
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
		currentLevel = new Level(maps[currentLevelIndex], null, null)
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
		playerTransform = Instantiate(playerTankPrefab, (Vector3) playerPosition, Quaternion.identity).transform;
		playerTransform.GetComponent<Breakable>().OnBreak += OnPlayerDefeat;
			
		enemyController.playerTransform = playerTransform;
		enemyController.Begin(20);
		StartCoroutine(currentLevel.Spawn());

//		currentLevel.OnEnemiesDefeat += OnEnemiesDefeat;
//		currentLevel.OnPlayerDefeat += OnPlayerDefeat;
		
		backGroundMusic.Play(backGroundMusic.Game);
	}

	private void UnloadLevel()
	{
		enemyController.Stop();

		Destroy(playerTransform.gameObject);
		playerTransform = null;
		
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

	private void OnDrawGizmosSelected()
	{
		if (currentLevel == null || !Application.isPlaying) return;

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
				Gizmos.DrawCube(grid.NodeWorldPosition(x, y), Vector3.one * 0.2f);
			}
		}
	}

	private void ExitGame()
	{
	#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
	#else
		Application.Quit
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