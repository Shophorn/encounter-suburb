using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[ExecuteInEditMode]
public class DisablePostProcessingInEditMode : MonoBehaviour
{
	public PostProcessLayer layer;
	public bool disableLayerInEditMode;

	public Component component;
	
	private void Awake()
	{
		if (Application.isPlaying)
		{
			layer.enabled = true;
		}
	}
	
	private void OnValidate()
	{
		if (disableLayerInEditMode && !Application.isPlaying)
		{
			layer.enabled = false;
		}
	}
}