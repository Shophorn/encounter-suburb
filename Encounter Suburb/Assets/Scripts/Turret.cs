using System;
using UnityEngine;

[Serializable]
public class Turret
{
	public Transform transform;
	public Vector3 forward => transform.forward;
	public float turnSpeed = 90f;

	public void AimAt(Vector3 point)
	{
		var toPoint = point - transform.position;
		toPoint.y = 0f;

		var target = Quaternion.LookRotation(toPoint);
		transform.rotation =
			Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
	}
}