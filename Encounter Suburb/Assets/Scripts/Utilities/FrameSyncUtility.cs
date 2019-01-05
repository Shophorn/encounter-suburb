using System;
using System.Collections.Generic;
using UnityEngine;

public class FrameSyncUtility : MonoBehaviour
{
	private static FrameSyncUtility instance;

	private readonly Queue<Action> updateQueue = new Queue<Action>();
	
	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy(this);
		}
	}

	public static void SyncUpdate(Action action)
	{
		instance.updateQueue.Enqueue(action);
	}

	private void Update()
	{
		while (updateQueue.Count > 0)
		{
			updateQueue.Dequeue().Invoke();
		}
	}
}