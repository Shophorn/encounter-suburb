using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu]
public class ProjectileType : ScriptableObject
{
	[NonSerialized] public int id;

	public Mesh mesh;
	public Material material;
	public ParticleSystem fireFx;
	[FormerlySerializedAs("blastVfx")] public ParticleSystem blastFx;
	
	public float speed = 10f;
	public float collisionRadius = 0.5f;
	public float damage = 5f;
	public float maxRange = 10f;
	public float sqrMaxRange => maxRange * maxRange;
}
