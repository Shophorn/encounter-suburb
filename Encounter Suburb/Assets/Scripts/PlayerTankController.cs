using UnityEngine;

public class PlayerTankController : MonoBehaviour
{
	public Transform aimTargetCursor;
	
	private readonly Vector3 cursorOffset = new Vector3(0f, 0.1f, 0f);
	public LayerMask groundMask;

	public PlayerTank tank;

	private void Update()
	{
		var input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
		var drive = Vector3.ClampMagnitude(input, 1f);
		tank.Drive(drive);
		
		// Aim
		var aimRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hitInfo;
		if (Physics.Raycast(aimRay, out hitInfo, groundMask))
		{
			var target = hitInfo.point;
			aimTargetCursor.position = target + cursorOffset;

			tank.AimTurretAt(target);
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
