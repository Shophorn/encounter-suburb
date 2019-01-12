using System;
using System.Collections;
using PathFinding;
using UnityEngine;

public enum TankType { Hunter, Pummel, Heavy }

public class EnemyTankControllerSystem : MonoBehaviour
{
	[System.Obsolete("Don't use system's Transform", true)]
	private new Transform transform;

	[SerializeField] private EnemyTankBehaviour hunterBehaviour;
	[SerializeField] private EnemyTankBehaviour pummelBehaviour;

	[SerializeField] private LayerMask tankCollisionMask;
	[SerializeField] private float pathUpdateInterval = 0.25f;
	[SerializeField] private float riseFromUnderworldTime = 1f;
	[SerializeField] private float decisionMakeDelay = 0.25f;
	[SerializeField] private ParticleSystem spawnEffect;

	private struct TankInstance
	{
		public readonly Tank tank;
		public Path path;
		public float pathUpdateTime;
		public bool hasRequestedPath;
		public Breakable targetBreakable;
		

		public TankInstance(Tank tank)
		{
			this.tank = tank;
			path = null;
			pathUpdateTime = 0f;
			hasRequestedPath = false;
			targetBreakable = null;
		}

		public void Disable()
		{
			tank.gameObject.SetActive(false);
			path = null;
			pathUpdateTime = 0f;
			hasRequestedPath = false;
			targetBreakable = null;
		}
	}
	
	[System.Serializable]
	private class TankUnit
	{
		public EnemyTankBehaviour behaviour;
		public TankInstance[] units;
		public int[] activeIndicesMap;
		public int activeCount;
		public int nextIndex;
	}

	private TankUnit hunters;
	private TankUnit pummels;
	
	// Target candidates
	[NonSerialized] public Transform playerTransform;
	[NonSerialized] public Vector3 playerBasePosition;

	public event Action OnTankDestroyed;
	

	public void Begin(int hunterCount, int pummelCount)
	{
		hunters = CreateTankUnit(hunterBehaviour, hunterCount);
		pummels = CreateTankUnit(pummelBehaviour, pummelCount);

		enabled = true;
	}

	private void Update()
	{
		UpdateUnitOfType(TankType.Hunter);
		UpdateUnitOfType(TankType.Pummel);
	}

	private void UpdateUnitOfType(TankType type)
	{
		var unit = UnitFromType(type);
		var playerPosition = playerTransform.position;

		for (int i = 0; i < unit.activeCount; i++)
		{
			// use this to refernce anything
			int index = unit.activeIndicesMap[i];
			
			var instance = unit.units[index];
			var tankPosition = instance.tank.transform.position;
			float sqrDistanceToPlayer = (playerPosition - tankPosition).sqrMagnitude;

			Vector3 targetPosition;
			float sqrDistanceToTarget;

			if (sqrDistanceToPlayer < hunterBehaviour.sqrEngageRange)
			{
				targetPosition = playerPosition;
				sqrDistanceToTarget = sqrDistanceToPlayer;
			}
			else
			{
				targetPosition = playerBasePosition;
				sqrDistanceToTarget = (tankPosition - playerBasePosition).sqrMagnitude;
			}
			
			// Follow Path
			if (instance.path == null)
			{
				if (!instance.hasRequestedPath)
					RequestPath(type, index, targetPosition);
				continue;
			}

			var wayPoint = instance.path.currentPoint;
			var toWayPoint = wayPoint - instance.tank.transform.position;

			// If we have reached current waypoint, move to next
			if (toWayPoint.sqrMagnitude < 0.0001f)
			{
				// If this is last, set null, it will be reset later
				if (!instance.path.MoveNext())
				{
					unit.units[index].path = null;
					continue;
				}

				wayPoint = instance.path.currentPoint;
				toWayPoint = wayPoint - instance.tank.transform.position;
			}

			// Stay still if there is breakable target on the way
			float maxMoveMagnitude = instance.targetBreakable != null ? 0f : 1f;

			var driveVector = Vector3.ClampMagnitude(toWayPoint, maxMoveMagnitude);
			instance.tank.Drive(driveVector);
			
			// Aim and Shoot
			if (instance.targetBreakable != null)
			{
				targetPosition = instance.targetBreakable.transform.position;
				sqrDistanceToTarget = (tankPosition - targetPosition).sqrMagnitude;
			}
			
			instance.tank.AimTurretAt(targetPosition);
			const float shootDotThreshold = 0.95f;
			Vector3 toTarget = targetPosition - tankPosition;

			bool inRange = sqrDistanceToTarget < instance.tank.gun.type.projectile.sqrMaxRange;
			bool inSight = Vector3.Dot(toTarget, instance.tank.turretForward) > shootDotThreshold;

			if (inRange && inSight)
			{
				instance.tank.collider.enabled = false;
				instance.tank.gun.Fire();
				instance.tank.collider.enabled = true;
			}
		}
	}

