using System;
using System.Collections;
using System.Collections.Generic;
using PathFinding;
using UnityEngine;
using Array = UnityScript.Lang.Array;

public enum TankType { Hunter, Pummel, Heavy }

public class EnemyTankControllerSystem : MonoBehaviour
{
	[System.Obsolete("Don't use system's Transform", true)]
	private new Transform transform;

	[SerializeField] private EnemyTankBehaviour hunterBehaviour;
	[SerializeField] private EnemyTankBehaviour pummelBehaviour;
	[SerializeField] private EnemyTankBehaviour heavyBehaviour;

	[SerializeField] private LayerMask tankCollisionMask;
	[SerializeField] private float pathUpdateInterval = 0.25f;
	[SerializeField] private float riseFromUnderworldTime = 1f;
	[SerializeField] private float decisionMakeDelay = 0.25f;
	[SerializeField] private ParticleSystem spawnEffect;

	private TankUnitArray units;
	
	// Target candidates
	[NonSerialized] public Transform playerTransform;
	[NonSerialized] public Vector3 playerBasePosition;

	public event Action OnTankDestroyed;
	

	public void Begin(int hunterCount, int pummelCount, int heavyCount)
	{
		units = new TankUnitArray
		{
			[TankType.Hunter] = CreateTankUnit(hunterBehaviour, hunterCount),
			[TankType.Pummel] = CreateTankUnit(pummelBehaviour, pummelCount),
			[TankType.Heavy] = CreateTankUnit(heavyBehaviour, heavyCount)
		};


		enabled = true;
	}

	private void Update()
	{
		UpdateUnitOfType(TankType.Hunter);
		UpdateUnitOfType(TankType.Pummel);
		UpdateUnitOfType(TankType.Heavy);
	}

	private void UpdateUnitOfType(TankType type)
	{
		var unit = units[type];
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

			var gunForward = instance.tank.fixedTurret ? instance.tank.transform.forward : instance.tank.turretForward; 
			
			bool inRange = sqrDistanceToTarget < instance.tank.gun.type.projectile.sqrMaxRange;
			bool inSight = Vector3.Dot(toTarget, gunForward) > shootDotThreshold;

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
		units[type].units[index].targetBreakable = target;
	}

	// TODO: Only 1 usage, remove
	private void RequestPath(TankType type, int index, Vector3 target)
	{
		TankUnit unit = units[type];
		unit.units[index].hasRequestedPath = true;
		
		PathRequestManager.RequestPath(
			start:				unit.units[index].tank.transform.position,
			end:				target, 
			preferBreakWalls:	unit.behaviour.preferBreakWalls,
			callback:			path => OnReceivePath(type, index, path)
		);
	}
	
	private void OnReceivePath(TankType type, int index, Path path)
	{
		units[type].units[index].path = path;
		units[type].units[index].pathUpdateTime = Time.time + pathUpdateInterval;

		units[type].units[index].hasRequestedPath = false;
	}
	
	public void Spawn(TankType type, Vector3 position)
	{
		TankUnit unit = units[type];
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
		
		var unit = units[type];
		unit.activeIndicesMap[unit.activeCount++] = index;
		Sort(unit.activeIndicesMap, unit.activeCount);
	}

	private void UnSpawn(TankType type, int index)
	{
		var tank = units[type].units[index];
		tank.tank.gameObject.SetActive(false);
		tank.path = null;
		tank.targetBreakable = null;

		OnTankDestroyed?.Invoke();

		// Swap with last active, and sort those left
		int activeIndex = GetActiveIndex(type, index);
		var unit = units[type];
		unit.activeCount--;
		unit.activeIndicesMap[activeIndex] = unit.activeIndicesMap[unit.activeCount];
		Sort(unit.activeIndicesMap, unit.activeCount);
	}

	private int GetActiveIndex(TankType type, int unitIndex)
	{
		var unit = units[type];
		for (int i = 0; i < unit.activeCount; i++)
		{
			if (unit.activeIndicesMap[i] == unitIndex)
			{
				return i;
			}
		}

		return -1;
	}
	
	public void Stop()
	{
		enabled = false;
		
		StopAllCoroutines();

		for (int i = 0; i < units.count; i++)
		{
			// Unload
			for (int ii = 0; ii < units[i].units.Length; ii++)
			{
				Destroy(units[i].units[ii].tank.gameObject);
			}
		}

		units = null;
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

public class TankUnitArray
{
	public static readonly int Count = Enum.GetNames(typeof(TankType)).Length;
	public int count => Count;
	
	private readonly TankUnit[] units = new TankUnit[Count];

	public TankUnit this[TankType type]
	{
		get { return units[(int) type]; }
		set { units[(int) type] = value; }
	}

	public TankUnit this[int index]
	{
		get { return units[index]; }
		set { units[index] = value; }
	}
}
