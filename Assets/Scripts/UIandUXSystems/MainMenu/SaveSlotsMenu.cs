using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveSlotsMenu : Menu
{
    [Header("Menu Navigation")]
    [SerializeField] private MainMenu mainMenu;

    [SerializeField] private Button backButton;

    private SaveSlots[] saveSlots;

    private bool isLoadingGame = false;

    private void Awake()
    {
        saveSlots = this.GetComponentsInChildren<SaveSlots>();
    }

    public void OnSaveSlotClicked(SaveSlots slot)
    {
        DisableMenuButtons();

        DataPersistenceManager.instance.ChangeSelectedProfileId(slot.GetProfileId());

        if (!isLoadingGame)
        { 
            DataPersistenceManager.instance.NewGame();
        }

        DataPersistenceManager.instance.SaveGame();

        SceneManager.LoadSceneAsync("SampleScene");
    }

    public void OnBackClicked()
    {
        mainMenu.ActivateMenu();
        this.DeactivateMenu();
    }

    public void ActivateMenu(bool isLoadingGame)
    {
        this.gameObject.SetActive(true);

        this.isLoadingGame = isLoadingGame;

        GameObject firstSelected = backButton.gameObject;

        Dictionary<string, GameData> profilesGameData = DataPersistenceManager.instance.GetAllProfilesGameData();

        foreach (SaveSlots saveSlot in saveSlots)
        {
            GameData profileData = null;
            profilesGameData.TryGetValue(saveSlot.GetProfileId(), out profileData);
            saveSlot.SetData(profileData);
            if (profileData == null && isLoadingGame)
            {
                saveSlot.SetInteractable(false);
            } 
            else
            {
                saveSlot.SetInteractable(true);
            }
        }
 
    }

    public void DisableMenuButtons()
    {
        foreach(SaveSlots saveSlot in saveSlots)
        {
            saveSlot.SetInteractable(false); 
        }
        backButton.gameObject.SetActive(false);
    }

    public void DeactivateMenu()
    {
        this.gameObject.SetActive(false);
    }
}
