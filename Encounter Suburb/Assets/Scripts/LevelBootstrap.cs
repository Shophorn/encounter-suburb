using UnityEngine;

public class LevelBootstrap : MonoBehaviour
{
	private static LevelBootstrap instance;

	private void Awake()
	{
		instance = this;
	}

	[SerializeField] private Color[] _skyColors;
	public static Color RandomSkyColor(System.Random random) => instance._skyColors.RandomItem(random);
	
	[SerializeField] private GameObject _enemySpawnPrefab;
	[SerializeField] private Breakable _brickBlockPrefab;
	[SerializeField] private Breakable _concreteBlockPrefab;
	[SerializeField] private GameObject _waterPrefab;
	[SerializeField] private Breakable _basePrefab;

	public static GameObject enemySpawnPrefab => instance._enemySpawnPrefab;
	public static Breakable brickBlockPrefab => instance._brickBlockPrefab;
	public static Breakable concreteBlockPrefab => instance._concreteBlockPrefab;
	public static GameObject waterPrefab => instance._waterPrefab;
	public static Breakable playerBasePrefab => instance._basePrefab;

	[SerializeField] private GameObject[] _treePrefabs;
	public static GameObject RandomTree(System.Random random) => instance._treePrefabs.RandomItem(random);
	
	[SerializeField] private Texture2D _groundTexture;
	[SerializeField] private Texture2D _waterTexture;
	[SerializeField] private Texture2D _woodsTexture;
	[SerializeField] private Texture2D _constructionTexture;
	[SerializeField] private Texture2D _tileMask;

	public static Texture2D groundTexture => instance._groundTexture;
	public static Texture2D waterTexture => instance._waterTexture;
	public static Texture2D woodsTexture => instance._woodsTexture;
	public static Texture2D constructionTexture => instance._constructionTexture;
	public static Texture2D tileMask => instance._tileMask;

	[SerializeField] private Material _mapMaterial;
	
	public static Material mapMaterial => instance._mapMaterial;
}

public static class ArrayRandom
{
	public static T RandomItem<T>(this T[] array, System.Random random)
	{
		return array[random.Next(array.Length)];
	}
}