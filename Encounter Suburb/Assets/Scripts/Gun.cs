using System.Collections;
using UnityEngine;

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
			for (int ii = 0; ii < muzzles.Length; ii++)
			{
				ProjectileSystem.Shoot(
					muzzles[ii].position,
					muzzles[ii].rotation,
					projectile
				);
			}
			
			continueTime = Time.time + reloadTime;
			while (Time.time < continueTime)
				yield return null;
		}

		continueTime = Time.time + burstCooldownTime;
		while (Time.time < continueTime)
			yield return null;
	}
}
