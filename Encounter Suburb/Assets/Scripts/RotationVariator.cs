using UnityEngine;

public class RotationVariator : MonoBehaviour
{
	public int steps;
	
	private void Awake()
	{
		float angle = 360f / steps * Random.Range(0, steps);
		transform.Rotate(0, angle, 0, Space.World);
		
		Destroy(this);
	}
}