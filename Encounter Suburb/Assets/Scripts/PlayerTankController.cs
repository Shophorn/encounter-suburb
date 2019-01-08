using UnityEngine;

public class PlayerTankController : MonoBehaviour
{
	public Transform aimTargetCursor;
	public Transform aimCurrentCursor;
	
	private readonly Vector3 cursorOffset = new Vector3(0f, 0.1f, 0f);
	public LayerMask groundMask;

	public Tank tank;
	public ParticleSystem explosion;

	private float muzzleOffset;

	private void Start()
	{
		var turretToMuzzle = tank.turretTransform.position - tank.gun.muzzle.position;
		turretToMuzzle.y = 0;
		muzzleOffset = turretToMuzzle.magnitude;

		GetComponent<Breakable>().OnBreak += () => Instantiate(explosion, transform.position, Quaternion.identity);
	}

	private void Update()
	{
		tank.Drive(Vector3.ClampMagnitude(new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")), 1f));
		
		// Aim
		var aimRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hitInfo;
		if (Physics.Raycast(aimRay, out hitInfo, groundMask))
		{
			var target = hitInfo.point;
			aimTargetCursor.position = target + cursorOffset;

			tank.AimTurretAt(target);

			var targetAimVector = target - tank.turretTransform.position;
			targetAimVector.y = 0;
			float currentRange = Mathf.Min(targetAimVector.magnitude, tank.gun.type.projectile.maxRange) + muzzleOffset;
			aimCurrentCursor.position =
				tank.turretTransform.position + tank.turretForward * currentRange + cursorOffset;
		}

		// Shoot
		tank.collider.enabled = false;
		if (tank.gun.type.autoFire)
		{
			if (Input.GetButton("Fire1"))
				tank.gun.Fire();
		}
		else
		{
			if (Input.GetButtonDown("Fire1"))
				tank.gun.Fire();
		}
		tank.collider.enabled = true;
	}
}
