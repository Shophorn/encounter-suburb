using UnityEngine;
using UnityEngine.UI;

public enum MenuView
{
	Main, LevelComplete, GameOver, GameComplete
}

public class MenuSystem : MonoBehaviour
{
	public GameObject canvasObject;
	
	[Header("Main Menu")]
	public GameObject mainMenuView;
	public Button mainMenu_Play;
	
	[Header("Level Complete")]
	public GameObject levelCompleteView;
	public Button levelComplete_Next;
	public Button levelComplete_Menu;
	
	[Header("Game Over")]
	public GameObject gameOverView;
	public Button gameOver_Menu;
	
	[Header("Game Complete")]
	public GameObject gameCompleteView;
	public Button gameComplete_Menu;

	public void Show(MenuView view)
	{
		Hide();
			
		canvasObject.SetActive(true);

		switch (view)
		{
			case MenuView.Main:				mainMenuView.SetActive(true);		break;
			case MenuView.LevelComplete:	levelCompleteView.SetActive(true);	break;
			case MenuView.GameOver:			gameOverView.SetActive(true);		break;
			case MenuView.GameComplete:		gameCompleteView.SetActive(true);	break;
		}		
	}

	public void Hide()
	{
		canvasObject.SetActive(false);
		
		mainMenuView.SetActive(false);
		levelCompleteView.SetActive(false);
		gameOverView.SetActive(false);
		gameCompleteView.SetActive(false);
	}
	
}