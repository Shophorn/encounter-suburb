using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum MenuView
{
	Main, LevelComplete, GameOver, GameComplete
}

public class MenuSystem : MonoBehaviour
{
	public GameObject canvasObject;
	public GameObject backGround;
	
	[Header("Main Menu")]
	public GameObject mainMenuView;
	public Button mainMenu_Play;
	public Button mainMenu_Exit;
	
	[Header("Level Complete")]
	public GameObject levelCompleteView;
	public Button levelComplete_Next;
	public Button levelComplete_Menu;
	
	[Header("Level Start")]
	public GameObject levelStartView;
	public Text nextLevelNameText;
	public Text timeCounterText;
	public int startCounterCount = 5;
	
	[Header("Level End")]
	public GameObject levelEndLabel;
	public Text levelEndText;
	public float endMessageViewTime = 2f;
	
	[Header("Game Over")]
	public GameObject gameOverView;
	public Button gameOver_Menu;
	
	[Header("Game Complete")]
	public GameObject gameCompleteView;
	public Button gameComplete_Menu;

	[Header("Pause")]
	public GameObject pauseLabel;

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
		
		backGround.SetActive(true); // This defaults to true, but can be set false when showing
		
		mainMenuView.SetActive(false);
		levelCompleteView.SetActive(false);
		levelStartView.SetActive(false);
		levelEndLabel.SetActive(false);
		gameOverView.SetActive(false);
		gameCompleteView.SetActive(false);
		pauseLabel.SetActive(false);
	}

	public void ShowLevelStartInfo(string mapName, int number, Action callback)
	{
		Hide();
		canvasObject.SetActive(true);
		levelStartView.SetActive(true);
		
		nextLevelNameText.text = $"{number : 000} {mapName}";
		StartCoroutine(DoCountdownTimer(callback));
	}

	private IEnumerator DoCountdownTimer(Action callback)
	{
		for (int i = startCounterCount; i >= 0; i--)
		{
			timeCounterText.text = i.ToString();
			yield return new WaitForSeconds(1);
		}

		Hide();
		callback();
	}

	public void ShowEndStatus(string message, Action callback)
	{
		Hide();
		backGround.SetActive(false);
		canvasObject.SetActive(true);
		
		levelEndLabel.SetActive(true);
		levelEndText.text = message;

		this.Invoke(endMessageViewTime, () =>
		{
			Hide();
			callback();
		});
	}

	public void ViewPause(bool show)
	{
		Hide();

		if (show)
		{
			backGround.SetActive(false);
			
			canvasObject.SetActive(true);
			pauseLabel.SetActive(true);
		}
	}
}