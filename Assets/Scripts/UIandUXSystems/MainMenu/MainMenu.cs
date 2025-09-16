using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MainMenu : Menu
{
    [Header("Menu Navigation")]
    [SerializeField] private SaveSlotsMenu saveSlotsMenu;

    [SerializeField] private Button loadGame;

    private void Start()
    {
        if (!DataPersistenceManager.instance.HasGameData())
        {
            loadGame.interactable = false;
        }
    }

   public void OnNewGameSelected()
    {
        saveSlotsMenu.ActivateMenu(false);
        this.DeactivateMenu();
    }
    
    public void OnLoadGameClicked()
    {
        saveSlotsMenu.ActivateMenu(true);
        this.DeactivateMenu();
    }

    public void ActivateMenu()
    {
        this.gameObject.SetActive(true);
    }

    public void DeactivateMenu()
    {
        this.gameObject.SetActive(false);
    }
}
