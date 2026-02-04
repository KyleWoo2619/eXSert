/*
    Written by Brandon Wahl

    The Script handles the crane puzzle in the cargo bay area. Here, the player control different parts
    of the crane with their respective movement keys; player movement is disabled while the puzzle is active.
    There is many QoL options for those working in engines. These include swapping controls and adding smoothing if wanted.

    Used CoPilot to help with custom property drawers for showing/hiding fields in the inspector and properly adding
    lerping functionality.
*/

using Unity.Cinemachine;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;
#endif


//Once the pieces are in the list, you can set which axes they move on and their min/max positions
[System.Serializable]
public class CranePart
{
    [Tooltip("GameObject to move")]
    public GameObject partObject;
    
    [Tooltip("Enable movement on X axis")]
    public bool moveX = false;
    [Tooltip("Enable movement on Y axis")]
    public bool moveY = false;
    [Tooltip("Enable movement on Z axis")]
    public bool moveZ = false;
    
    [ShowIfX]
    [Tooltip("Min X position")]
    public float minX = -5f;

    [ShowIfX]
    [Tooltip("Max X position")]
    public float maxX = 5f;
    
    [ShowIfY]
    [Tooltip("Min Y position")]
    public float minY = 0f;

    [ShowIfY]
    [Tooltip("Max Y position")]
    public float maxY = 10f;
    
    [ShowIfZ]
    [Tooltip("Min Z position")]
    public float minZ = -5f;
    
    [ShowIfZ]
    [Tooltip("Max Z position")]
    public float maxZ = 5f;
}

// These will be used to show/hide fields in the inspector based on which axes are enabled
public class ShowIfXAttribute : PropertyAttribute { }
public class ShowIfYAttribute : PropertyAttribute { }
public class ShowIfZAttribute : PropertyAttribute { }

public class CranePuzzle : PuzzlePart
{
    // Cache of the player's movement component so it can be re-enabled later
    private PlayerMovement cachedPlayerMovement;

    [Header("Input Actions")]
     [SerializeField] internal InputActionReference craneMoveAction;
    [SerializeField] internal InputActionReference _escapePuzzleAction;
    [SerializeField] internal InputActionReference _confirmPuzzleAction;

    [Space(10)]
    [Header("Camera")]
    // Cinemachine camera for the puzzle
    [SerializeField] CinemachineCamera puzzleCamera;

    [Space(10)]

    // List of crane parts to move
    [Header("Crane Parts")]
    [SerializeField] protected List<CranePart> craneParts = new List<CranePart>();

    [Space(10)]

    // Swap input mapping so X uses W/S and Z uses A/D
    [Tooltip("Swap input mapping so X uses W/S and Z uses A/D")]
    [SerializeField] private bool swapXZControls = false;

    [Space(10)]

    [Header("Crane Settings")]
    [SerializeField] private float craneMoveSpeed = 2f;
    [Tooltip("Height to which the magnet extends")]
    [SerializeField] private GameObject[] craneUI; // UI elements to show/hide during puzzle

    [Space(10)]
    [Header("Crane Control Settings")]
    [Tooltip("Invert horizontal input (A/D) so A acts as right and D as left when enabled")]
    [SerializeField] private bool invertHorizontal = false;

    private bool isMoving = false;
    private bool puzzleActive = false;
    internal bool isExtending = false;
    protected bool isAutomatedMovement = false;
    internal bool isRetracting;

    private InputAction runtimeCraneMoveAction;
    protected InputAction runtimeConfirmAction;
    protected InputAction runtimeEscapeAction;
    private Vector2 cachedMoveInput;
    private Coroutine moveCoroutine;

    internal readonly Dictionary<CranePart, Vector3> cranePartStartLocalPositions = new Dictionary<CranePart, Vector3>();

    private void Awake()
    {
        foreach (GameObject img in craneUI)
        {
            img.SetActive(false);
        }

        CacheCranePartStartPositions();
    }


    #region IPuzzleInterface Methods

