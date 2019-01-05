using System;
using UnityEngine;
using UnityEngine.UI;

public enum MenuView
{
	Main, LevelComplete, GameOver
}

public class MenuSystem : MonoBehaviour
{
	[Header("Main Menu")]
	public GameObject mainMenuView;
	public Button mainMenu_Play;
	
	[Header("Level Complete")]
	public GameObject levelCompleteView;
	public Button levelComplete_Next;
	
	[Header("Game Over")]
	public GameObject gameOverView;
	public Button gameOver_Menu;
	
	public void Show(MenuView view)
	{
		Hide();
			
		gameObject.SetActive(true);

		switch (view)
		{
			case MenuView.Main:				mainMenuView.SetActive(true);		break;
			case MenuView.LevelComplete:	levelCompleteView.SetActive(true);	break;
			case MenuView.GameOver:			gameOverView.SetActive(true);		break;
		}		
	}

	public void Hide()
	{
		gameObject.SetActive(false);
		
		mainMenuView.SetActive(false);
		levelCompleteView.SetActive(false);
		gameOverView.SetActive(false);
	}
	
}