using UnityEngine;

public class PlayerTankController : MonoBehaviour
{
	public Transform aimTargetCursor;
	public Transform aimCurrentCursor;
	
	private readonly Vector3 cursorOffset = new Vector3(0f, 0.1f, 0f);
	public LayerMask groundMask;

	public Tank tank;

	private float muzzleOffset;

	private void Start()
	{
		var turretToMuzzle = tank.turret.transform.position - tank.gun.muzzle.position;
		turretToMuzzle.y = 0;
		muzzleOffset = turretToMuzzle.magnitude;
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

			tank.turret.AimAt(target);

			var targetAimVector = target - tank.turret.transform.position;
			targetAimVector.y = 0;
			float currentRange = Mathf.Min(targetAimVector.magnitude, tank.gun.type.projectile.maxRange) + muzzleOffset;
			aimCurrentCursor.position =
				tank.turret.transform.position + tank.turret.forward * currentRange + cursorOffset;
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
