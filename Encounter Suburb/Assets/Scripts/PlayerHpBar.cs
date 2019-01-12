using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHpBar : MonoBehaviour
{
	[SerializeField] private Slider playerHp;
	[SerializeField] private Slider baseHp;

	[SerializeField] private Image playerFill;
	[SerializeField] private Image baseFill;

	[SerializeField] private Gradient playerGradient;
	[SerializeField] private Gradient baseGradient;
	private const float flashThreshold = 0.1f;

	public void SetPlayer(Breakable breakable)
	{
		playerHp.maxValue = breakable.maxHp;
		playerHp.value = breakable.hp;
		playerFill.color = playerGradient.Evaluate(1f);

		breakable.OnHit += () =>
		{
			playerHp.value = breakable.hp;

			float t = playerHp.value / playerHp.maxValue;
			playerFill.color = playerGradient.Evaluate(t);

			if (t < flashThreshold)
			{
				StartCoroutine(FlashPlayer());
			}
			
		};
	}
	
	public void SetBase(Breakable breakable)
	{
		baseHp.maxValue = breakable.maxHp;
		baseHp.value = breakable.hp;
		baseFill.color = baseGradient.Evaluate(1f);

		breakable.OnHit += () =>
		{
			baseHp.value = breakable.hp;

			float t = playerHp.value / playerHp.maxValue;
			baseFill.color = baseGradient.Evaluate(t);

			if (t < flashThreshold)
			{
				StartCoroutine(FlashBase());
			}
		};
	}

	public void Stop()
	{
		StopAllCoroutines();
	}
	
	private IEnumerator FlashPlayer()
	{
		while (true)
		{
			var a = playerGradient.Evaluate(playerHp.value / playerHp.maxValue);
			var b = Color.white;
			playerFill.color = Color.Lerp(a, b, Mathf.PingPong(Time.time * 5, 1f));
			yield return null;
		}
	}
	
	private IEnumerator FlashBase()
	{
		while (true)
		{
			var a = playerGradient.Evaluate(baseHp.value / baseHp.maxValue);
			var b = Color.white;
			baseFill.color = Color.Lerp(a, b, Mathf.PingPong(Time.time * 5, 1f));
			yield return null;
		}
	}
}