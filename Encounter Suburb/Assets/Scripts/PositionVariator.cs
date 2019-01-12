using UnityEngine;

public class PositionVariator : MonoBehaviour
{
	public Vector3 range;

	private void Awake()
	{
		transform.position += new Vector3(
			range.x * (Random.value * 2 - 1),
			range.y * (Random.value * 2 - 1),
			range.z * (Random.value * 2 - 1)
		);
		
		Destroy(this);
	}
}