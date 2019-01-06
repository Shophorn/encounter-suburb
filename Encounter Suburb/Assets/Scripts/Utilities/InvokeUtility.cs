using System;
using System.Collections;
using UnityEngine;

public static class InvokeUtility
{
	public static void Invoke(this MonoBehaviour host, float time, Action function)
	{
		host.StartCoroutine(WaitForInvoke(time, function));
	}

	private static IEnumerator WaitForInvoke(float time, Action function)
	{
		yield return new WaitForSeconds(time);
		function();
	}
}