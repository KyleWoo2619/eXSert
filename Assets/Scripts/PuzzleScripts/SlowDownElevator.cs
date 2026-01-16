using System.Collections;
using UnityEngine;

/// <summary>
/// Handles elevator deceleration puzzle mechanics.
/// Smoothly slows down the elevator, then triggers rail and platform animations.
/// </summary>
public class SlowDownElevator : MonoBehaviour, IPuzzleInterface
{
    [Header("Required References")]
    [SerializeField] private ElevatorWalls _elevatorWalls;
    [SerializeField] private PlaySoundThroughManager elevatorAmbience;

    [Header("Deceleration")]
    [SerializeField] [Range(0.1f, 10f)] private float decelerationDuration = 2f;

    [Header("Rail Drop")]
    [SerializeField] private GameObject railToGoDown;
    [SerializeField] [Range(0.1f, 10f)] private float railDropDuration = 3.5f;

    [Header("Platform Extension")]
    [SerializeField] private GameObject platformToExtend;
    [SerializeField] [Range(0.1f, 10f)] private float platformExtendDuration = 3.5f;

    [Header("Animation Timing")]
    [SerializeField] [Range(0f, 5f)] private float delayBeforeDrop = 0.5f;
    [SerializeField] [Range(0f, 5f)] private float delayBetweenAnimations = 0.5f;

    // Internal state
    internal bool _isDecelerating = false;
    private float _initialSpeed = 0f;
    private float _decelerationTimer = 0f;
    private float _actualDecelerationDuration = 0f;
    private float _totalDecelerationDistance = 0f;
    private float _initialDoorWallY = 0f;
    private float _initialBelowWallY = 0f;
    private bool _soundFadeStarted = false;
    
    public bool isCompleted { get; set; } = false;

    private void Awake()
    {
        // Ensure the puzzle starts idle until explicitly triggered
        _isDecelerating = false;
        _soundFadeStarted = false;
        _decelerationTimer = 0f;
        _initialSpeed = 0f;
    }

    private void Update()
    {
        if (_isDecelerating)
        {
            SlowDownOnInteract();
        }
    }


    /// <summary>
    /// Initiates the elevator deceleration puzzle.
    /// </summary>
    public void StartPuzzle()
    {
        if (_elevatorWalls == null)
        {
            Debug.LogError("[SlowDownElevator] ElevatorWalls reference is missing!");
            return;
        }
        
        _soundFadeStarted = false;
        _decelerationTimer = 0f;
        _isDecelerating = true;
        _initialSpeed = _elevatorWalls.elevatorSpeed;
        
        // Store initial wall positions
        if(_elevatorWalls.wallWithDoor != null)
        {
            _initialDoorWallY = _elevatorWalls.wallWithDoor.transform.position.y;
        }
        if(_elevatorWalls.wallBelow != null)
        {
            _initialBelowWallY = _elevatorWalls.wallBelow.transform.position.y;
        }

        // Calculate deceleration duration based on wall Y distance to endYPos
        // Account for wrapping if wall is already below endYPos
        _totalDecelerationDistance = _elevatorWalls.endYPos - _initialDoorWallY;
        if(_totalDecelerationDistance < 0)
        {
            // Wall is below endYPos, calculate wrap-around distance
            // Distance = distance to bottom + distance from top to target
            float distanceToBottom = Mathf.Abs(_initialDoorWallY - _elevatorWalls.yBounds);
            float distanceFromTopToTarget = _elevatorWalls.restartPoint - _elevatorWalls.endYPos;
            _totalDecelerationDistance = distanceToBottom + distanceFromTopToTarget;
        }
        _actualDecelerationDuration = (_initialSpeed > 0.01f) ? (2f * _totalDecelerationDistance / _initialSpeed) : decelerationDuration;
    }

    /// <summary>
    /// Called when the puzzle ends. Placeholder for future logic.
    /// </summary>
    public void EndPuzzle()
    {
        // Puzzle end logic (if needed in future)
    }

