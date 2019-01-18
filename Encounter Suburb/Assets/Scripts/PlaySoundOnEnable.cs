using UnityEngine;

public class PlaySoundOnEnable : MonoBehaviour
{
	public AudioClip clip;

	[Range(0f, 1f)] public float volume = 1f;
	
	private void OnEnable()
	{
		if (gameObject.activeInHierarchy)
		{
			AudioSource.PlayClipAtPoint(clip, transform.position, volume);
		}
	}
}
