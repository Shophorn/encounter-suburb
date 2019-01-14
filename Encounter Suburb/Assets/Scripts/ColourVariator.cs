using UnityEngine;

public class ColourVariator : MonoBehaviour
{
	public Gradient colours;
	
	public float changeValueRange;
	public bool changeValue;
	
	public Renderer[] renderers;

	public void Awake()
	{
		if (changeValue)
		{
			var value = 1f + Random.Range(0f, changeValueRange) - (changeValueRange / 2f);
			for (int i = 0; i < renderers.Length; i++)
			{
				var color = renderers[i].material.color * value;
				renderers[i].material.color = color;
			}
		}
		else
		{
			var color = colours.Evaluate(Random.value);
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].material.color = color;
			}
		}
		
		Debug.Log($"{gameObject.name}: Color varied");
		
		Destroy(this);
	}
}