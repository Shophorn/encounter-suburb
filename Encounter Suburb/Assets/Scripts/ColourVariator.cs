using UnityEngine;

public class ColourVariator : MonoBehaviour
{
	public Gradient colours;

	public Renderer[] renderers;	
	
	public void Apply(float t)
	{
		var color = colours.Evaluate(t);
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].material.color = color;
		}
	}
}