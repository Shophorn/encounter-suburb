using System;
using UnityEngine;

public class Tank : MonoBehaviour
{	
	public Hull hull;
	public Turret turret = null;
	public Gun gun;

	public new BoxCollider collider;

	private Vector3[] collisionRayPoints;
	public LayerMask collisionMask;

	public event Action<Breakable> OnCollideBreakable;

	public float engageRange = 5f;
	public float sqrEngageRange { get; private set; }
	
	public float preferredShootDistance = 3f;
	public float sqrPreferredShootDistance { get; private set; }
	
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

		sqrEngageRange = engageRange * engageRange;
		sqrPreferredShootDistance = preferredShootDistance * preferredShootDistance;
	}

	/// <summary>
	/// Move tank. Params 'drive' and 'steer' in range (-1.0f ... 1.0f)
	/// </summary>
	public void Drive(float drive, float steer)
	{
		float moving = Mathf.Abs(drive);

		drive *= (drive < 0f ? hull.backwardSpeed : hull.forwardSpeed) * Time.deltaTime;
		steer *= Mathf.Lerp(hull.steerStationarySpeed, hull.steerMovingSpeed, moving) * Time.deltaTime;

		// Drive in two steps to better approximate turning curve
		float halfDrive = drive * 0.5f;
		transform.Translate(Collide(halfDrive) * Vector3.forward, Space.Self);
		transform.Rotate(transform.up, steer, Space.Self);
		transform.Translate(Collide(halfDrive) * Vector3.forward, Space.Self);
	}
	
	public void Drive(Vector3 input)
	{
		const float driveDotThreshold = 0.95f;
		const float magnitudeThreshold = 0.01f;
		
		float magnitude = input.magnitude;
		if (magnitude < magnitudeThreshold) return;

		float dot = Vector3.Dot(transform.forward, input / magnitude);
		float drive = dot < driveDotThreshold ? 0f : (magnitude * hull.forwardSpeed * Time.deltaTime);
		transform.Translate(Vector3.forward * Collide(drive), Space.Self);

		var turretRotation = turret.transform.rotation;
		
		Quaternion targetRotation = Quaternion.LookRotation(input, transform.up);
		transform.rotation = 
			Quaternion.RotateTowards(transform.rotation, targetRotation, hull.steerStationarySpeed * Time.deltaTime);


		turret.transform.rotation = turretRotation;
	}

	private float Collide(float drive)
	{
		const float skinWidth = 0.01f;
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
			
//			Debug.DrawRay(transform.TransformPoint(collisionRayPoints[i]), transform.forward, Color.cyan);
		}

		return drive - skinWidth;
	}
	
}
