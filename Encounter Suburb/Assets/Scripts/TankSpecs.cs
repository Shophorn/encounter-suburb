using UnityEngine;

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
	public int burstCount = 1;
	public float fireRate = 1f;
	public float burstCooldownTime = 2f;

	public Gun CreateGun(params Transform[] muzzles)
	{
		float reloadTime = 1f / fireRate;
		return new Gun
		{
			muzzles = muzzles,
			projectile = projectile,
			reloadTime = reloadTime,
			burstCount = burstCount,
			burstCooldownTime = Mathf.Max(0f, burstCooldownTime - reloadTime)
		};
	}
}