    /// <summary>
    /// Updates the elevator deceleration over time.
    /// Smoothly reduces elevator speed and triggers follow-up animations when complete.
    /// </summary>
    private void SlowDownOnInteract()
    {
        // Start fading sound when deceleration begins
        if(!_soundFadeStarted && elevatorAmbience != null)
        {
            elevatorAmbience.StopSound();
            _soundFadeStarted = true;
        }

        _decelerationTimer += Time.deltaTime;
        float decelerationProgress = Mathf.Clamp01(_decelerationTimer / _actualDecelerationDuration);
        
        // Apply ease-out quadratic curve for smooth deceleration (starts fast, ends slow)
        float easedProgress = 1f - (1f - decelerationProgress) * (1f - decelerationProgress);
        
        if(_elevatorWalls != null)
        {
            // Stop ElevatorWalls script from moving the walls 
            _elevatorWalls.elevatorSpeed = 0f;
            
            // Calculate distance traveled based on eased deceleration progress
            float distanceTraveled = _totalDecelerationDistance * easedProgress;
            
            // Move wallWithDoor downward by distance traveled, handling wrap-around
            if(_elevatorWalls.wallWithDoor != null)
            {
                Vector3 doorPos = _elevatorWalls.wallWithDoor.transform.position;
                float targetY = _elevatorWalls.endYPos;
                
                // If fully complete, go to exact target
                if(decelerationProgress >= 1f)
                {
                    doorPos.y = targetY;
                }
                else
                {
                    float newY = _initialDoorWallY - distanceTraveled;
                    
                    // Handle wrapping if we go below yBounds
                    if(newY <= _elevatorWalls.yBounds)
                    {
                        float excessDistance = _elevatorWalls.yBounds - newY;
                        newY = _elevatorWalls.restartPoint - excessDistance;
                    }
                    
                    doorPos.y = newY;
                }
                
                _elevatorWalls.wallWithDoor.transform.position = doorPos;
            }
            
            // Move wallBelow to maintain relative offset (one wall height below)
            if(_elevatorWalls.wallBelow != null)
            {
                Vector3 belowPos = _elevatorWalls.wallBelow.transform.position;
                float wallHeight = _initialBelowWallY - _initialDoorWallY;
                float targetY = _elevatorWalls.endYPos + wallHeight;
                
                // If fully complete, go to exact target
                if(decelerationProgress >= 1f)
                {
                    belowPos.y = targetY;
                }
                else
                {
                    float newY = _initialBelowWallY - distanceTraveled;
                    
                    // Handle wrapping if we go below yBounds
                    if(newY <= _elevatorWalls.yBounds)
                    {
                        float excessDistance = _elevatorWalls.yBounds - newY;
                        newY = _elevatorWalls.restartPoint - excessDistance;
                    }
                    
                    belowPos.y = newY;
                }
                
                _elevatorWalls.wallBelow.transform.position = belowPos;
            }
        }
        
        if(decelerationProgress >= 1f)
        {
            _isDecelerating = false;
            isCompleted = true;
            StartCoroutine(DropRailAndExtendPlatform());
        }
    }

    /// <summary>
    /// Coroutine that sequences the rail drop and platform extension animations.
    /// </summary>
    private IEnumerator DropRailAndExtendPlatform()
    {
        yield return new WaitForSeconds(delayBeforeDrop);
        yield return StartCoroutine(AnimateObject(railToGoDown, Vector3.down * 5, railDropDuration, "Rail dropped!"));
        yield return new WaitForSeconds(delayBetweenAnimations);
        yield return StartCoroutine(AnimateObject(platformToExtend, Vector3.forward * 3, platformExtendDuration, "Platform extended!"));
    }

    /// <summary>
    /// Smoothly animates an object to a new position using easing.
    /// </summary>
    private IEnumerator AnimateObject(GameObject targetObject, Vector3 movement, float duration, string completionMessage)
    {
        if(targetObject == null)
        {
            Debug.LogError("[SlowDownElevator] Target object is null!");
            yield break;
        }

        Vector3 startPosition = targetObject.transform.position;
        Vector3 endPosition = startPosition + movement;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = EaseOutCubic(progress);
            targetObject.transform.position = Vector3.Lerp(startPosition, endPosition, easedProgress);
            yield return null;
        }

        targetObject.transform.position = endPosition;
    }

    /// <summary>
    /// Easing function for smooth deceleration effect.
    /// Provides cubic easing out curve (fast start, slow finish).
    /// </summary>
    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