	private static TankUnit CreateTankUnit(EnemyTankBehaviour behaviour, int count)
	{
		var tanks = new TankInstance[count];
		for (int i = 0; i < count; i++)
		{
			tanks[i] = new TankInstance(Instantiate(behaviour.prefab));
			tanks[i].tank.gameObject.SetActive(false);
		}

		return new TankUnit
		{
			behaviour = behaviour,
			units = tanks,
			activeIndicesMap = new int[count],
			activeCount = 0,
			nextIndex = 0
		};
	}

	private void AddTargetBreakable(TankType type, int index, Breakable target)
	{
		// Do not add another tank or player as target
		if (target.GetComponent<Tank>() != null || target.transform == playerTransform) return;
		
		UnitFromType(type).units[index].targetBreakable = target;
	}

	// TODO: Only 1 usage, remove
	private void RequestPath(TankType type, int index, Vector3 target)
	{
		TankUnit unit = UnitFromType(type);
		unit.units[index].hasRequestedPath = true;
		
		PathRequestManager.RequestPath(
			start:				unit.units[index].tank.transform.position,
			end:				target, //unit.behaviour.preferTargetPlayer < 2 ? playerBasePosition : playerTransform.position,
			preferBreakWalls:	unit.behaviour.preferBreakWalls,
			callback:			path => OnReceivePath(type, index, path)
		);
	}
	
	private void OnReceivePath(TankType type, int index, Path path)
	{
		UnitFromType(type).units[index].path = path;
		UnitFromType(type).units[index].pathUpdateTime = Time.time + pathUpdateInterval;

		UnitFromType(type).units[index].hasRequestedPath = false;
	}
	
	public void Spawn(TankType type, Vector3 position)
	{
		TankUnit unit = UnitFromType(type);
		int index = unit.nextIndex;
		unit.nextIndex++;

		var tank = unit.units[index].tank;
		tank.GetComponent<Breakable>().OnBreak += () => UnSpawn(type, index);
		tank.OnCollideBreakable += breakable => AddTargetBreakable(type, index, breakable);
		tank.collisionMask = tankCollisionMask;

		StartCoroutine(UnearthTank(tank, position, type, index));
	}

	private IEnumerator UnearthTank(Tank tank, Vector3 position, TankType type, int index)
	{
		// Telegraph enemy spawning to player
		Instantiate(spawnEffect, position, spawnEffect.transform.rotation);
		float waitTime = spawnEffect.main.duration - riseFromUnderworldTime;
		yield return new WaitForSeconds(waitTime);
		
		// Rise
		position.y = -tank.height;
		tank.transform.position = position;
		tank.gameObject.SetActive(true);
		tank.collider.enabled = false;

		float percent = 0f;
		while (percent < 1f)
		{
			percent += Time.deltaTime / riseFromUnderworldTime;
			position.y = (percent - 1f) * tank.height; // minimized lerp
			tank.transform.position = position;
			yield return null;
		}
		
		// Active after pondering a moment
		yield return new WaitForSeconds(decisionMakeDelay);

		tank.collider.enabled = true;
		
		var unit = UnitFromType(type);
		unit.activeIndicesMap[unit.activeCount++] = index;
		Sort(unit.activeIndicesMap, unit.activeCount);
	}

	private void UnSpawn(TankType type, int index)
	{
		var tank = UnitFromType(type).units[index];
		tank.tank.gameObject.SetActive(false);
		tank.path = null;
		tank.targetBreakable = null;

		OnTankDestroyed?.Invoke();

		// Swap with last active, and sort those left
		int activeIndex = GetActiveIndex(type, index);
		var unit = UnitFromType(type);
		unit.activeCount--;
		unit.activeIndicesMap[activeIndex] = unit.activeIndicesMap[unit.activeCount];
		Sort(unit.activeIndicesMap, unit.activeCount);
	}

	private int GetActiveIndex(TankType type, int unitIndex)
	{
		var unit = UnitFromType(type);
		for (int i = 0; i < unit.activeCount; i++)
		{
			if (unit.activeIndicesMap[i] == unitIndex)
			{
				return i;
			}
		}

		return -1;
	}
	
	
	private TankUnit UnitFromType(TankType type)
	{
		switch (type)
		{
			case TankType.Hunter: return hunters;
			case TankType.Pummel: return pummels;
		}

		return null;
	}


	public void Stop()
	{
		enabled = false;
		
		UnloadUnit(hunters);
		hunters = null;
		
		UnloadUnit(pummels);
		pummels = null;
	}

	private void UnloadUnit(TankUnit unit)
	{
		for (int i = 0; i < unit.units.Length; i++)
		{
			Destroy(unit.units[i].tank.gameObject);
		}
	}
	
	private static void Sort(int[] array, int count)
	{
		for (int i = 0; i < count; i++)
		{
			for (int j = i; j < count; j++)
			{
				if (array[j] < array[i])
				{
					int temp = array[j];
					array[j] = array[i];
					array[i] = temp;
				}
			}
		}
	}
}