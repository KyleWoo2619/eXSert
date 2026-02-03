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

public class CranePuzzle : MonoBehaviour, IPuzzleInterface
{
    // Cache of the player's movement component so it can be re-enabled later
    private PlayerMovement cachedPlayerMovement;

    [Header("Input Actions")]
     [SerializeField] protected InputActionReference craneMoveAction;
    [SerializeField] protected InputActionReference _escapePuzzleAction;
    [SerializeField] protected InputActionReference _confirmPuzzleAction;

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

    internal readonly Dictionary<CranePart, Vector3> cranePartStartLocalPositions = new Dictionary<CranePart, Vector3>();

    public bool isCompleted { get; set; }

    private void Awake()
    {
        foreach (GameObject img in craneUI)
        {
            img.SetActive(false);
        }

        CacheCranePartStartPositions();
    }
    
    protected virtual void Update()
    {
        // If not active or automated movement is running, skip processing
        if (!puzzleActive || isAutomatedMovement || isExtending || craneParts == null || craneParts.Count == 0) return;   

        // Read CranePuzzle move action when available (prefer runtime action from PlayerInput)
        Vector2 move = InputReader.MoveInput;
        if (runtimeCraneMoveAction != null && runtimeCraneMoveAction.enabled)
        {
            move = runtimeCraneMoveAction.ReadValue<Vector2>();
        }
        else if (craneMoveAction != null && craneMoveAction.action != null && craneMoveAction.action.enabled)
        {
            move = craneMoveAction.action.ReadValue<Vector2>();
        }

        // Ignore small input within deadzone
        if (move.sqrMagnitude < InputReader.Instance.leftStickDeadzoneValue * InputReader.Instance.leftStickDeadzoneValue)
        {
            // No input, so reset cached start positions so AmIMoving() returns false
            CacheCranePartStartPositions();
            return;
        }

        // Optionally invert only horizontal input (A/D)
        if (invertHorizontal)
        {
            move.x = -move.x;
        }

        // Move all crane parts simultaneously based on their enabled axes
        foreach (CranePart part in craneParts)
        {
            // Skip if no part object assigned
            if (part.partObject == null) continue;

            // Work entirely in local space so designer-entered limits are relative to the crane hierarchy
            Transform partTransform = part.partObject.transform;
            Vector3 currentPos = partTransform.localPosition;
            Vector3 newPos = currentPos;

            if (!cranePartStartLocalPositions.TryGetValue(part, out Vector3 startPos))
            {
                startPos = currentPos;
                cranePartStartLocalPositions[part] = startPos;
            }

            // Determine input mapping depending on swap controls bool
            float inputForX = swapXZControls ? move.y : move.x;
            float inputForY = move.y;
            float inputForZ = swapXZControls ? move.x : (part.moveY ? 0f : move.y);

            // Apply movement based on which axes are enabled, respecting bounds
            if (part.moveX)
            {
                newPos.x += inputForX * craneMoveSpeed * Time.deltaTime;
                newPos.x = Mathf.Clamp(newPos.x, part.minX, part.maxX);
                
            }

            if (part.moveY)
            {
                newPos.y += inputForY * craneMoveSpeed * Time.deltaTime;
                newPos.y = Mathf.Clamp(newPos.y, part.minY, part.maxY);
                
            }

            if (part.moveZ)
            {
                newPos.z += inputForZ * craneMoveSpeed * Time.deltaTime;
                newPos.z = Mathf.Clamp(newPos.z, part.minZ, part.maxZ);
                
            }
            else
            {
                newPos.z = Mathf.Clamp(newPos.z, part.minZ, part.maxZ);
            }

            if (!part.moveX)
            {
                newPos.x = startPos.x;
                
            }

            if (!part.moveY)
            {
                newPos.y = startPos.y;
            }

            if (!part.moveZ)
            {
                newPos.z = startPos.z;
            }

            partTransform.localPosition = newPos;

        }

        if(isCompleted || _escapePuzzleAction != null && _escapePuzzleAction.action != null && _escapePuzzleAction.action.triggered)
        {
            EndPuzzle();
        }
    }

    #region IPuzzleInterface Methods

    // Called by whatever system starts this puzzle
    public virtual void StartPuzzle()
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
            }
            else
            {
                runtimeCraneMoveAction = null;
            }

            if (runtimeCraneMoveAction != null && !runtimeCraneMoveAction.enabled)
            {
                runtimeCraneMoveAction.Enable();
            }

            if (craneMoveAction != null && craneMoveAction.action != null && !craneMoveAction.action.enabled)
            {
                craneMoveAction.action.Enable();
            }
        }

        _escapePuzzleAction.action.Enable(); // Make sure enabled
        puzzleActive = true;

        // Prevent player input reads (used across movement, dash, etc.); Jump still wont deactivate idk why
        InputReader.inputBusy = true;

        // Finds the player
        var player = GameObject.FindWithTag("Player");
        Debug.Log($"Player found: {(player != null ? player.name : "NULL")}");
        
        if (player != null)
        {
            // Try to find PlayerMovement on the player, its children, or parent; fallback to any active instance
            var pm = player.GetComponent<PlayerMovement>();

            Debug.Log($"PlayerMovement found: {(pm != null ? "YES" : "NO")}");

            // If found, disable movement and cache for restoration
            if (pm != null)
            {
                cachedPlayerMovement = pm;
                pm.enabled = false;
                Debug.Log($"PlayerMovement disabled on {(pm.gameObject != null ? pm.gameObject.name : player.name)}");
            }
            else
            {
                Debug.LogError($"PlayerMovement NOT FOUND on {player.name} or its hierarchy; gameplay movement will remain enabled.");
            }
        }
        else
        {
            Debug.LogError("Player with 'Player' tag not found!");
        }

        // Changes camera priority to switch to puzzle camera
       if(puzzleCamera != null)
       {
           puzzleCamera.Priority = 21;
       }
    }

    // Call this when the puzzle is finished or cancelled
    public virtual void EndPuzzle()
    {
            foreach (GameObject img in craneUI)
            {
                img.SetActive(false);
            }

            puzzleActive = false;

            StopAllCoroutines();

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
            RestorePlayerMovement();
            
    }
    #endregion

    internal bool AmIMoving()
    {
        foreach (CranePart part in craneParts)
        {
            if (part.partObject == null) continue;

            Vector3 currentPos = part.partObject.transform.localPosition;
            if (cranePartStartLocalPositions.TryGetValue(part, out Vector3 startPos))
            {
                if (currentPos != startPos)
                {
                    return true;
                }
            }
        }
        return false;
    }
    public bool IsMoving()
    {
        return AmIMoving();
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
    protected virtual void CheckForConfirm()
    {
        // Override in derived classes
    }

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