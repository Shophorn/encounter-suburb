using System;
using System.Collections;
using System.Linq;
using PathFinding;
using UnityEngine;

public enum TankType { Hunter, Pummel, Heavy }

public class EnemyTankControllerSystem : MonoBehaviour
{
	[Obsolete("Do not use transform", true)]
	private new Transform transform;
	
	[SerializeField] private float pathUpdateInterval = 0.2f;
	[SerializeField] private EnemyTankBehaviour[] behaviours;
	private Tank[] prefabs;
	
	public LayerMask tankCollisionMask;

	[Serializable]
	private class UnitCollection
	{
		public int activeCount;
		public int nextFreeIndex;
		
		public Tank[] units;
		public Path[] paths;
		public float[] pathUpdateTimes;
		public Breakable[] targetBreakables;
	}

	private UnitCollection[] unitCollections;
	
	public Transform playerTransform { get; set; }
	public Vector3 playerBasePosition { get; set; }

	public event Action OnTankDestroyed;

	[Header("Effects")]
	public ParticleSystem explosion;
	public ParticleSystem spawnEffect;
	
	public void Begin(int maxCount)
	{
		prefabs = behaviours.Select(b => b.prefab).ToArray();
		
		// Initialize better type aware arrays
		int typeCount = behaviours.Length;
		unitCollections = new UnitCollection[typeCount];
		for (int i = 0; i < typeCount; i++)
		{
			unitCollections[i] = new UnitCollection
			{
				activeCount = 0,
				nextFreeIndex = 0,
				units = FillWithType((TankType)i, maxCount),
				paths = new Path[maxCount],
				pathUpdateTimes = new float[maxCount],
				targetBreakables = new Breakable[maxCount]
			};
		}
		
		enabled = true;
	}

	public void Stop()
	{
		enabled = false;
		Clear();
	}
	
	private void Update()
	{
		var playerPosition = playerTransform.position;
		for (int t = 0; t < behaviours.Length; t++)
		{
			int type = t;
			int count = unitCollections[t].units.Length; //activeCount;
			var units = unitCollections[t].units;
			var paths = unitCollections[t].paths;
			var pathUpdateTimes = unitCollections[t].pathUpdateTimes;
			var targetBreakables = unitCollections[t].targetBreakables;
			
			for (int i = 0; i < count; i++)
			{
				// Shoudl instead track active units
				if (!units[i].gameObject.activeInHierarchy) continue;

				var tankPosition = units[i].transform.position;
				float sqrDistanceToPlayer = (playerPosition - tankPosition).sqrMagnitude;

				Vector3 targetPosition;
				float sqrDistanceToTarget;

				if (sqrDistanceToPlayer < behaviours[type].sqrEngageRange)
				{
					targetPosition = playerPosition;
					sqrDistanceToTarget = sqrDistanceToPlayer;
				}
				else
				{
					targetPosition = playerBasePosition;
					sqrDistanceToTarget = (tankPosition - playerBasePosition).sqrMagnitude;
				}

				if (paths[i] == null || pathUpdateTimes[i] < Time.time)
				{
					int index = i;
					PathRequestManager.RequestPath(tankPosition, targetPosition, behaviours[type].preferBreakWalls,
						(path) => OnReceivePath(type, index, path));
				}

				var wayPoint = paths[i].currentPoint;
				var toWayPoint = wayPoint - tankPosition;

				const float sqrEpsilon = 0.001f;
				if (toWayPoint.sqrMagnitude < sqrEpsilon)
				{
					if (!paths[i].MoveNext())
					{
						paths[i] = null;
						continue;
					}

					wayPoint = paths[i].currentPoint;
					toWayPoint = wayPoint - tankPosition;
				}

				float maxMoveMagnitude = targetBreakables[i] != null ? 0f : 1f;

				var driveVector = Vector3.ClampMagnitude(toWayPoint, maxMoveMagnitude);
				Debug.DrawRay(units[i].transform.position + Vector3.up * 0.5f, driveVector * 5f, Color.cyan);

				units[i].Drive(driveVector);

				// Aim and Shoot
				bool doShoot = false;
				if (targetBreakables[i] != null)
				{
					units[i].AimTurretAt(targetBreakables[i].transform.position);
					doShoot = true;
				}
				else
				{
					units[i].AimTurretAt(targetPosition);
					const float shootDotThreshold = 0.95f;
					Vector3 toTarget = targetPosition - tankPosition;

					bool inRange = sqrDistanceToTarget < units[i].gun.type.projectile.sqrMaxRange;
					bool inSight = Vector3.Dot(toTarget, units[i].turretForward) > shootDotThreshold;

					doShoot = inRange && inSight;
				}

				if (doShoot)
				{
					units[i].collider.enabled = false;
					units[i].gun.Fire();
					units[i].collider.enabled = true;
				}
			}
		}
	}

