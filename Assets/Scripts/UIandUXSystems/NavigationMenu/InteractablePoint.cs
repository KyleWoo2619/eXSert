/*
    Generic script for world points where collectibles (logs, diaries, etc.) will be located
    Can be used for both LogPoint and DiaryPoint functionality

    Written by Brandon Wahl
*/
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class InteractablePoint : MonoBehaviour
{
    public enum InteractType { Log, Diary, Puzzle }

    [Header("Interactable Entry")]
    [SerializeField] private InteractType interactType;
    [SerializeField] private ScriptableObject collectibleInfo; // NavigationLogSO or DiarySO

    [SerializeField] private InputActionReference _interactAction;
    [SerializeField] private TextMeshProUGUI interractableText;

    [SerializeField] private Image interactGamePadIcon;

    private string activeControlScheme;

    private bool playerIsNear = false;
    private string collectibleId;

    private void FixedUpdate()
    {
        OnInteractButtonPressed();

       activeControlScheme = InputReader.playerInput.currentControlScheme; 
    }

    private void Awake()
    {
        // gets ID based on type
        if (interactType == InteractType.Log)
        {
            var logSO = collectibleInfo as NavigationLogSO;
            if (logSO != null) collectibleId = logSO.logID;
        }
        else if (interactType == InteractType.Diary)
        {
            var diarySO = collectibleInfo as DiarySO;
            if (diarySO != null) collectibleId = diarySO.diaryID;
        } 
        else 
        {
            collectibleInfo = null;
        }

        // Will hide the text on start
        interractableText.gameObject.SetActive(false);


        interactGamePadIcon.gameObject.SetActive(false);

        
    }

    // Subscribes and unsubscribes to state change events
    private void OnEnable()
    {
        if (interactType == InteractType.Log)
        {
            EventsManager.Instance.logEvents.onLogStateChange += OnLogStateChange;
        }
        else if (interactType == InteractType.Diary)
        {
            EventsManager.Instance.diaryEvents.onDiaryStateChange += OnDiaryStateChange;
        }
    }

    private void OnDisable()
    {
        if (interactType == InteractType.Log)
        {
            EventsManager.Instance.logEvents.onLogStateChange -= OnLogStateChange;
        }
        else if (interactType == InteractType.Diary)
        {
            EventsManager.Instance.diaryEvents.onDiaryStateChange -= OnDiaryStateChange;
        }
    }

    // Handles log state changes
    private void OnLogStateChange(Logs logs)
    {
        if (logs.info.logID.Equals(collectibleId))
        {
            Debug.Log("Log with id " + collectibleId + " updated to state: Is Found " + logs.info.isFound);
        }
    }

    // Handles diary state changes
    private void OnDiaryStateChange(Diaries diaries)
    {
        if (diaries.info.diaryID.Equals(collectibleId))
        {
            Debug.Log("Diary with id " + collectibleId + " updated to state: Is Found " + diaries.info.isFound);
        }
    }

    // If the player is in the trigger and presses the interact button, then the corresponding event is triggered
    private void OnInteractButtonPressed()
    {
        if (playerIsNear)
        {
            if(_interactAction != null && _interactAction.action != null && _interactAction.action.triggered)
            {
                if (interactType == InteractType.Log)
                {
                    var logSO = collectibleInfo as NavigationLogSO;
                    logSO.isFound = true;
                    Debug.Log("Log triggered");

                }
                else if (interactType == InteractType.Diary)
                {
                    var diarySO = collectibleInfo as DiarySO;
                    diarySO.isFound = true;
                    Debug.Log("Diary triggered");
                }
                else
                {
                    Debug.Log("Event triggered");
                }

                interractableText.gameObject.SetActive(false); // Once the gameobject is off the text stays, so it is turned off here
                this.gameObject.SetActive(false); // Makes the interactable point disappear after interaction

                if(activeControlScheme != "KeyboardMouse"){
                    interactGamePadIcon.gameObject.SetActive(false); // turns off the gamepad icon
                }
            }
        }
    }


    // PlayerIsNear bool changes depending on these
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = true;
            interractableText.gameObject.SetActive(true);
            //Shows different text based on control scheme
            if(activeControlScheme == "KeyboardMouse"){
                interractableText.text = $"Press {(_interactAction.action.controls[0].name).ToUpperInvariant()} to interact";
            }
            else
            {
                string gamePadButtonName = _interactAction.action.controls[0].name;
                foreach (var iconEntry in SettingsManager.Instance.gamePadIcons)
                {
                    if (iconEntry.Key == gamePadButtonName)
                    {
                        interactGamePadIcon.sprite = iconEntry.Value;
                        break;
                    }
                }
                interactGamePadIcon.gameObject.SetActive(true);
                interractableText.text = $"Press \n\n to interact";
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
            interractableText.gameObject.SetActive(false);
            
            if(activeControlScheme != "KeyboardMouse"){
                    interactGamePadIcon.gameObject.SetActive(false); // turns off the gamepad icon
                }   
        }
    }
}
