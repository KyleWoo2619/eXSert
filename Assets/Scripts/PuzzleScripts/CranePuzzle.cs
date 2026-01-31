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
using TMPro;
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

public class CranePuzzle : MonoBehaviour, IPuzzleInterface
{
    private enum DetectionResult
    {
        None,
        Target,
        Wrong
    }
    // Cache of the player's movement component so it can be re-enabled later
    private PlayerMovement cachedPlayerMovement;

    [Header("References")]
    [SerializeField] private CraneGrabObject craneGrabObjectScript;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference _escapePuzzleAction;
    [SerializeField] private InputActionReference _confirmPuzzleAction;

    [Space(10)]
    [Header("Camera")]
    // Cinemachine camera for the puzzle
    [SerializeField] CinemachineCamera puzzleCamera;

    [Space(10)]

    // List of crane parts to move
    [Header("Crane Parts")]
    [SerializeField] private List<CranePart> craneParts = new List<CranePart>();
    [SerializeField] public GameObject magnetExtender;

    [Space(10)]

    // Swap input mapping so X uses W/S and Z uses A/D
    [Tooltip("Swap input mapping so X uses W/S and Z uses A/D")]
    [SerializeField] private bool swapXZControls = false;

    [Space(10)]
    
    [Header("Smoothing - Optional")]
    [Tooltip("Enable smooth interpolation when moving crane parts")]
    [SerializeField] private bool useLerp = true;

    [Tooltip("Higher values = snappier movement. Should be between 5-20")]
    [SerializeField] private float lerpSpeed = 10f;

    [Space(10)]

    [Header("Crane Settings")]
    [SerializeField] private float craneMoveSpeed = 2f;
    [Tooltip("Height to which the magnet extends")]
    [SerializeField] private float magnetExtendHeight;
    [SerializeField] private GameObject[] craneUI; // UI elements to show/hide during puzzle
    [Tooltip("Invert horizontal input (A/D) so A acts as right and D as left when enabled")]
    [SerializeField] private bool invertHorizontal = false;


    [Space(10)]

    [Header("Puzzle Settings")]
    [Tooltip("Object crane needs to grab")]
    [SerializeField] internal GameObject targetObject;
    [SerializeField] private GameObject targetDropZone;
    private Vector3 targetEndPos;
    [SerializeField] private LayerMask grabLayerMask;
    [SerializeField] private float magnetDetectLength;
    private Vector3 magnetStartPos;
    internal bool isGrabbed;

    private bool puzzleActive = false;
    internal bool isExtending = false;
    private bool isAutomatedMovement = false;

    // Coroutines for magnet animation
    private Coroutine retractCoroutine;

    public bool isCompleted { get; set; }

    private void Awake()
    {
        foreach (GameObject img in craneUI)
        {
            img.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Visualize both detection rays with colors that reflect hit state
        AssignRayData();


        // If not active or automated movement is running, skip processing
        if (!puzzleActive || isAutomatedMovement || craneParts == null || craneParts.Count == 0) return;

        if(_confirmPuzzleAction != null && _confirmPuzzleAction.action != null && !isExtending)
        {
            CheckForConfirm();
        }

        // Read centralized input
        Vector2 move = InputReader.MoveInput;

        // Ignore small input within deadzone
        if (move.sqrMagnitude < InputReader.Instance.leftStickDeadzoneValue * InputReader.Instance.leftStickDeadzoneValue) return;

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
                Vector3 targetPos = newPos;
                // Clamp before lerping to ensure we don't exceed bounds
                targetPos.x = Mathf.Clamp(targetPos.x, part.minX, part.maxX);
                targetPos.y = Mathf.Clamp(targetPos.y, part.minY, part.maxY);
                targetPos.z = Mathf.Clamp(targetPos.z, part.minZ, part.maxZ);
                partTransform.localPosition = Vector3.Lerp(currentPos, targetPos, Mathf.Clamp01(lerpSpeed * Time.deltaTime));
            }
            else
            {
                partTransform.localPosition = newPos;
            }
        }

