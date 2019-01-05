using UnityEngine;

[CreateAssetMenu]
public class Hull : ScriptableObject
{
	public float forwardSpeed = 10f;
	public float backwardSpeed = 5f;

	public float steerMovingSpeed = 45f;
	public float steerStationarySpeed = 90f;
}