    // Called by whatever system starts this puzzle
    public override void StartPuzzle()
    {   
        CacheCranePartStartPositions();

        if (craneUI != null && craneUI.Length > 0)
        {
            if(InputReader.Instance.activeControlScheme == "Gamepad")
            {
                if (craneUI.Length > 1 && craneUI[1] != null)
                {
                    craneUI[1].SetActive(true);
                }
            } 
            else if (InputReader.Instance.activeControlScheme == "Keyboard&Mouse")
            {
                if (craneUI[0] != null)
                {
                    craneUI[0].SetActive(true);
                }
            }
        }

        SwapActionMaps("CranePuzzle");

        if (InputReader.Instance != null && InputReader.Instance._playerInput != null)
        {
            var actions = InputReader.Instance._playerInput.actions;
            var craneMap = actions.FindActionMap("CranePuzzle");
            if (craneMap != null && !craneMap.enabled)
            {
                craneMap.Enable();
            }

            // Prefer runtime action instance from the CranePuzzle map (asset is cloned at runtime)
            if (craneMap != null)
            {
                string moveActionName = (craneMoveAction != null && craneMoveAction.action != null)
                    ? craneMoveAction.action.name
                    : "Move";
                runtimeCraneMoveAction = craneMap.FindAction(moveActionName);

                string confirmActionName = (_confirmPuzzleAction != null && _confirmPuzzleAction.action != null)
                    ? _confirmPuzzleAction.action.name
                    : "Confirm";
                runtimeConfirmAction = craneMap.FindAction(confirmActionName);

                string escapeActionName = (_escapePuzzleAction != null && _escapePuzzleAction.action != null)
                    ? _escapePuzzleAction.action.name
                    : "EscapePuzzle";
                runtimeEscapeAction = craneMap.FindAction(escapeActionName);
            }
            else
            {
                runtimeCraneMoveAction = null;
                runtimeConfirmAction = null;
                runtimeEscapeAction = null;
            }

            if (runtimeCraneMoveAction != null && !runtimeCraneMoveAction.enabled)
            {
                runtimeCraneMoveAction.Enable();
            }

            if (runtimeConfirmAction != null && !runtimeConfirmAction.enabled)
            {
                runtimeConfirmAction.Enable();
            }

            if (runtimeEscapeAction != null && !runtimeEscapeAction.enabled)
            {
                runtimeEscapeAction.Enable();
            }

            if (craneMoveAction != null && craneMoveAction.action != null && !craneMoveAction.action.enabled)
            {
                craneMoveAction.action.Enable();
            }
        }

        _escapePuzzleAction.action.Enable(); // Make sure enabled
        if (_confirmPuzzleAction != null && _confirmPuzzleAction.action != null)
        {
            _confirmPuzzleAction.action.Enable();
        }
        puzzleActive = true;

        // Prevent player input reads (used across movement, dash, etc.); Jump still wont deactivate idk why
        InputReader.inputBusy = true;

        // Finds the player
        var player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            // Try to find PlayerMovement on the player, its children, or parent; fallback to any active instance
            var pm = player.GetComponent<PlayerMovement>();

            // If found, disable movement and cache for restoration
            if (pm != null)
            {
                cachedPlayerMovement = pm;
                pm.enabled = false;
            }
        }

        // Changes camera priority to switch to puzzle camera
       if(puzzleCamera != null)
       {
           puzzleCamera.Priority = 21;
       }

