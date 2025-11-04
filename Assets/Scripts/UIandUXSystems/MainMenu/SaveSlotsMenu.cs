/*
Written by Brandon Wahl

Handles the save slot menu and the actions of the buttons clicked

*/

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

    [SerializeField] internal SaveSlots currentSaveSlotSelected = null;

    private bool isLoadingGame = false;

    private void Awake()
    {
        saveSlots = this.GetComponentsInChildren<SaveSlots>();
    }

    //When a save slot is clicked, it gathers the profile Id and loads the proper data
    public void OnSaveSlotClicked()
    {

        DisableMenuButtons();

        DataPersistenceManager.instance.ChangeSelectedProfileId(currentSaveSlotSelected.GetProfileId());

        if (!isLoadingGame)
        { 
            DataPersistenceManager.instance.NewGame();
        }

        DataPersistenceManager.instance.SaveGame();

        SceneManager.LoadSceneAsync("FP_Elevator");
    }

    //When the back button is click it activates the main menu again
    public void OnBackClicked()
    {
        mainMenu.ActivateMenu();
        this.DeactivateMenu();
    }

    //Activates the main menu when called
    public void ActivateMenu(bool isLoadingGame)
    {
        this.gameObject.SetActive(true);

        this.isLoadingGame = isLoadingGame;

        GameObject firstSelected = backButton.gameObject;

        Dictionary<string, GameData> profilesGameData = DataPersistenceManager.instance.GetAllProfilesGameData();

        //Disables and enables interactability of save slots depending if there is data attached to the profile Id
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

    //Makes it so when clicking buttons other buttons are noninteractable so no errors occur
    public void DisableMenuButtons()
    {
        foreach(SaveSlots saveSlot in saveSlots)
        {
            saveSlot.SetInteractable(false); 
        }
        backButton.gameObject.SetActive(false);
    }

    //Disables main menu
    public void DeactivateMenu()
    {
        this.gameObject.SetActive(false);
    }
}
