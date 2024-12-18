using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private Button _startButton;
    private Button _exitButton;

    private void Awake()
    {
        _startButton = GameObject.Find("StartButton").GetComponent<Button>();
        _exitButton = GameObject.Find("ExitButton").GetComponent<Button>();
        
        _startButton.onClick.AddListener(OnStartButtonClicked);
        _exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private static void OnExitButtonClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else 
        Application.Quit();
        #endif  
    }

    private static void OnStartButtonClicked()
    {
        SceneManager.LoadScene("Game");
    }
}
