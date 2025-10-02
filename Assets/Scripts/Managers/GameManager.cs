using UnityEngine;
using System;
using Utility.SceneManagement;

public class GameManager : Singletons.Singleton<GameManager>
{
    // Fields
    [SerializeField]
    bool _debugMode = false;

    // Action
    public static event Action OnGameStart;
    public static event Action OnGameOver;

    public static event Action OnPlayerHurt;
    public static event Action OnPlayerHeal;

    // Properties
    public bool debugMode { get { return _debugMode; } }

    // Methods
    public void StartGame()
    {
        OnGameStart?.Invoke();
    }

    public void GameOver()
    {
        OnGameOver?.Invoke();
    }

    public void PlayerHurt()
    {
        OnPlayerHurt?.Invoke();
    }

    public void PlayerHeal()
    {
        OnPlayerHeal?.Invoke();
    }
}