       if (moveCoroutine == null)
        {
            moveCoroutine = StartCoroutine(MoveCraneCoroutine());
        }
    }

    public void ExitPuzzle()
    {
        foreach (GameObject img in craneUI)
            {
                img.SetActive(false);
            }

            puzzleActive = false;

            StopAllCoroutines();
            moveCoroutine = null;
            isMoving = false;

            // Unlock crane movement
            LockOrUnlockMovement(false);
            isExtending = false;
            isAutomatedMovement = false;

            // Disable input actions
            if (_escapePuzzleAction != null && _escapePuzzleAction.action != null)
            {
                _escapePuzzleAction.action.Disable();
            }
            if (_confirmPuzzleAction != null && _confirmPuzzleAction.action != null)
            {
                _confirmPuzzleAction.action.Disable();
            }

            if (craneMoveAction != null && craneMoveAction.action != null)
            {
                craneMoveAction.action.Disable();
            }
            if (runtimeCraneMoveAction != null)
            {
                runtimeCraneMoveAction.Disable();
                runtimeCraneMoveAction = null;
            }
            if (runtimeConfirmAction != null)
            {
                runtimeConfirmAction.Disable();
                runtimeConfirmAction = null;
            }
            if (runtimeEscapeAction != null)
            {
                runtimeEscapeAction.Disable();
                runtimeEscapeAction = null;
            }

            // Sets camera priority back to normal
            if(puzzleCamera != null)
            {
                puzzleCamera.Priority = 9;
            }

            // Re-enable player input
            InputReader.inputBusy = false;

            SwapActionMaps("Gameplay");
            
            if (InputReader.Instance != null && InputReader.Instance._playerInput != null)
            {
                var cranePuzzleMap = InputReader.Instance._playerInput.actions.FindActionMap("CranePuzzle");
                if (cranePuzzleMap != null)
                {
                    cranePuzzleMap.Disable();
                }
                
                InputReader.Instance._playerInput.enabled = true;
                InputReader.Instance._playerInput.ActivateInput();
                InputReader.Instance._playerInput.actions.Enable();
                
                var gameplayMap = InputReader.Instance._playerInput.actions.FindActionMap("Gameplay");
                if (gameplayMap != null)
                {
                    gameplayMap.Enable();
                }
            }
            isCompleted = false;
            RestorePlayerMovement();
    }

    // Call this when the puzzle is finished or cancelled
    public override void EndPuzzle()
    {
        isCompleted = true;
        ExitPuzzle();       
    }

    #endregion
    // Read CranePuzzle move action when available (prefer runtime action from PlayerInput)
    private void ReadMoveAction()
    {
        InputAction actionToRead = runtimeCraneMoveAction != null ? runtimeCraneMoveAction : (craneMoveAction != null ? craneMoveAction.action : null);
        if (actionToRead != null)
        {
            cachedMoveInput = actionToRead.ReadValue<Vector2>();

            if (invertHorizontal)
                cachedMoveInput.x *= -1f; 
        }
    }

    public IEnumerator MoveCraneCoroutine()
    {
        while (puzzleActive && !isAutomatedMovement && !isExtending)
        {

            ReadMoveAction();

            if(_escapePuzzleAction != null && _escapePuzzleAction.action != null && _escapePuzzleAction.action.triggered)
            {
                ExitPuzzle();
                yield break;
            }

            CheckForConfirm();

            CraneMovement();

            yield return null;
        }

        isMoving = false;
        moveCoroutine = null;
    }

    private void CraneMovement()
    {
        Vector2 input = cachedMoveInput;
        float xInput = input.x;
        float yInput = input.y;

        if (swapXZControls)
        {
            float temp = xInput;
            xInput = yInput;
            yInput = temp;
        }

        bool hasInput = input.sqrMagnitude > 0.0001f;
        isMoving = hasInput;

        if (hasInput)
        {
            for (int i = 0; i < craneParts.Count; i++)
            {
                CranePart part = craneParts[i];
                if (part == null || part.partObject == null) continue;

                Vector3 localPos = part.partObject.transform.localPosition;
                Vector3 delta = Vector3.zero;

                if (part.moveX)
                {
                    delta.x = xInput;
                }
                if (part.moveY)
                {
                    delta.y = yInput;
                }
                if (part.moveZ)
                {
                    delta.z = yInput;
                }
                if (delta != Vector3.zero)
                {
                    Vector3 next = localPos + delta * craneMoveSpeed * Time.deltaTime;

                    if (part.moveX)
                    {
                        next.x = Mathf.Clamp(next.x, part.minX, part.maxX);
                    }
                    if (part.moveY)
                    {
                        next.y = Mathf.Clamp(next.y, part.minY, part.maxY);
                    }
                    if (part.moveZ)
                    {
                        next.z = Mathf.Clamp(next.z, part.minZ, part.maxZ);
                    }

                    part.partObject.transform.localPosition = next;
                }
            }
        }
    }

    public bool IsMoving()
    {
        return isMoving;
    }
    
    public bool IsRetracting()
    {
        return isRetracting;
    }

    #region Restrict/Restore Movement
    //After puzzle ends, restore player movement if it was disabled
    private void RestorePlayerMovement()
    {
        // Restore player's movement component; reacquire if cache missing
            if (cachedPlayerMovement == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    cachedPlayerMovement = player.GetComponent<PlayerMovement>();
            }

            if (cachedPlayerMovement != null)
            {
                cachedPlayerMovement.enabled = true;
                
                var cc = cachedPlayerMovement.GetComponent<CharacterController>();
                if (cc != null && !cc.enabled)
                {
                    cc.enabled = true;
                }
            }
            cachedPlayerMovement = null;
    }

    protected void LockOrUnlockMovement(bool lockMovement)
    {
        for (int i = 0; i < craneParts.Count; i++)
        {
            CranePart part = craneParts[i];
            
            // craneParts[1]: Lock X and Y, control Z only
            if (i == 1)
            {
                part.moveX = false;
                part.moveY = false;
                part.moveZ = !lockMovement;
            }
            // craneParts[0]: Lock Y and Z, control X only
            else if (i == 0)
            {
                part.moveX = !lockMovement;
                part.moveY = false;
                part.moveZ = false;
            }
            // Other parts: Lock/unlock all axes
            else
            {
                part.moveX = !lockMovement;
                part.moveY = !lockMovement;
                part.moveZ = !lockMovement;
            }
        }
    }
    #endregion

    private void CacheCranePartStartPositions()
    {
        cranePartStartLocalPositions.Clear();
        if (craneParts == null)
        {
            return;
        }

        foreach (CranePart part in craneParts)
        {
            if (part != null && part.partObject != null)
            {
                cranePartStartLocalPositions[part] = part.partObject.transform.localPosition;
            }
        }
    }

    #region Utility Scripts
    // Swaps action maps
    private void SwapActionMaps(string actionMapName)
    {
        InputReader.Instance._playerInput.SwitchCurrentActionMap(actionMapName);
    }

    private string GetLayerMaskNames(LayerMask mask)
    {
        List<string> layers = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    layers.Add(layerName);
                }
            }
        }
        return layers.Count > 0 ? string.Join(", ", layers) : "None";
    }

    // Checks for confirm input to start magnet extension
    protected virtual void CheckForConfirm(){}

    #endregion

}

