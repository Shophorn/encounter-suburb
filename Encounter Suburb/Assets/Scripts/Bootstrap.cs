using UnityEngine;

public class Bootstrap : MonoBehaviour
{
	private static Bootstrap instance;

	private void Awake()
	{
		instance = this;
	}
	
	[SerializeField] private GameObject _enemySpawnPrefab;
	[SerializeField] private GameObject _brickBlockPrefab;
	[SerializeField] private GameObject _concreteBlockPrefab;
	[SerializeField] private GameObject _bushPrefab;
	[SerializeField] private GameObject _waterPrefab;
	[SerializeField] private GameObject _basePrefab;

	public static GameObject enemySpawnPrefab => instance._enemySpawnPrefab;
	public static GameObject brickBlockPrefab => instance._brickBlockPrefab;
	public static GameObject concreteBlockPrefab => instance._concreteBlockPrefab;
	public static GameObject bushPrefab => instance._bushPrefab;
	public static GameObject waterPrefab => instance._waterPrefab;
	public static GameObject basePrefab => instance._basePrefab;
}