	private Tank[] FillWithType(TankType type, int count)
	{
		var array = new Tank [count];
		for (int i = 0; i < count; i++)
		{
			// Use separate variable to pass to lambda, so it keeps it's value.
			array[i] = Instantiate(prefabs[(int) type]);
			array[i].name += $" {i}";
			array[i].gameObject.SetActive(false);
		}
		return array;
	}

	public void Spawn(Vector3 position, TankType type)
	{
		if (!enabled) return;

		int typeIndex = (int) type;

		int index = unitCollections[typeIndex].nextFreeIndex;
		unitCollections[typeIndex].nextFreeIndex++;

		var unit = unitCollections[typeIndex].units[index];
		unit.GetComponent<Breakable>().OnBreak += () => UnSpawn(typeIndex, index);
		unit.OnCollideBreakable += breakable => AddTargetBreakable(typeIndex, index, breakable);
		unit.collisionMask = tankCollisionMask;

		StartCoroutine(UnearthUnit(unit, position, typeIndex, index));
		
		PathRequestManager.RequestPath(
			start:				position,
			end:				behaviours[typeIndex].preferTargetPlayer < 2 ? playerTransform.position : playerBasePosition,
			preferBreakWalls:	behaviours[typeIndex].preferBreakWalls,
			callback:			path => OnReceivePath(typeIndex, index, path)
		);
	}

	private IEnumerator UnearthUnit(Tank unit, Vector3 position, int typeIndex, int index)
	{
		// Wait some time, then rise from ground in some other time
		const float riseTime = 1f;
		float waitTime = spawnEffect.main.duration - riseTime;

		Instantiate(spawnEffect, position, spawnEffect.transform.rotation);

		yield return new WaitForSeconds(waitTime);
		
		// hide underground
		position.y = -unit.height;
		unit.transform.position = position;
		unit.gameObject.SetActive(true);

		
		float percent = 0f;
		while(percent < 1f)
		{
			percent += Time.deltaTime / riseTime;
			position.y = (percent - 1f) * unit.height; // lerp
			unit.transform.position = position;
			yield return null;
		}
		
		// Activate, but ponder around a moment
		const float ponderTime = 0.25f;
		yield return new WaitForSeconds(ponderTime);
	}

	private void SwapWithInactive(int typeIndex, int inactive, int active)
	{
		var uc = unitCollections[typeIndex];
		
		// Swap with last active
		var tempUnit = uc.units[active];
		uc.units[active] = uc.units[inactive];
		uc.units[inactive] = tempUnit;

		uc.paths[inactive] = uc.paths[active];
		uc.pathUpdateTimes[inactive] = uc.pathUpdateTimes[active];
		uc.targetBreakables[inactive] = uc.targetBreakables[active];
		
		uc.paths[active] = null;
		uc.pathUpdateTimes[active] = 0f;
		uc.targetBreakables[active] = null;
	}
	
	
	private void UnSpawn(int typeIndex, int index)
	{
		var uc = unitCollections[typeIndex];
		uc.units[index].gameObject.SetActive(false);
		uc.paths[index] = null;
		uc.targetBreakables[index] = null;
		
		OnTankDestroyed();

		Instantiate(explosion, unitCollections[typeIndex].units[index].transform.position, Quaternion.identity);
	}

	private void AddTargetBreakable(int typeIndex, int index, Breakable target)
	{
		unitCollections[typeIndex].targetBreakables[index] = target;
		target.OnBreak += () => unitCollections[typeIndex].targetBreakables[index] = null;
	}

	private void OnReceivePath(int typeIndex, int index, Path path)
	{
		unitCollections[typeIndex].paths[index] = path;
		unitCollections[typeIndex].pathUpdateTimes[index] = Time.time + pathUpdateInterval;
	}

	private void Clear()
	{
		for (int i = 0; i < unitCollections.Length; i++)
		{
			for (int j = 0; j < unitCollections[i].units.Length; j++)
			{
				Destroy(unitCollections[i].units[j].gameObject);
			}
		}

		unitCollections = null;
	}
}