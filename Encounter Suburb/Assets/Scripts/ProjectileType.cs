using UnityEngine;

[CreateAssetMenu]
public class ProjectileType : ScriptableObject
{
	public Mesh mesh;
	public Material material;
	public float speed = 10f;
	public float collisionRadius = 0.5f;
	public float damage = 5f;
	public float blastRadius = 2.0f;
	public ParticleSystem blastFX;
	public float maxRange = 10f;
	public float sqrMaxRange => maxRange * maxRange;
	public bool explodeOnMaxRange = false;
}
