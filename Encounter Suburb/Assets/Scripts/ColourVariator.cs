using UnityEngine;

public class ColourVariator : MonoBehaviour
{
	public Gradient colours;

	public Renderer[] renderers;

	public void Awake()
	{
		var color = colours.Evaluate(Random.value);
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].material.color = color;
		}
	}
	
	public void Apply(float t)
	{
		return;
		var color = colours.Evaluate(t);
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].material.color = color;
		}
	}
}