using System.Collections;
using UnityEngine;

//[Serializable]
public class Gun
{
	// Data
	public Transform[] muzzles;
	public ProjectileType projectile;
	public float reloadTime;
	public int burstCount;
	public float burstCooldownTime; // When assigning this, take last bullet's reload time into account

	public IEnumerator FireBurst()
	{
		float continueTime = 0f;
		for (int i = 0; i < burstCount; i++)
		{
			FireOnce();

			continueTime = Time.time + reloadTime;
			while (Time.time < continueTime) yield return null;
		}

		continueTime = Time.time + burstCooldownTime;
		while (Time.time < continueTime) yield return null;
	}
	
	public void FireOnce()
	{
		for (int i = 0; i < muzzles.Length; i++)
		{
			ProjectileSystem.Shoot(muzzles[i].position, muzzles[i].rotation, projectile);
		}
	}
//	
//	
//	
//	
//	
//	
//	
//	
//	
//	// State
//	private float lastFireTime;
//	
//	/// <summary>
//	/// Percent (0 ... 1) describing current reload state. 
//	/// </summary>
//	/// <returns></returns>
//	public float reloadState => (Time.time - lastFireTime) / reloadTime;
//	
//	public void FireXXX()
//	{
//		if (reloadState < 1f) return;
//
//		for (int i = 0; i < muzzles.Length; i++)
//		{
//			ProjectileSystem.Shoot(muzzles[i].position, muzzles[i].rotation, projectile);
//		}
//		lastFireTime = Time.time;
//	}

}
