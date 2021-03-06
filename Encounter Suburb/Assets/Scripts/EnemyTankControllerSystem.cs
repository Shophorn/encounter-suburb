using System;
using System.Collections;
using PathFinding;
using UnityEngine;
using Random = UnityEngine.Random;


public class EnemyTankControllerSystem : MonoBehaviour
{
	[Obsolete("Don't use system's Transform", true)]
	private new Transform transform;

	[SerializeField] private EnemyTankBehaviour hunterBehaviour;
	[SerializeField] private EnemyTankBehaviour pummelBehaviour;
	[SerializeField] private EnemyTankBehaviour heavyBehaviour;

	[SerializeField] private TankTypeArray<EnemyTankBehaviour> behaviours;

	[SerializeField] private LayerMask tankCollisionMask;
	[SerializeField] private float pathUpdateInterval = 0.25f;
	[SerializeField] private float riseFromUnderworldTime = 1f;
	[SerializeField] private float decisionMakeDelay = 0.25f;
	[SerializeField] private ParticleSystem spawnEffect;

	private TankUnitArray units;
	[NonSerialized] public ColorHSV tankBaseColor;
	
	
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

		behaviours = new TankTypeArray<EnemyTankBehaviour>
		{
			[TankType.Hunter] = hunterBehaviour,
			[TankType.Pummel] = pummelBehaviour,
			[TankType.Heavy] = heavyBehaviour,
		};
		
		enabled = true;
	}

	private void Update()
	{
		UpdateUnit(TankType.Hunter);
		UpdateUnit(TankType.Pummel);
		UpdateUnit(TankType.Heavy);
	}

	private void UpdateUnit(TankType type)
	{
		var unit = units[type];
		var playerPosition = playerTransform.position;
		
		for (int i = 0; i < unit.activeCount; i++)
		{
			// use this to reference anything, i points to different thing
			int index = unit.activeIndicesMap[i];
			
			var instance = unit.instances[index];
			var tankPosition = instance.tank.transform.position;
			float sqrDistanceToPlayer = (playerPosition - tankPosition).sqrMagnitude;

			Vector3 targetPosition;
			float sqrDistanceToTarget;

			switch (behaviours[type].preferredTarget)
			{
				case EnemyTankBehaviour.PreferredTarget.Player:
					targetPosition = playerPosition;
					sqrDistanceToTarget = sqrDistanceToPlayer;
					break;
				case EnemyTankBehaviour.PreferredTarget.Base:
					targetPosition = playerBasePosition;
					sqrDistanceToTarget = (playerBasePosition - tankPosition).sqrMagnitude;
					break;
				default:
				{
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

					break;
				}
			}
			
			// Follow Path
			if (instance.path == null)
			{
				if (!instance.hasRequestedPath)
					RequestPath(unit, index, targetPosition);
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
					unit.instances[index].path = null;
					continue;
				}

				wayPoint = instance.path.currentPoint;
				toWayPoint = wayPoint - instance.tank.transform.position;
			}

			// Stay still if there is breakable target on the way
			float maxMoveMagnitude = instance.targetBreakable != null ? 0f : 1f;

			var driveVector = Vector3.ClampMagnitude(toWayPoint, maxMoveMagnitude);
			instance.tank.Drive(driveVector);
			
			// TODO: explicitly set player, block or base as target
			// Aim
			if (instance.targetBreakable != null)
			{
				targetPosition = instance.targetBreakable.transform.position;
				sqrDistanceToTarget = (tankPosition - targetPosition).sqrMagnitude;
			}
			instance.tank.AimGunAt(targetPosition);

			// Shoot, disable colliders before, so we don't hit ourselves
			instance.tank.collider.enabled = false;
			if (instance.shootEnumerator == null || !instance.shootEnumerator.MoveNext())
			{
				Vector3 toTarget = targetPosition - tankPosition;

				const float inSightDotThreshold = 0.95f;
				bool inSight = Vector3.Dot(toTarget, instance.tank.gunForward) > inSightDotThreshold;
				bool inRange = sqrDistanceToTarget < instance.tank.gun.projectile.sqrMaxRange;

				instance.shootEnumerator = 
					inRange && inSight ? 
						instance.tank.gun.FireBurst() : 
						null;
			}
			// Done shooting, re-enable collider
			instance.tank.collider.enabled = true;
			
			// instance is struct re-set
			unit.instances[index] = instance;
		}
	}

	private TankUnit CreateTankUnit(EnemyTankBehaviour behaviour, int count)
	{
		var instances = new TankInstance[count];
		for (int i = 0; i < count; i++)
		{
			instances[i] = new TankInstance(Instantiate(behaviour.prefab));
			instances[i].tank.gameObject.SetActive(false);

			const float colorRandomRange = 0.8f;
			var hsv = tankBaseColor;
			hsv.value += Random.value * colorRandomRange - colorRandomRange / 2f;
			hsv.saturation += Random.value * colorRandomRange - colorRandomRange / 2f;

			var renderers = instances[i].tank.GetComponentsInChildren<MeshRenderer>();
			for (int ii = 0; ii < renderers.Length; ii++)
			{
				renderers[ii].material.color = hsv;
			}
		}

		return new TankUnit
		{
			behaviour = behaviour,
			instances = instances,
			activeIndicesMap = new int[count],
			activeCount = 0,
			nextIndex = 0
		};
	}

	private void AddTargetBreakable(TankType type, int index, Breakable target)
	{
		// Do not add another tank or player as target
		if (target.GetComponent<Tank>() != null || target.transform == playerTransform) return;
		units[type].instances[index].targetBreakable = target;
		
		Debug.Log($"{units[type].instances[index].tank.name}: Add target breakable");
	}

	private void RequestPath(TankUnit unit, int index, Vector3 target)
	{
		unit.instances[index].hasRequestedPath = true;
		
		PathRequestManager.RequestPath(
			start:				unit.instances[index].tank.transform.position,
			end:				target, 
			preferBreakWalls:	unit.behaviour.preferBreakWalls,
			callback:			path => OnReceivePath(unit, index, path)
		);
	}
	
	private void OnReceivePath(TankUnit unit, int index, Path path)
	{
		unit.instances[index].path = path;
		unit.instances[index].pathUpdateTime = Time.time + pathUpdateInterval;

		unit.instances[index].hasRequestedPath = false;
	}
	
	public void Spawn(TankType type, Vector3 position)
	{
		TankUnit unit = units[type];
		int index = unit.nextIndex;
		unit.nextIndex++;

		var tank = unit.instances[index].tank;
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

		var toBase = playerBasePosition - position;
		toBase.y = 0;
		var startRotation = Quaternion.LookRotation(-toBase);
		var endRotation = Quaternion.LookRotation(toBase);
		
		position.y = -tank.height;
		tank.transform.position = position;
		tank.transform.rotation = startRotation;
		tank.gameObject.SetActive(true);
		tank.collider.enabled = false;

		// Rise
		float percent = 0f;
		while (percent < 1f)
		{
			percent += Time.deltaTime / riseFromUnderworldTime;

			position.y = (percent - 1f) * tank.height; // minimized lerp
			tank.transform.position = position;
			
			tank.transform.rotation = Quaternion.Slerp(startRotation, endRotation, percent);
			
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
		var tank = units[type].instances[index];
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
		units.Unload();

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


