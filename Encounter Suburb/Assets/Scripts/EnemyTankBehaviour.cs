using UnityEngine;

[CreateAssetMenu]
public class EnemyTankBehaviour : ScriptableObject
{
	public Tank prefab;
	public TankSpecs specs;
	
	public float engageRange = 5f;
	public float sqrEngageRange { get; private set; }
	
	public float preferredShootDistance = 3f;
	public float sqrPreferredShootDistance { get; private set; }
	public bool preferBreakWalls = false;
	
	public enum PreferredTarget { None, Player, Base }
	public PreferredTarget preferredTarget;

	public void OnValidate()
	{
		sqrEngageRange = engageRange * engageRange;
		sqrPreferredShootDistance = preferredShootDistance * preferredShootDistance;
	}
}
