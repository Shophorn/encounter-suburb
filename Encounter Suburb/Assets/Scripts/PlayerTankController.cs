using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerTankController : MonoBehaviour
{
	public Transform aimTargetCursor;
	
	private readonly Vector3 cursorOffset = new Vector3(0f, 0.1f, 0f);
	public LayerMask groundMask;

//	public PlayerTank tank;

	private IEnumerator shootEnumerator;
	
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
	
	private void Update()
	{
		var input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
		var drive = Vector3.ClampMagnitude(input, 1f);
		Drive(drive);
		
		// Aim
		var aimRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hitInfo;
		if (Physics.Raycast(aimRay, out hitInfo, groundMask))
		{
			var target = hitInfo.point;
			aimTargetCursor.position = target + cursorOffset;

			AimTurretAt(target);
		}

		// Shoot
		collider.enabled = false;
		if (shootEnumerator == null || !shootEnumerator.MoveNext())
		{
			shootEnumerator = Input.GetButton("Fire1") ? gun.FireBurst() : null;
		}
		collider.enabled = true;
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
