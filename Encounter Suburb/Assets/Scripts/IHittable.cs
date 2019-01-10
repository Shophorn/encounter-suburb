using System;

public interface IHittable
{
	void Hit(float damage);
	event Action OnHit;
}