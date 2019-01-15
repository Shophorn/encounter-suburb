using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class ComponentPool<T> where T : Component
{
	private List<T> items;
	private T prototype;
	
	public ComponentPool(int capacity, T prototype)
	{
		this.prototype = prototype;
		
		items = new List<T>();
		for (int i = 0; i < capacity; i++)
		{
			items.Add(Object.Instantiate(prototype));
			items[i].gameObject.SetActive(false);
		}
	}

	// Get enabled item
	public T GetActive()
	{
		// Get first disabled, and activate it
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].gameObject.activeInHierarchy) continue;
			
			items[i].gameObject.SetActive(true);
			return items[i];
		}

		items.Add(Object.Instantiate(prototype));
		return items[items.Count - 1];
	}

}
