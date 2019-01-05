using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu]
public class LevelInfo : ScriptableObject
{
	public Map map;
	public int count;
	public Material material;

	public Level Level()
	{
		return new Level()
		{
			map = map,
			count = count,
			material = material
		};
	}
}
