using UnityEngine;

public class DisableInPlayMode : MonoBehaviour
{
	private void Awake()
	{
		gameObject.SetActive(false);
	}
}