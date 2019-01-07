using System;
using PathFinding;
using UnityEngine;

public class EnemyTankControllerSystem : MonoBehaviour
{
	[Obsolete("Do not use transform", true)]
	private new Transform transform;
	
	private int maxCount;
	private int count = 0;

	private const float pathUpdateInterval = 0.2f;

	public Tank prefab;
	public LayerMask tankCollisionMask;
	public ParticleSystem explosion;
	
	private Tank [] tanks;
	private Path [] paths;
	private float[] pathUpdateTimes;
	private Breakable[] targetBreakables;
 	
	public Transform playerTransform { get; set; }
	public Vector3 playerBasePosition { get; set; }

	public event Action OnTankDestroyed;

	public void Begin(int maxCount)
	{
		this.maxCount = maxCount;
		count = 0;
		
		tanks = new Tank[maxCount];
		paths = new Path[maxCount];
		pathUpdateTimes = new float[maxCount];
		targetBreakables = new Breakable[maxCount];

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

		for (int i = 0; i < count; i++)
		{
			if (!tanks[i].gameObject.activeInHierarchy) continue;

			var tankPosition = tanks[i].transform.position;
			float sqrDistanceToPlayer = (playerPosition - tankPosition).sqrMagnitude;

			Vector3 targetPosition;
			float sqrDistanceToTarget;
			
			if (sqrDistanceToPlayer < tanks[i].sqrEngageRange)
			{
				targetPosition = playerPosition;
				sqrDistanceToTarget = sqrDistanceToPlayer;
			}
			else
			{
				targetPosition = playerBasePosition;
				sqrDistanceToTarget = (tankPosition - playerBasePosition).sqrMagnitude;
			}
			
			if (paths[i] == null)
			{
				int index = i;
				PathRequestManager.RequestPath(tankPosition, targetPosition, (path) => OnReceivePath(index, path));
				continue;
			}

			if (pathUpdateTimes[i] < Time.time)
			{
				int index = i;
				PathRequestManager.RequestPath(tankPosition, targetPosition, (path) => OnReceivePath(index, path));
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
			Debug.DrawRay(tanks[i].transform.position + Vector3.up * 0.5f, driveVector * 5f, Color.cyan);
			
			tanks[i].Drive(driveVector);

			// Aim and Shoot
			bool doShoot = false;
			if (targetBreakables[i] != null)
			{
				tanks [i].turret.AimAt(targetBreakables[i].transform.position);
				doShoot = true;
			}
			else
			{
				tanks[i].turret.AimAt(targetPosition);
				const float shootDotThreshold = 0.95f;
				Vector3 toTarget = targetPosition - tankPosition;

				bool inRange = sqrDistanceToTarget < tanks[i].gun.type.projectile.sqrMaxRange;
				bool inSight = Vector3.Dot(toTarget, tanks[i].turret.forward) > shootDotThreshold;

				doShoot = inRange && inSight;
			}

			if (doShoot)
			{
				tanks[i].collider.enabled = false;
				tanks[i].gun.Fire();
				tanks[i].collider.enabled = true;
			}
		}
	}
	
	public void Spawn(Vector3 position)
	{
		if (!enabled) return;
		if (count >= maxCount - 1) return;

		int index = count;
		count++;
		
		tanks[index] = Instantiate(prefab, position, Quaternion.identity);
		tanks[index].GetComponent<Breakable>().OnBreak += () => OnDestroyed(index);
		tanks[index].OnCollideBreakable += breakable => AddTargetBreakable(index, breakable);
		tanks[index].collisionMask = tankCollisionMask;
		
		PathRequestManager.RequestPath(position, playerTransform.position, path => OnReceivePath(index, path));
	}

	private void OnDestroyed(int index)
	{
		tanks[index].gameObject.SetActive(false);
		OnTankDestroyed.Invoke();

		Instantiate(explosion, tanks[index].transform.position, Quaternion.identity);
	}
	
	private void AddTargetBreakable(int index, Breakable target)
	{
		targetBreakables[index] = target;
		target.OnBreak += () => RemoveTargetBreakable(index);
	}

	private void RemoveTargetBreakable(int index)
	{
		targetBreakables[index] = null;
	}
	
	private void OnReceivePath(int index, Path path)
	{
		paths[index] = path;
		pathUpdateTimes[index] = Time.time + pathUpdateInterval;
	}

	private void Clear()
	{
		for (int i = 0; i < count; i++)
		{
			Destroy(tanks[i].gameObject);
		}

		tanks = null;
		paths = null;
		pathUpdateTimes = null;
		targetBreakables = null;
	}
}