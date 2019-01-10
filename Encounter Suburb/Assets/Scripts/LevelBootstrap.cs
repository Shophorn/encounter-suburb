using UnityEngine;

public class LevelBootstrap : MonoBehaviour
{
	private static LevelBootstrap instance;

	private void Awake()
	{
		instance = this;
	}
	
	[SerializeField] private GameObject _enemySpawnPrefab;
	[SerializeField] private Breakable _brickBlockPrefab;
	[SerializeField] private Breakable _concreteBlockPrefab;
	[SerializeField] private GameObject _bushPrefab;
	[SerializeField] private GameObject _waterPrefab;
	[SerializeField] private Breakable _basePrefab;

	public static GameObject enemySpawnPrefab => instance._enemySpawnPrefab;
	public static Breakable brickBlockPrefab => instance._brickBlockPrefab;
	public static Breakable concreteBlockPrefab => instance._concreteBlockPrefab;
	public static GameObject bushPrefab => instance._bushPrefab;
	public static GameObject waterPrefab => instance._waterPrefab;
	public static Breakable playerBasePrefab => instance._basePrefab;
}