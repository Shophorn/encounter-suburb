using System.Collections.Generic;
using UnityEngine;

public class ProjectileSystem : MonoBehaviour
{
	private static ProjectileSystem instance;

	private readonly Dictionary<ProjectileType, List<SimpleTransform>> projectiles
		= new Dictionary<ProjectileType, List<SimpleTransform>>();
	
	// Avoid allocation
	private const int MAX_PROJECTILES_PER_TYPE = 1024;
	private readonly Matrix4x4[] toRender = new Matrix4x4[MAX_PROJECTILES_PER_TYPE];

	public LayerMask hitMask;
	
	private void Awake()
	{
		instance = this;
		
		foreach (var type in Resources.FindObjectsOfTypeAll<ProjectileType>())
		{
			instance.projectiles.Add(type, new List<SimpleTransform>());
		}
	}

	private void Update()
	{
		foreach (var pair in projectiles)
		{
			var type = pair.Key;
			var list = pair.Value;
			int count = list.Count;

			float step = type.speed * Time.deltaTime;
			float radius = type.collisionRadius;
			
			for (int i = 0; i < count; i++)
			{
				RaycastHit hitInfo;
				if (Physics.SphereCast(list[i].position, radius, list[i].direction, out hitInfo, step, hitMask))
				{
					var hittable = hitInfo.transform.GetComponent<IHittable>();
					hittable?.Hit(type.damage);
					
					list.RemoveAt(i);
					i--;
					count--;
					continue;
				}
				
				var current = list[i];
				current.distance += step;
				if (current.distance > type.maxRange)
				{
					list.RemoveAt(i);
					i--;
					count--;
					continue;
				}
				
				list[i] = current;
				
				toRender[i] = Matrix4x4.TRS(current.position, current.rotation, Vector3.one * radius);
			}
			
			Graphics.DrawMeshInstanced(pair.Key.mesh, 0, pair.Key.material, toRender, count);
		}
	}

	public static void Shoot(Vector3 position, Quaternion rotation, ProjectileType type)
	{
		var initialHits = Physics.OverlapSphere(position, type.collisionRadius, instance.hitMask);
		if (initialHits.Length > 0)
		{
			initialHits[0].GetComponent<Breakable>()?.Hit(type.damage);
			return;
		}
		
		instance.projectiles[type].Add(
			new SimpleTransform
			{
				start = position,
				rotation = rotation,
				direction = rotation * Vector3.forward
			});

	}
}

public struct SimpleTransform
{
	public Vector3 start;
	public Vector3 direction;
	public float distance;
	public Quaternion rotation;

	public Vector3 position => start + direction * distance;
}

