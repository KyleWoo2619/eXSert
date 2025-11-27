/*
    Generic script for world points where collectibles (logs, diaries, etc.) will be located
    Can be used for both LogPoint and DiaryPoint functionality

    Written by Brandon Wahl
*/
using System;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Attribute to show field only for Puzzle type
public class ShowIfPuzzleAttribute : PropertyAttribute { }

// Attribute to show field only for Log or Diary type
public class ShowIfLogOrDiaryAttribute : PropertyAttribute { }

// Attribute to show field only for Item type
public class ShowIfItemAttribute : PropertyAttribute { }

// Attribute to show field for Log, Diary, or Item types
public class ShowIfCollectibleAttribute : PropertyAttribute { }    

[RequireComponent(typeof(BoxCollider))]
public class InteractablePoint : MonoBehaviour
{
    [SerializeField] private bool showHitbox = false;

    public enum InteractType { Log, Diary, Puzzle, Item }

    [Header("Interactable Entry")]
    [SerializeField] private InteractType interactType;

    [ShowIfLogOrDiary]
    [SerializeField] private ScriptableObject collectibleInfo; // Only shows when InteractType is Log or Diary

    [SerializeField] private InputActionReference _interactAction;
    [SerializeField] private TextMeshProUGUI interractableText;

    [SerializeField] private Image interactGamePadIcon;
    
    [ShowIfPuzzle]
    [SerializeField] private AnimationClip interactAnimation; // Only shows when InteractType is Puzzle

    [ShowIfPuzzle]
    [SerializeField] private GameObject puzzleHandler; // Only shows when InteractType is Puzzle
    private bool playerIsNear = false;
    
    [ShowIfCollectible]
    [SerializeField] private string collectibleId;
    // Mark that this interactable has already been used to prevent re-showing
    private bool interacted = false;

    // Detects if the player is using keyboard or gamepad
    private bool IsUsingKeyboard()
    {
        var scheme = InputReader.Instance != null ? InputReader.Instance.activeControlScheme ?? string.Empty : string.Empty;
        return scheme.IndexOf("keyboard", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void Update()
    {
        OnInteractButtonPressed();
    }

    private void Awake()
    {
        this.GetComponent<Collider>().isTrigger = true;

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

        if(interactAnimation == null || interactType == InteractType.Puzzle)
        {
            Debug.LogWarning("No interaction animation assigned to InteractablePoint at " + this.gameObject.name);
        }
        
        //Standardize collectible ID
        collectibleId = collectibleId.Trim().ToLowerInvariant();

        // Will hide the text on start
        interractableText.gameObject.SetActive(false);


        interactGamePadIcon.gameObject.SetActive(false);

        Debug.Log(InputReader.Instance.activeControlScheme);
        
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
        if (playerIsNear && !interacted)
        {
            if(_interactAction != null && _interactAction.action != null && _interactAction.action.triggered)
            {
                Debug.Log($"Interact pressed on {gameObject.name} (id={collectibleId})");

                if (interactType == InteractType.Log)
                {
                    var logSO = collectibleInfo as NavigationLogSO;
                    logSO.isFound = true;
                    Debug.Log("Log triggered");
                    
                    // Trigger event to add log to scrolling list
                    EventsManager.Instance.logEvents.FoundLog(collectibleId);
                }
                else if (interactType == InteractType.Diary)
                {
                    var diarySO = collectibleInfo as DiarySO;
                    diarySO.isFound = true;
                    Debug.Log("Diary triggered");
                    
                    // Trigger event to add diary to scrolling list
                    EventsManager.Instance.diaryEvents.FoundDiary(collectibleId);
                }
                else if (interactType == InteractType.Puzzle)
                {
                    if (puzzleHandler == null)
                    {
                        Debug.LogError($"puzzleHandler is NULL on {gameObject.name}. Assign it in the inspector.");
                    }
                        else
                        {
                            var puzzleHandlerComponent = puzzleHandler.GetComponent<PuzzleHandler>();
                            if (puzzleHandlerComponent == null)
                            {
                                Debug.LogError($"PuzzleHandler component not found on {puzzleHandler.name}.");
                            }
                            else
                            {
                                if (InternalPlayerInventory.Instance == null)
                                {
                                    Debug.LogError("InternalPlayerInventory.Instance is null");
                                }
                                else
                                {
                                    bool has = InternalPlayerInventory.Instance.collectedInteractables.Contains(puzzleHandlerComponent.puzzleIDNeeded);
                                    Debug.Log($"Inventory contains required id: {has}");
                                    if (has)
                                    {
                                        try
                                        {
                                            puzzleHandlerComponent.ActivatePuzzle();
                                            Debug.Log("Puzzle triggered");
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.LogError($"Exception while activating puzzle on {puzzleHandler.name}: {ex}");
                                        }
                                    }
                                }
                            }
                        }
                }
                else if (interactType == InteractType.Item)
                {
                    Debug.Log($"{collectibleId} found!");
                    InternalPlayerInventory.Instance.AddCollectible(collectibleId);
                }

                if(interactType != InteractType.Puzzle) //If it is a puzzle you should be able to interact again later
                {
                     // Once the gameobject is off the text stays, so it is turned off here
                     // mark interacted so triggers won't re-show the prompt
                    interacted = true;

                    // disable collider immediately so player cannot retrigger while we deactivate
                    var col = GetComponent<Collider>();
                    if (col != null) col.enabled = false;

                    // Always hide the gamepad icon after interaction regardless of control scheme
                    

                    // Attempt to deactivate the whole gameobject. If something else re-enables it later,
                    // the interacted bool prevents it from showing again.
                    Debug.Log($"Disabling interactable {gameObject.name}");
                    this.gameObject.SetActive(false); // Makes the interactable point disappear after interaction
                }

                if (interactGamePadIcon != null)
                    interactGamePadIcon.gameObject.SetActive(false);
                interractableText.gameObject.SetActive(false);
            }
        }
    }


    // PlayerIsNear bool changes depending on these
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (interacted) return; // already used
            playerIsNear = true;
            interractableText.gameObject.SetActive(true);
            //Shows different text based on control scheme
            if(IsUsingKeyboard()){
                interractableText.text = $"Press {(_interactAction.action.controls[0].name).ToUpperInvariant()} to interact";
                if (interactGamePadIcon != null)
                    interactGamePadIcon.gameObject.SetActive(false);
            }
            else
            {
                string gamePadButtonName = _interactAction.action.controls[0].name;
                if (interactGamePadIcon != null)
                    interactGamePadIcon.gameObject.SetActive(true);
                foreach (var iconEntry in SettingsManager.Instance.gamePadIcons)
                {
                    if (iconEntry.Key == gamePadButtonName)
                    {
                        interactGamePadIcon.sprite = iconEntry.Value;
                        break;
                    }
                }
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

            if (interactGamePadIcon != null)
            {
                interactGamePadIcon.gameObject.SetActive(false); // turns off the gamepad icon
            }
        }
    }

    private void OnDrawGizmos()
    {
        if(showHitbox)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>().size);
        }
    }
}

