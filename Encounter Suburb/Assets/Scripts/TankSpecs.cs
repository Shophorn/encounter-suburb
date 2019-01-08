using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu]
public class TankSpecs : ScriptableObject
{
	[Header("Hull")]
	public float moveSpeed = 10f;
	public float rotationSpeed = 90f;
	
	[Header("Turret")]
	public float turretTurnSpeed = 90f;
	
	[Header("Gun")]
	public ProjectileType projectile;
	public float roundsPerSecond = 1;
	public float reloadTime;

	public bool autoFire;
	
	private void OnValidate()
	{
		reloadTime = 1f /(float) roundsPerSecond;
	}

}