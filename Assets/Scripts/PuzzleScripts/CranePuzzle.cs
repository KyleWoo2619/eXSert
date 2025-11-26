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
using Microsoft.Unity.VisualStudio.Editor;

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
    private EnhancedPlayerMovement cachedPlayerMovement;
    private bool disabledPlayerMovement = false;

    // Cinemachine camera for the puzzle
    [SerializeField] CinemachineCamera puzzleCamera;

    // List of crane parts to move
    [Header("Crane Parts")]
    [SerializeField] private List<CranePart> craneParts = new List<CranePart>();

    // Swap input mapping so X uses W/S and Z uses A/D
    [Tooltip("Swap input mapping so X uses W/S and Z uses A/D")]
    [SerializeField] private bool swapXZControls = false;
    
    [Header("Smoothing")]
    [Tooltip("Enable smooth interpolation when moving crane parts")]
    [SerializeField] private bool useLerp = true;

    [Tooltip("Higher values = snappier movement. Should be between 5-20")]
    [SerializeField] private float lerpSpeed = 10f;
    
    // Speed of crane
    [Tooltip("Movement speed applied to the crane (units/sec)")]

    [Header("Crane Settings")]
    [SerializeField] private float craneMoveSpeed = 2f;

    [SerializeField] private GameObject[] craneUI;

    // When true the crane will respond to input
    private bool puzzleActive = false;

    public bool isCompleted { get; set; }

    private void Awake()
    {
        foreach (GameObject img in craneUI)
        {
            img.SetActive(false);
        }

    }

    // Called by whatever system starts this puzzle
    public void StartPuzzle()
    {   
        if(InputReader.Instance.activeControlScheme == "Gamepad")
        {
            craneUI[1].SetActive(true);
        } 
        else 
        {
            craneUI[0].SetActive(true);
        }

        puzzleActive = true;

        // Prevent player input reads (used across movement, dash, etc.); Jump still wont deactivate idk why
        InputReader.inputBusy = true;

        // Finds the player
        var player = GameObject.FindWithTag("Player");
        Debug.Log($"Player found: {(player != null ? player.name : "NULL")}");
        
        if (player != null)
        {
            var pm = player.GetComponent<EnhancedPlayerMovement>();
            Debug.Log($"EnhancedPlayerMovement found: {(pm != null ? "YES" : "NO")}");
            
            // If found, the movement is disabled and stored for later restoration
            if (pm != null)
            {
                cachedPlayerMovement = pm;
                pm.enabled = false;
                disabledPlayerMovement = true;
                Debug.Log($"EnhancedPlayerMovement disabled on {player.name}");
            }
            else
            {
                Debug.LogError($"EnhancedPlayerMovement NOT FOUND on {player.name}");
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
       else
       {
           Debug.LogError("Puzzle Camera not assigned in CranePuzzle.");
       }

       //isCompleted = true;
    }

    // Call this when the puzzle is finished or cancelled
    public void EndPuzzle()
    {

        if(isCompleted)
        {
            foreach (GameObject img in craneUI)
            {
                img.SetActive(false);
            }

            puzzleActive = false;

            // Sets camera priority back to normal
            if(puzzleCamera != null)
            {
                puzzleCamera.Priority = 9;
            }

            // Re-enable player input
            InputReader.inputBusy = false;

            // Restore player's movement component if we disabled it
            if (disabledPlayerMovement && cachedPlayerMovement != null)
            {
                cachedPlayerMovement.enabled = true;
                cachedPlayerMovement = null;
                disabledPlayerMovement = false;
            }
        }

      
    }

    private void Update()
    {
        // If not active, skip processing
        if (!puzzleActive || craneParts == null || craneParts.Count == 0) return;

        // Read centralized input
        Vector2 move = InputReader.MoveInput;

        // Ignore small input within deadzone
        if (move.sqrMagnitude < InputReader.Instance.leftStickDeadzoneValue * InputReader.Instance.leftStickDeadzoneValue) return;

        // Move all crane parts simultaneously based on their enabled axes
        foreach (CranePart part in craneParts)
        {
            // Skip if no part object assigned
            if (part.partObject == null) continue;

            // Get current position
            Vector3 currentPos = part.partObject.transform.position;
            Vector3 newPos = currentPos;

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

            // Lerps to new position if enabled
            if (useLerp)
            {
                float t = Mathf.Clamp01(lerpSpeed * Time.deltaTime);
                part.partObject.transform.position = Vector3.Lerp(currentPos, newPos, t);
            }
            else
            {
                part.partObject.transform.position = newPos;
            }
        }
    }

    // Call this to switch between crane parts (e.g., with keyboard shortcuts or UI buttons)
    public void SetActiveCranePart(int index)
    {
        Debug.Log($"SetActiveCranePart is deprecated. All crane parts are now active simultaneously.");
    }
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