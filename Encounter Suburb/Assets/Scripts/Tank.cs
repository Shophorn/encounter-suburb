using System;
using UnityEngine;

public class Tank : MonoBehaviour
{
	// data
	private Vector3[] collisionRayPoints;
	public LayerMask collisionMask;
	public TankSpecs specs;
	public bool fixedTurret;
	public Gun gun;
	public float height; // For spawning enemytank from below ground

	// state
	public new BoxCollider collider;
	public event Action<Breakable> OnCollideBreakable;
	
		
	[Header("Turret")]
	public Transform turretTransform;
	public Vector3 turretForward => turretTransform.forward;
	public Quaternion turretRotation;
	
	private void Awake()
	{
		collider = GetComponent<BoxCollider>();

		// TODO: We don't probably need this many rows, columns are more important
		const int collisionRayCount = 4;
		collisionRayPoints = new Vector3[collisionRayCount * collisionRayCount];
		
		var min = transform.InverseTransformPoint(collider.bounds.min);
		var max = transform.InverseTransformPoint(collider.bounds.max);

		for (int i= 0, y = 0; y < collisionRayCount; y++)
		{
			float yy = Mathf.Lerp(min.y, max.y, (float) y / (collisionRayCount - 1));
			for (int x = 0; x < collisionRayCount; x++, i++)
			{
				float xx = Mathf.Lerp(min.x, max.x, (float) x / (collisionRayCount - 1));
				collisionRayPoints[i] = new Vector3(xx, yy, max.z); 
			}
		}
	}
	
	public void Drive(Vector3 input)
	{
		const float driveDotThreshold = 0.95f;
		const float magnitudeThreshold = 0.01f;
		
		float magnitude = input.magnitude;
		if (magnitude < magnitudeThreshold) return;

		float dot = Vector3.Dot(transform.forward, input / magnitude);
		float drive = dot < driveDotThreshold ? 0f : (magnitude * specs.moveSpeed * Time.deltaTime);
		transform.Translate(Vector3.forward * Collide(drive), Space.Self);

		Quaternion targetRotation = Quaternion.LookRotation(input, transform.up);
		transform.rotation = 
			Quaternion.RotateTowards(transform.rotation, targetRotation, specs.rotationSpeed * Time.deltaTime);

		if (!fixedTurret)
		{
			turretTransform.rotation = turretRotation;
		}
	}

	private float Collide(float drive)
	{
		// Add skinwidth to drive ray length, to collide before actual contact
		const float skinWidth = 1f;
		drive += skinWidth;
		
		RaycastHit hitInfo;
		for (int i = 0; i < collisionRayPoints.Length; i++)
		{
			bool hit = Physics.Raycast(
				new Ray(transform.TransformPoint(collisionRayPoints[i]), transform.forward),
				out hitInfo,
				drive,
				collisionMask
			);

			if (hit && hitInfo.distance < drive)
			{
				drive = hitInfo.distance;

				var breakable = hitInfo.collider.GetComponent<Breakable>();
				if (breakable != null)
				{
					OnCollideBreakable?.Invoke(breakable);
				}
			}
		}
		
		// Remove skinwidth, but as drive now may be smaller than that, limit to zero to not drive backwards
		return Mathf.Max(0f, drive - skinWidth);
	}
	
	public void AimTurretAt(Vector3 point)
	{
		if (fixedTurret)
		{
			var toPoint = point - transform.position;
			toPoint.y = 0;
			var targetRotation = Quaternion.LookRotation(toPoint);
			
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, specs.rotationSpeed * Time.deltaTime);
		}
		else
		{
			var toPoint = point - turretTransform.position;
			toPoint.y = 0f;
			var target = Quaternion.LookRotation(toPoint);
			
			turretTransform.rotation =
				Quaternion.RotateTowards(turretTransform.rotation, target, specs.turretTurnSpeed * Time.deltaTime);
			turretRotation = turretTransform.rotation;
		}
	}
	
}

public struct TankInstance
{
	public readonly Tank tank;
	public PathFinding.Path path;
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
public class TankUnit
{
	public EnemyTankBehaviour behaviour;
	public TankInstance[] units;
	public int[] activeIndicesMap;
	public int activeCount;
	public int nextIndex;
}