// Custom Property Drawers for showing fields based on movement axis toggles

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(ShowIfXAttribute))]
public class ShowIfXDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {

        string parentPathX = property.propertyPath;
        int lastDot = parentPathX.LastIndexOf('.');
        if (lastDot >= 0)
        {
            string prefix = parentPathX.Substring(0, lastDot);
            var moveXField = property.serializedObject.FindProperty(prefix + ".moveX");
            if (moveXField != null && moveXField.boolValue)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        string parentPathH = property.propertyPath;
        int lastDotH = parentPathH.LastIndexOf('.');
        if (lastDotH >= 0)
        {
            string prefix = parentPathH.Substring(0, lastDotH);
            var moveXField = property.serializedObject.FindProperty(prefix + ".moveX");
            if (moveXField != null && moveXField.boolValue)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
        }
        return 0f;
    }
}

[CustomPropertyDrawer(typeof(ShowIfYAttribute))]
public class ShowIfYDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        string parentPathY = property.propertyPath;
        int lastDotY = parentPathY.LastIndexOf('.');
        if (lastDotY >= 0)
        {
            string prefix = parentPathY.Substring(0, lastDotY);
            var moveYField = property.serializedObject.FindProperty(prefix + ".moveY");
            if (moveYField != null && moveYField.boolValue)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        string parentPathHY = property.propertyPath;
        int lastDotHY = parentPathHY.LastIndexOf('.');
        if (lastDotHY >= 0)
        {
            string prefix = parentPathHY.Substring(0, lastDotHY);
            var moveYField = property.serializedObject.FindProperty(prefix + ".moveY");
            if (moveYField != null && moveYField.boolValue)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
        }
        return 0f;
    }
}

[CustomPropertyDrawer(typeof(ShowIfZAttribute))]
public class ShowIfZDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        string parentPathZ = property.propertyPath;
        int lastDotZ = parentPathZ.LastIndexOf('.');
        if (lastDotZ >= 0)
        {
            string prefix = parentPathZ.Substring(0, lastDotZ);
            var moveZField = property.serializedObject.FindProperty(prefix + ".moveZ");
            if (moveZField != null && moveZField.boolValue)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        string parentPathHZ = property.propertyPath;
        int lastDotHZ = parentPathHZ.LastIndexOf('.');
        if (lastDotHZ >= 0)
        {
            string prefix = parentPathHZ.Substring(0, lastDotHZ);
            var moveZField = property.serializedObject.FindProperty(prefix + ".moveZ");
            if (moveZField != null && moveZField.boolValue)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
        }
        return 0f;
    }
}
#endif