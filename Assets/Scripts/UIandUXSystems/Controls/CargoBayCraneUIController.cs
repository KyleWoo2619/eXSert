using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CargoBayCraneUIController : MonoBehaviour
{
    [Header("Control Scheme UI")]
    [SerializeField] private GameObject controllerIcon;

    [Header("Move Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField, Range(0.1f, 1f)] private float pressThreshold = 0.5f;

    [Header("Direction Icons")]
    [SerializeField] private GameObject forwardIcon;
    [SerializeField] private GameObject backIcon;
    [SerializeField] private GameObject leftIcon;
    [SerializeField] private GameObject rightIcon;

    [Header("Pop Animation")]
    [SerializeField, Range(1f, 2f)] private float popScale = 1.15f;
    [SerializeField, Range(0.05f, 0.5f)] private float popDuration = 0.12f;
    [SerializeField] private bool useUnscaledTime = true;

    private readonly Dictionary<GameObject, Vector3> baseScales = new Dictionary<GameObject, Vector3>();
    private readonly Dictionary<GameObject, Coroutine> popRoutines = new Dictionary<GameObject, Coroutine>();

    private InputAction runtimeMoveAction;

    private bool lastUp;
    private bool lastDown;
    private bool lastLeft;
    private bool lastRight;

    private void Awake()
    {
        CacheBaseScale(forwardIcon);
        CacheBaseScale(backIcon);
        CacheBaseScale(leftIcon);
        CacheBaseScale(rightIcon);
    }

    private void OnEnable()
    {
        runtimeMoveAction = ResolveRuntimeMoveAction();
        RefreshControllerIcon();
    }

    private void Update()
    {
        RefreshControllerIcon();
        UpdateDirectionPop();
    }

    private void RefreshControllerIcon()
    {
        if (controllerIcon == null)
            return;

        bool isGamepad = string.Equals(InputReader.activeControlScheme, "Gamepad");
        controllerIcon.SetActive(isGamepad);
    }

    private void UpdateDirectionPop()
    {
        InputAction action = runtimeMoveAction ?? moveAction?.action;
        if (action == null)
            return;

        Vector2 move = action.ReadValue<Vector2>();
        bool up = move.y >= pressThreshold;
        bool down = move.y <= -pressThreshold;
        bool left = move.x <= -pressThreshold;
        bool right = move.x >= pressThreshold;

        if (up && !lastUp)
            PlayPop(forwardIcon);
        if (down && !lastDown)
            PlayPop(backIcon);
        if (left && !lastLeft)
            PlayPop(leftIcon);
        if (right && !lastRight)
            PlayPop(rightIcon);

        lastUp = up;
        lastDown = down;
        lastLeft = left;
        lastRight = right;
    }

    private InputAction ResolveRuntimeMoveAction()
    {
        if (InputReader.PlayerInput == null)
            return null;

        if (moveAction == null || moveAction.action == null)
            return null;

        var actions = InputReader.PlayerInput.actions;
        if (actions == null)
            return moveAction.action;

        var map = actions.FindActionMap("CranePuzzle");
        if (map == null)
            return moveAction.action;

        return map.FindAction(moveAction.action.name) ?? moveAction.action;
    }

    private void CacheBaseScale(GameObject target)
    {
        if (target == null || baseScales.ContainsKey(target))
            return;

        baseScales[target] = target.transform.localScale;
    }

    private void PlayPop(GameObject target)
    {
        if (target == null)
            return;

        CacheBaseScale(target);

        if (popRoutines.TryGetValue(target, out Coroutine routine) && routine != null)
            StopCoroutine(routine);

        popRoutines[target] = StartCoroutine(PopRoutine(target));
    }

    private IEnumerator PopRoutine(GameObject target)
    {
        if (!baseScales.TryGetValue(target, out Vector3 baseScale))
            baseScale = target.transform.localScale;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, popDuration);
        Vector3 startScale = baseScale * popScale;
        target.transform.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.transform.localScale = Vector3.Lerp(startScale, baseScale, t);
            yield return null;
        }

        target.transform.localScale = baseScale;
        popRoutines[target] = null;
    }
}
