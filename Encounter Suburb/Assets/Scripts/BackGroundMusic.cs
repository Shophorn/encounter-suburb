using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackGroundMusic : MonoBehaviour
{
	public bool active;
	
	public readonly AudioClip None = null;
	public AudioClip Menu;
	public AudioClip Game;
	public AudioClip Defeat;
	public AudioClip Victory;

	private AudioSource source;
	
	private void Awake()
	{
		source = GetComponent<AudioSource>();
	}

	public void Play(AudioClip clip)
	{
		if (!active) return;
		
		source.clip = clip;
		source.Play();
	}
}