using System;
using UnityEngine;

[CreateAssetMenu]
public class GunType : ScriptableObject
{
	public ProjectileType projectile;
	public int roundsPerSecond = 1;
	public float reloadTime;

	public bool autoFire;
	
	private void OnValidate()
	{
		reloadTime = 1f /(float) roundsPerSecond;
	}
}

[Serializable]
public class Gun
{
	public GunType type;
	public Transform muzzle;

	private float lastFireTime;

	/// <summary>
	/// Percent (0 ... 1) describing current reload state. 
	/// </summary>
	/// <returns></returns>
	public float reloadState => (Time.time - lastFireTime) / type.reloadTime;
	
	public void Fire()
	{
		if (reloadState < 1f) return;
		
		ProjectileSystem.Shoot(muzzle.position, muzzle.rotation, type.projectile);
		lastFireTime = Time.time;
	}
}