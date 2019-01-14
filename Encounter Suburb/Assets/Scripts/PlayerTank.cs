using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerTank : MonoBehaviour
{
	public TankSpecs specs;
	public Gun gun;

	public new BoxCollider collider { get; private set; }

	private new Rigidbody rigidbody;
	
	[Header("Turret")]
	public Transform turretTransform;
	public Transform muzzle;
	
	private void Awake()
	{
		collider = GetComponent<BoxCollider>();
		rigidbody = GetComponent<Rigidbody>();

		GetComponent<Breakable>().OnBreak += () => gameObject.SetActive(false);

		gun = specs.CreateGun(muzzle);
	}
	
	public void Drive(Vector3 input)
	{
		const float driveDotThreshold = 0.95f;
		const float magnitudeThreshold = 0.01f;
		
		float magnitude = input.magnitude;
		if (magnitude < magnitudeThreshold) return;

		float dot = Vector3.Dot(transform.forward, input / magnitude);
		float drive = dot < driveDotThreshold ? 0f : (magnitude * specs.moveSpeed * Time.deltaTime);
		rigidbody.MovePosition(transform.position + transform.forward * drive);

		// Use this to keep turret in place
		var turretRotation = turretTransform.rotation;
		
		Quaternion targetRotation = Quaternion.LookRotation(input, transform.up);
		rigidbody.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation, specs.rotationSpeed * Time.deltaTime));
		turretTransform.rotation = turretRotation;
	}

	public void AimTurretAt(Vector3 point)
	{
		var toPoint = point - turretTransform.position;
		toPoint.y = 0f;

		var target = Quaternion.LookRotation(toPoint);
		turretTransform.rotation = target;
	}
	
}
