using System.Collections.Generic;
using UnityEngine;

namespace PathFinding
{
	[System.Serializable]
	public class Path
	{
		public Vector3[] points;
		public Vector2[] directions;
		
		public int currentIndex { get; private set; } = 0;
		public Vector3 currentPoint => points[currentIndex];

		public bool MoveNext()
		{
			if (currentIndex + 1 == points.Length)
			{
				return false;
			}

			currentIndex++;
			return true;
		}
	}
}