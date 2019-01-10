using UnityEngine;
using UnityEngine.UI;

public class PlayerHpBar : MonoBehaviour
{
	[SerializeField] private Slider playerHp;
	[SerializeField] private Slider baseHp;

	public void SetPlayer(Breakable breakable)
	{
		playerHp.maxValue = breakable.maxHp;
		playerHp.value = breakable.hp;

		breakable.OnHit += () => playerHp.value = breakable.hp;
	}
	
	public void SetBase(Breakable breakable)
	{
		baseHp.maxValue = breakable.maxHp;
		baseHp.value = breakable.hp;

		breakable.OnHit += () => baseHp.value = breakable.hp;
	}
}