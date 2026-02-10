using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenuCheatLoader : MonoBehaviour
{
    [System.Serializable]
    private class CheatEntry
    {
        public KeyCode key = KeyCode.Alpha1;
        public string sceneName = "";
        public string spawnPointId = "default";
    }

    [Header("Activation")]
    [SerializeField] private bool requireCtrl = true;
    [SerializeField] private bool requireShift = true;
    [SerializeField] private bool onlyFromMainMenu = true;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Cheat Targets")]
    [SerializeField] private List<CheatEntry> entries = new()
    {
        new CheatEntry { key = KeyCode.Alpha1, sceneName = "CargoBay", spawnPointId = "default" },
        new CheatEntry { key = KeyCode.Alpha2, sceneName = "CrewQuarters", spawnPointId = "default" },
        new CheatEntry { key = KeyCode.Alpha3, sceneName = "Hangar", spawnPointId = "default" },
        new CheatEntry { key = KeyCode.Alpha4, sceneName = "FinalBoss", spawnPointId = "default" }
    };

    private void Update()
    {
        if (onlyFromMainMenu && !IsMainMenuActive())
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (requireCtrl && !(keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed))
            return;

        if (requireShift && !(keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
            return;

        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.sceneName))
                continue;

            if (IsKeyPressedThisFrame(keyboard, entry.key))
            {
                LoadTarget(entry);
                return;
            }
        }
    }

    private bool IsMainMenuActive()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            return true;

        return SceneManager.GetActiveScene().name == mainMenuSceneName;
    }

    private void LoadTarget(CheatEntry entry)
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadInitialGameScene(
                entry.sceneName,
                additiveSceneName: null,
                pauseUntilLoaded: false,
                spawnPointIdOverride: entry.spawnPointId,
                updateCheckpointAfterLoad: false);
            return;
        }

        SceneManager.LoadScene(entry.sceneName, LoadSceneMode.Single);
    }

    private static bool IsKeyPressedThisFrame(Keyboard keyboard, KeyCode key)
    {
        return key switch
        {
            KeyCode.Alpha1 => keyboard.digit1Key.wasPressedThisFrame,
            KeyCode.Alpha2 => keyboard.digit2Key.wasPressedThisFrame,
            KeyCode.Alpha3 => keyboard.digit3Key.wasPressedThisFrame,
            KeyCode.Alpha4 => keyboard.digit4Key.wasPressedThisFrame,
            _ => false
        };
    }
}
