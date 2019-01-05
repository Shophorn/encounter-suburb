using System;
using PathFinding;
using UnityEngine;

public class EnemyTankControllerSystem : MonoBehaviour
{
	[Obsolete("Do not use transform", true)]
	private new Transform transform;
	
	private int maxCount = 20;
	private int count = 0;

	private float pathUpdateInterval = 0.2f;

	public LayerMask tankCollisionMask;
	
	private Tank [] tanks;
	private Path [] paths;
	private float[] pathUpdateTimes;
	private Breakable[] targetBreakables;
 	
	public static Transform playerTransform;
	public static Vector3 playerBasePosition; 
	
	public float engageRange = 10f;
	public float preferredShootDistance = 5f;

	public Tank prefab;

	private static EnemyTankControllerSystem instance;

	public static event Action OnEnemyKilled;

	public float[] distances;
	
	private void Awake()
	{
		instance = this;
	}
	
	private void Start()
	{
		tanks = new Tank[maxCount];
		paths = new Path[maxCount];
		pathUpdateTimes = new float[maxCount];
		targetBreakables = new Breakable[maxCount];
		
		distances = new float[maxCount];
	}

	private void Update()
	{
		var playerPosition = playerTransform.position;
		
		for (int i = 0; i < count; i++)
		{
			if (!tanks[i].gameObject.activeInHierarchy) continue;

			var tankPosition = tanks[i].transform.position;
			if (paths[i] == null)
			{
				int index = i;
				PathRequestManager.RequestPath(tankPosition, playerTransform.position, (path) => instance.OnReceivePath(index, path));
				pathUpdateTimes[i] = Time.time + pathUpdateInterval;
				continue;
			}

			if (pathUpdateTimes[i] < Time.time)
			{
				int index = i;
				PathRequestManager.RequestPath(tankPosition, playerTransform.position, (path) => instance.OnReceivePath(index, path));
				pathUpdateTimes[i] = Time.time + pathUpdateInterval;
			}
			
			var wayPoint = paths[i].currentPoint;
			var toWayPoint = wayPoint - tankPosition;

			distances[i] = toWayPoint.sqrMagnitude;
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

			if (targetBreakables[i] != null)
			{
				tanks [i].turret.AimAt(targetBreakables[i].transform.position);
			}
			
			tanks[i].collider.enabled = false;
			tanks[i].gun.Fire();
			tanks[i].collider.enabled = true;			
		}
	}

	public static void Spawn(Vector3 position)
	{
		if (!instance.enabled) return;
		if (instance.count >= instance.maxCount - 1) return;

		int index = instance.count;
		instance.count++;
		
		var newTank = Instantiate(instance.prefab, position, Quaternion.identity);
		newTank.GetComponent<Breakable>().OnBreak += () => newTank.gameObject.SetActive(false);

		newTank.GetComponent<Breakable>().OnBreak += OnEnemyKilled;
		newTank.OnCollideBreakable += breakable => instance.AddTargetBreakable(index, breakable);

		newTank.collisionMask = instance.tankCollisionMask;
		
		instance.tanks[index] = newTank;
		
		PathRequestManager.RequestPath(position, playerTransform.position, path => instance.OnReceivePath(index, path));
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
	}

	private void OnDrawGizmos()
	{
		if (paths == null) return;
		
		for (int i = 0; i < paths.Length; i++)
		{
			for (int j = 0; j < paths[i].points.Length; j++)
			{
				Gizmos.color = Color.green;
				if (paths[i].currentIndex == j)
				{
					Gizmos.color = Color.red;
				}
				Gizmos.DrawSphere(paths[i].points[j], 0.5f);
				var dir = paths[i].directions[j];
				Gizmos.DrawRay(paths[i].points[j], new Vector3(dir.x, 0, dir.y) * 5f);
			}
		}
	}
}