// Custom Property Drawers

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ShowIfPuzzleAttribute))]
public class ShowIfPuzzleDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as InteractablePoint;
        if (parent != null)
        {
            var interactTypeField = property.serializedObject.FindProperty("interactType");
            if (interactTypeField != null && interactTypeField.enumValueIndex == 2) // 2 = Puzzle
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as InteractablePoint;
        if (parent != null)
        {
            var interactTypeField = property.serializedObject.FindProperty("interactType");
            if (interactTypeField != null && interactTypeField.enumValueIndex == 2) // 2 = Puzzle
            {
                return EditorGUI.GetPropertyHeight(property);
            }
        }
        return 0;
    }
}

[CustomPropertyDrawer(typeof(ShowIfItemAttribute))]
public class ShowIfItemDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as InteractablePoint;
        if (parent != null)
        {
            var interactTypeField = property.serializedObject.FindProperty("interactType");
            if (interactTypeField != null && interactTypeField.enumValueIndex == 3) // 3 = Item
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as InteractablePoint;
        if (parent != null)
        {
            var interactTypeField = property.serializedObject.FindProperty("interactType");
            if (interactTypeField != null && interactTypeField.enumValueIndex == 3) // 3 = Item
            {
                return EditorGUI.GetPropertyHeight(property);
            }
        }
        return 0;
    }
}

[CustomPropertyDrawer(typeof(ShowIfLogOrDiaryAttribute))]
public class ShowIfLogOrDiaryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as InteractablePoint;
        if (parent != null)
        {
            var interactTypeField = property.serializedObject.FindProperty("interactType");
            if (interactTypeField != null && (interactTypeField.enumValueIndex == 0 || interactTypeField.enumValueIndex == 1)) // 0 = Log, 1 = Diary
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as InteractablePoint;
        if (parent != null)
        {
            var interactTypeField = property.serializedObject.FindProperty("interactType");
            if (interactTypeField != null && (interactTypeField.enumValueIndex == 0 || interactTypeField.enumValueIndex == 1)) // 0 = Log, 1 = Diary
            {
                return EditorGUI.GetPropertyHeight(property);
            }
        }
        return 0;
    }
}

[CustomPropertyDrawer(typeof(ShowIfCollectibleAttribute))]
public class ShowIfCollectibleDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as InteractablePoint;
        if (parent != null)
        {
            var interactTypeField = property.serializedObject.FindProperty("interactType");
            // Show for Log (0), Diary (1), or Item (3)
            if (interactTypeField != null && (interactTypeField.enumValueIndex == 0 || interactTypeField.enumValueIndex == 1 || interactTypeField.enumValueIndex == 3))
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as InteractablePoint;
        if (parent != null)
        {
            var interactTypeField = property.serializedObject.FindProperty("interactType");
            // Show for Log (0), Diary (1), or Item (3)
            if (interactTypeField != null && (interactTypeField.enumValueIndex == 0 || interactTypeField.enumValueIndex == 1 || interactTypeField.enumValueIndex == 3))
            {
                return EditorGUI.GetPropertyHeight(property);
            }
        }
        return 0;
    }
}
#endif