        if(isCompleted || _escapePuzzleAction != null && _escapePuzzleAction.action != null && _escapePuzzleAction.action.triggered)
        {
            EndPuzzle();
        }
    }

    #region IPuzzleInterface Methods

    // Called by whatever system starts this puzzle
    public void StartPuzzle()
    {   
        if(InputReader.Instance.activeControlScheme == "Gamepad")
        {
            craneUI[1].SetActive(true);
        } 
        else if (InputReader.Instance.activeControlScheme == "Keyboard&Mouse")
        {
            craneUI[0].SetActive(true);
        }

        SwapActionMaps("CranePuzzle");

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
    public void EndPuzzle()
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

    private DetectionResult DetectDesiredObjectBelow()
    {
        RaycastHit hit;
        RaycastHit hit2;

        Debug.Log(targetObject != null ? $"Detecting object: {targetObject.name}" : "No target object set for detection");

        GetRayData(out var originA, out var originB, out var originC, out var originD, out var castDir);

        bool hitFirst = Physics.Raycast(originA, castDir, out hit, magnetDetectLength);
        bool hitSecond = Physics.Raycast(originB, castDir, out hit2, magnetDetectLength);
        bool hitThird = Physics.Raycast(originC, castDir, out var hit3, magnetDetectLength);
        bool hitFourth = Physics.Raycast(originD, castDir, out var hit4, magnetDetectLength);
        
        if(hitFirst || hitSecond || hitThird || hitFourth)
        {
            
            if((hitFirst && hit.collider.gameObject == targetObject) || (hitSecond && hit2.collider.gameObject == targetObject) 
                || (hitThird && hit3.collider.gameObject == targetObject) || (hitFourth && hit4.collider.gameObject == targetObject))
            {
                Debug.Log("Desired object detected below magnet");
                craneGrabObjectScript.GrabObject(targetObject);
                isGrabbed = true;
                
                return DetectionResult.Target;
            }
            else
            {
                string hitName = hitFirst ? hit.collider.gameObject.name : hitSecond ? hit2.collider.gameObject.name 
                    : hitThird ? hit3.collider.gameObject.name : hit4.collider.gameObject.name;
                Debug.Log($"Object detected below magnet: {hitName}, but it is not the desired object - bouncing off!");
                return DetectionResult.Wrong;
            }
        }
        else
        {
            Debug.Log("Raycast did not hit anything on grabLayerMask");
        }

        return DetectionResult.None;
    }

    #region Coroutine Animations
    private IEnumerator AnimateMagnet(GameObject magnet, Vector3 targetPosition, float duration, bool magnetRetract = true)
    {
        LockOrUnlockMovement(true);
        Vector3 startPosition = magnet.transform.localPosition;
        targetPosition = new Vector3(magnet.transform.localPosition.x, magnetExtendHeight, magnet.transform.localPosition.z);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            magnet.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            
            // Check continuously during extension for objects below
            DetectionResult detectionResult = DetectDesiredObjectBelow();
            
            // If hit wrong object, bounce back immediately
            if (detectionResult == DetectionResult.Wrong && elapsed > 0.1f) // Small delay to avoid instant bounce
            {
                Debug.Log("Hit wrong object during extension - bouncing back!");
                isExtending = false;
                
                if (retractCoroutine != null)
                {
                    StopCoroutine(retractCoroutine);
                }
                retractCoroutine = StartCoroutine(RetractMagnet(magnet, startPosition, duration * 0.5f));
                yield break;
            }
            else if (detectionResult == DetectionResult.Target) // Target found
            {
                isExtending = false;
                if (magnetRetract)
                {
                    if (retractCoroutine != null)
                    {
                        StopCoroutine(retractCoroutine);
                    }
                    retractCoroutine = StartCoroutine(RetractMagnet(magnet, startPosition, duration));
                }
                yield break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        magnet.transform.localPosition = targetPosition;

        // Final check at full extension
        DetectionResult finalCheck = DetectDesiredObjectBelow();
        
        if (magnetRetract)
        {
            if (retractCoroutine != null)
            {
                StopCoroutine(retractCoroutine);
            }
            float retractDuration = finalCheck == DetectionResult.Wrong ? duration * 0.5f : duration;
            retractCoroutine = StartCoroutine(RetractMagnet(magnet, startPosition, retractDuration));
        }
        else
        {
            isExtending = false;
        }
    }

    private IEnumerator RetractMagnet(GameObject magnet, Vector3 originalPosition, float duration)
    {
        Vector3 startPosition = magnet.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            magnet.transform.localPosition = Vector3.Lerp(startPosition, originalPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if(!isCompleted)
            magnet.transform.localPosition = originalPosition;
        
        isExtending = false;
        
        if(isGrabbed)
        {
            isAutomatedMovement = true;
            LockOrUnlockMovement(false);
            
            // Convert world position of target drop zone to local space
            Vector3 targetWorldPos = targetDropZone.transform.position;
            Vector3 targetLocalPos = targetWorldPos;
            
            // If crane parts have a parent, convert world to local
            if (craneParts.Count > 0 && craneParts[0].partObject != null && craneParts[0].partObject.transform.parent != null)
            {
                targetLocalPos = craneParts[0].partObject.transform.parent.InverseTransformPoint(targetWorldPos);
            }

            yield return StartCoroutine(MoveCraneToPosition(craneParts[1].partObject, new Vector3(0, 0, targetLocalPos.z), 1));
            yield return StartCoroutine(MoveCraneToPosition(craneParts[0].partObject, new Vector3(targetLocalPos.x, 0, 0), 1));
            yield return new WaitForSeconds(0.5f);
            
            // Lower magnet to place the object - extend down to drop position
            Vector3 dropStartPos = magnetExtender.transform.localPosition;
            Vector3 dropTargetPos = new Vector3(dropStartPos.x, targetDropZone.transform.position.y, dropStartPos.z);
            float dropElapsed = 0f;
            float dropDuration = 1f;
            bool droppedEarly = false;
            
            while (dropElapsed < dropDuration && !droppedEarly)
            {
                magnetExtender.transform.localPosition = Vector3.Lerp(dropStartPos, dropTargetPos, dropElapsed / dropDuration);
                
                // Check if object hits something during descent
                if (Physics.Raycast(magnetExtender.transform.position, Vector3.down, out RaycastHit hitInfo, 2f, grabLayerMask))
                {
                    // If we hit something that's not the target drop zone, drop here
                    if (hitInfo.collider.gameObject != targetDropZone)
                    {
                        Debug.Log($"Object hit {hitInfo.collider.gameObject.name} during descent - dropping here!");
                        droppedEarly = true;
                        StartCoroutine(ReturnCraneToStartPosition(craneParts[1].partObject, magnetStartPos, 2f));
                        break;

                    }
                }
                
                dropElapsed += Time.deltaTime;
                yield return null;
            }
            
            magnetExtender.transform.localPosition = new Vector3(dropStartPos.x, magnetExtender.transform.localPosition.y, dropStartPos.z);
            
            // Release the object before clearing reference
            if (craneGrabObjectScript != null && targetObject != null)
            {
                craneGrabObjectScript.ReleaseObject(targetObject);
            }
            
            // Clear grab state
            GameObject releasedObject = targetObject;
            isGrabbed = false;
            targetObject = null;
            
            // Retract to original position after releasing the object
            yield return StartCoroutine(RetractMagnet(magnetExtender, originalPosition, 1f));

            isAutomatedMovement = false;
            isCompleted = true;
            EndPuzzle();
        } 
        else 
        {
            LockOrUnlockMovement(false); // Unlock movement if nothing was grabbed
        }
       
    }

    private IEnumerator ReturnCraneToStartPosition(GameObject crane, Vector3 startPosition, float duration)
    {
        Vector3 currentPos = crane.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            crane.transform.localPosition = Vector3.Lerp(currentPos, startPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        crane.transform.localPosition = startPosition;
    }

    private IEnumerator MoveCraneToPosition(GameObject crane, Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = crane.transform.localPosition;
        CranePart cranePart = craneParts.Find(p => p.partObject == crane);

        Vector3 finalTarget = new Vector3(
            cranePart.moveX ? targetPosition.x : startPosition.x,
            cranePart.moveY ? targetPosition.y : startPosition.y,
            cranePart.moveZ ? targetPosition.z : startPosition.z
        );
        
        if (cranePart.moveX)
            finalTarget.x = Mathf.Clamp(finalTarget.x, cranePart.minX, cranePart.maxX);
        if (cranePart.moveY)
            finalTarget.y = Mathf.Clamp(finalTarget.y, cranePart.minY, cranePart.maxY);
        if (cranePart.moveZ)
            finalTarget.z = Mathf.Clamp(finalTarget.z, cranePart.minZ, cranePart.maxZ);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            crane.transform.localPosition = Vector3.Lerp(startPosition, finalTarget, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        crane.transform.localPosition = finalTarget;
    }
    #endregion
    
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

    private void LockOrUnlockMovement(bool lockMovement)
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

    #region Utility Scripts
    // Swaps action maps
    private void SwapActionMaps(string actionMapName)
    {
        InputReader.Instance._playerInput.SwitchCurrentActionMap(actionMapName);
    }

    // Checks for confirm input to start magnet extension
    private void CheckForConfirm()
    {
        if (_confirmPuzzleAction.action.triggered && targetObject != null && !isExtending)
        {
            isExtending = true;
            StartCoroutine(AnimateMagnet(magnetExtender, new Vector3(targetObject.transform.position.x, magnetExtender.transform.position.y, targetObject.transform.position.z), 2f, true));
        }
    }

    private void GetRayData(out Vector3 originA, out Vector3 originB, out Vector3 originC, out Vector3 originD, out Vector3 castDir)
    {
        Vector3 offset = magnetExtender.transform.TransformDirection(Vector3.forward * 2f);
        Vector3 offset2 = magnetExtender.transform.TransformDirection(Vector3.right * 2f);
        originA = magnetExtender.transform.position + offset;
        originB = magnetExtender.transform.position - offset;
        originC = magnetExtender.transform.position + offset2;
        originD = magnetExtender.transform.position - offset2;
        castDir = magnetExtender.transform.TransformDirection(Vector3.down);
    }

    private void AssignRayData()
    {
        GetRayData(out var originA, out var originB, out var originC, out var originD, out var castDir);

        if (Physics.Raycast(originA, castDir, out var dbgHitA, magnetDetectLength, grabLayerMask))
        {
            Debug.DrawRay(originA, castDir * dbgHitA.distance, dbgHitA.collider.gameObject == targetObject ? Color.cyan : Color.red);
        }
        else
        {
            Debug.DrawRay(originA, castDir * magnetDetectLength, Color.yellow);
        }

        if (Physics.Raycast(originB, castDir, out var dbgHitB, magnetDetectLength, grabLayerMask))
        {
            Debug.DrawRay(originB, castDir * dbgHitB.distance, dbgHitB.collider.gameObject == targetObject ? Color.cyan : Color.red);
        }
        else
        {
            Debug.DrawRay(originB, castDir * magnetDetectLength, Color.yellow);
        }

        if (Physics.Raycast(originC, castDir, out var dbgHitC, magnetDetectLength, grabLayerMask))
        {
            Debug.DrawRay(originC, castDir * dbgHitC.distance, dbgHitC.collider.gameObject == targetObject ? Color.cyan : Color.red);
        }
        else
        {
            Debug.DrawRay(originC, castDir * magnetDetectLength, Color.yellow);
        }

        if (Physics.Raycast(originD, castDir, out var dbgHitD, magnetDetectLength, grabLayerMask))
        {
            Debug.DrawRay(originD, castDir * dbgHitD.distance, dbgHitD.collider.gameObject == targetObject ? Color.cyan : Color.red);
        }
        else
        {
            Debug.DrawRay(originD, castDir * magnetDetectLength, Color.yellow);
        }
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