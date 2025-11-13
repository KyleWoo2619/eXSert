/*
    Generic script for world points where collectibles (logs, diaries, etc.) will be located
    Can be used for both LogPoint and DiaryPoint functionality

    Written by Brandon Wahl
*/
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class InteractablePoint : MonoBehaviour
{
    public enum InteractType { Log, Diary, Puzzle }

    [Header("Interactable Entry")]
    [SerializeField] private InteractType interactType;
    [SerializeField] private ScriptableObject collectibleInfo; // NavigationLogSO or DiarySO

    [SerializeField] private InputActionReference _interactAction;
    [SerializeField] private TextMeshProUGUI interractableText;

    [SerializeField] private Sprite interactGamePadIcon;

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
                    interractableText.gameObject.SetActive(false); // Once the gameobject is off the text stays, so it is turned off here
                    this.gameObject.SetActive(false); // makes it so it can't be triggered again
                }
                else if (interactType == InteractType.Diary)
                {
                    var diarySO = collectibleInfo as DiarySO;
                    diarySO.isFound = true;
                    Debug.Log("Diary triggered");
                    this.gameObject.SetActive(false);
                    interractableText.gameObject.SetActive(false);
                }
                else
                {
                    Debug.Log("Event triggered");
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
                interractableText.text = $"Press <sprite index=0> to interact";
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
            interractableText.gameObject.SetActive(false);    
        }
    }
}
