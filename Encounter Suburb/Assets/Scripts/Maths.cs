using Mathf = UnityEngine.Mathf;
 
public static class Maths
{
	public static float Root3(float t)
	{
		const float pow = 1f / 3f;
		
		float sign = Mathf.Sign(t);
		float root = Mathf.Pow(sign * t, pow);

		return sign * root;
	}
}