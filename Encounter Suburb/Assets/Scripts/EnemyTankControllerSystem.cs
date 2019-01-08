using System;
using System.Linq;
using PathFinding;
using UnityEngine;

public enum TankType { Hunter, Pummel, Heavy }

public class EnemyTankControllerSystem : MonoBehaviour
{
	[Obsolete("Do not use transform", true)]
	private new Transform transform;
	
	private int maxCount;
	private int count = 0;

	[SerializeField] private float pathUpdateInterval = 0.2f;
	[SerializeField] private EnemyTankBehaviour[] behaviours;
	private Tank[] prefabs;
	
	public LayerMask tankCollisionMask;
	public ParticleSystem explosion;
	
	private Tank [] tanks;
	private TankType[] types;
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
		types = new TankType[maxCount];
		paths = new Path[maxCount];
		pathUpdateTimes = new float[maxCount];
		targetBreakables = new Breakable[maxCount];

		prefabs = behaviours.Select(b => b.prefab).ToArray();
		
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

			int type = (int)types[i];

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
			
			if (paths[i] == null)
			{
				int index = i;
				PathRequestManager.RequestPath(tankPosition, targetPosition, behaviours[type].preferBreakWalls, (path) => OnReceivePath(index, path));
				continue;
			}

			if (pathUpdateTimes[i] < Time.time)
			{
				int index = i;
				PathRequestManager.RequestPath(tankPosition, targetPosition, behaviours[type].preferBreakWalls, (path) => OnReceivePath(index, path));
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
				tanks [i].AimTurretAt(targetBreakables[i].transform.position);
				doShoot = true;
			}
			else
			{
				tanks[i].AimTurretAt(targetPosition);
				const float shootDotThreshold = 0.95f;
				Vector3 toTarget = targetPosition - tankPosition;

				bool inRange = sqrDistanceToTarget < tanks[i].gun.type.projectile.sqrMaxRange;
				bool inSight = Vector3.Dot(toTarget, tanks[i].turretForward) > shootDotThreshold;

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
	
	public void Spawn(Vector3 position, TankType type)
	{
		if (!enabled) return;
		if (count >= maxCount - 1) return;

		int index = count;
		count++;

		tanks[index] = Instantiate(prefabs[(int)type], position, Quaternion.identity);
		tanks[index].GetComponent<Breakable>().OnBreak += () => OnDestroyed(index);
		tanks[index].OnCollideBreakable += breakable => AddTargetBreakable(index, breakable);
		tanks[index].collisionMask = tankCollisionMask;
		
		PathRequestManager.RequestPath(
			start:				position, 
			end: 				playerTransform.position, 
			preferBreakWalls: 	behaviours[(int)type].preferBreakWalls, 
			callback: 			path => OnReceivePath(index, path)
		);
	}

	private void OnDestroyed(int index)
	{
		tanks[index].gameObject.SetActive(false);
		paths[index] = null;
		
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
		types = null;
		paths = null;
		pathUpdateTimes = null;
		targetBreakables = null;
	}

	private void OnDrawGizmosSelected()
	{
		if (paths == null || !Application.isPlaying) return;
		
		Gizmos.color = Color.green;
		for (int i = 0; i < paths.Length; i++)
		{
			if (paths[i] == null) continue;
			
			Gizmos.DrawSphere(paths[i].points[0] + Vector3.up * 0.5f, 0.15f);
			
			for (int j = 1; j < paths[i].points.Length; j++)
			{
				var a = paths[i].points[j - 1] + Vector3.up * 0.5f;
				var b = paths[i].points[j] + Vector3.up * 0.5f;
				Gizmos.DrawSphere(paths[i].points[i] + Vector3.up * 0.5f, 0.15f);
				Gizmos.DrawLine(a,b);
			}
		}
	}
}