using System;
using System.Collections.Generic;
using UnityEngine;

namespace PathFinding
{

	public static class PathRequestManager
	{
		private struct PathRequest
		{
			public Vector3 start;
			public Vector3 end;
			public Action<Path> callBack;
		}
		
		private static readonly Queue<PathRequest> pathRequests = new Queue<PathRequest>();
		private static bool isProcessing = false;
		
		public static void RequestPath(Vector3 start, Vector3 end, Action<Path> callback)
		{
			pathRequests.Enqueue(new PathRequest
			{
				start = start,
				end = end,
				callBack = callback
			});
			TryProcessNext();
		}

		private static void TryProcessNext()
		{
			if (isProcessing || pathRequests.Count == 0) return;

			isProcessing = true;
			
			var request = pathRequests.Dequeue();
			var path = PathFinder.instance.FindPath(request.start, request.end, false);

			request.callBack(path);

			// Do max one path finding per frame
			FrameSyncUtility.SyncUpdate(() =>
			{
				isProcessing = false;
				TryProcessNext();
			});
		}
	}
}