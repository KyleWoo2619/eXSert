using System;
using System.Collections;
using System.Collections.Generic;
using eXsert;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI.Loading
{
    /// <summary>
    /// Controls the blackout/prop showcase loading screen. Lives inside the LoadingScene and persists via DontDestroyOnLoad.
    /// </summary>
    public sealed class LoadingScreenController : MonoBehaviour
    {
        public static LoadingScreenController Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        [Header("Scene References")]
        [SerializeField] private CanvasGroup blackoutCanvasGroup;
        [SerializeField] private GameObject loadingCanvasRoot;
        [SerializeField] private LoadingPropManager propManager;
        [SerializeField, Tooltip("Optional objects that should also persist when the loading scene becomes DontDestroyOnLoad.")]
        private List<GameObject> additionalPersistentRoots = new();

        [Header("Timings")]
        [SerializeField, Range(0.05f, 2f)] private float fadeDuration = 0.35f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField, Range(0f, 60f)] private float minimumDisplaySeconds = 10f;

        [Header("Input")]
        [SerializeField, Tooltip("If true, the player's PlayerInput will switch to the Loading action map while the screen is visible.")]
        private bool handOffPlayerInputMap = true;
        [SerializeField, Tooltip("Time (0-1 normalized) into the fade-out at which gameplay should resume so the player never sees a frozen scene.")]
        [Range(0.1f, 0.95f)] private float resumeThresholdNormalized = 0.3f;

        private PlayerControls loadingControls;
        private string previousActionMap;
        private bool playerInputMapSwitched;
        private Coroutine activeRoutine;
        private float previousTimeScale = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (blackoutCanvasGroup != null)
            {
                blackoutCanvasGroup.alpha = 0f;
            }

            if (loadingCanvasRoot != null)
            {
                loadingCanvasRoot.SetActive(false);
            }

            DontDestroyOnLoad(gameObject);
            foreach (var root in additionalPersistentRoots)
            {
                if (root != null)
                    DontDestroyOnLoad(root);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Begins the loading workflow. The supplied routine performs the actual scene loading work.
        /// </summary>
        public void BeginLoading(IEnumerator loadSteps, bool pauseGame = true, float? minimumDisplayOverride = null)
        {
            if (!isActiveAndEnabled)
            {
                if (loadSteps != null)
                {
                    if (SceneLoader.Instance != null)
                        SceneLoader.Instance.StartCoroutine(loadSteps);
                    else
                        StartCoroutine(loadSteps);
                }
                return;
            }

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            float targetMinimumDisplay = minimumDisplayOverride ?? minimumDisplaySeconds;
            activeRoutine = StartCoroutine(RunLoadingSequence(loadSteps, pauseGame, targetMinimumDisplay));
        }

        private IEnumerator RunLoadingSequence(IEnumerator loadSteps, bool pauseGame, float minimumDisplayDuration)
        {
            previousTimeScale = Time.timeScale;
            if (pauseGame)
                Time.timeScale = 0f;

            EnableLoadingInput();
            SwitchPlayerInputToLoading();

            bool enforceMinimum = minimumDisplayDuration > 0f;
            float minDisplayEndTime = 0f;

            yield return FadeBlack(0f, 1f);

            if (loadingCanvasRoot != null)
                loadingCanvasRoot.SetActive(true);

            propManager?.ShowRandomProp();

            yield return FadeBlack(1f, 0f);
            if (enforceMinimum)
                minDisplayEndTime = Time.unscaledTime + minimumDisplayDuration;

            if (loadSteps != null)
                yield return StartCoroutine(loadSteps);

            if (enforceMinimum)
            {
                float remaining = minDisplayEndTime - Time.unscaledTime;
                if (remaining > 0f)
                    yield return new WaitForSecondsRealtime(remaining);
            }

            yield return FadeBlack(0f, 1f);

            propManager?.ClearProp();
            if (loadingCanvasRoot != null)
                loadingCanvasRoot.SetActive(false);

            yield return FadeOutAndResume(pauseGame);

            DisableLoadingInput();
            RestorePlayerInputMap();
            activeRoutine = null;
        }

        private IEnumerator FadeBlack(float from, float to)
        {
            if (blackoutCanvasGroup == null)
                yield break;

            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / fadeDuration);
                float curved = fadeCurve.Evaluate(t);
                blackoutCanvasGroup.alpha = Mathf.Lerp(from, to, curved);
                yield return null;
            }

            blackoutCanvasGroup.alpha = to;
        }

        private IEnumerator FadeOutAndResume(bool pauseGame)
        {
            if (blackoutCanvasGroup == null)
            {
                if (pauseGame)
                    Time.timeScale = previousTimeScale;
                yield break;
            }

            float timer = 0f;
            bool resumed = !pauseGame;
            float resumeThreshold = Mathf.Clamp(resumeThresholdNormalized, 0.05f, 0.95f);

            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / fadeDuration);
                float curved = fadeCurve.Evaluate(t);
                blackoutCanvasGroup.alpha = Mathf.Lerp(1f, 0f, curved);

                if (!resumed && t >= resumeThreshold)
                {
                    Time.timeScale = previousTimeScale;
                    resumed = true;
                }

                yield return null;
            }

            blackoutCanvasGroup.alpha = 0f;

            if (!resumed)
                Time.timeScale = previousTimeScale;
        }

        private void EnableLoadingInput()
        {
            if (loadingControls == null)
            {
                loadingControls = new PlayerControls();
            }

            loadingControls.Loading.Enable();
            loadingControls.Loading.Look.performed += HandleLookPerformed;
            loadingControls.Loading.Look.canceled += HandleLookPerformed;
            loadingControls.Loading.Zoom.performed += HandleZoomPerformed;
            loadingControls.Loading.Zoom.canceled += HandleZoomPerformed;
        }

        private void DisableLoadingInput()
        {
            if (loadingControls == null)
                return;

            loadingControls.Loading.Look.performed -= HandleLookPerformed;
            loadingControls.Loading.Look.canceled -= HandleLookPerformed;
            loadingControls.Loading.Zoom.performed -= HandleZoomPerformed;
            loadingControls.Loading.Zoom.canceled -= HandleZoomPerformed;
            loadingControls.Loading.Disable();
            propManager?.SetLookInput(Vector2.zero);
            propManager?.SetZoomInput(0f);
        }

        private void HandleLookPerformed(InputAction.CallbackContext context)
        {
            propManager?.SetLookInput(context.ReadValue<Vector2>());
        }

        private void HandleZoomPerformed(InputAction.CallbackContext context)
        {
            propManager?.SetZoomInput(context.ReadValue<float>());
        }

        private void SwitchPlayerInputToLoading()
        {
            if (!handOffPlayerInputMap || InputReader.playerInput == null)
                return;

            try
            {
                previousActionMap = InputReader.playerInput.currentActionMap != null
                    ? InputReader.playerInput.currentActionMap.name
                    : "Gameplay";
                InputReader.playerInput.SwitchCurrentActionMap("Loading");
                playerInputMapSwitched = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LoadingScreen] Failed to switch PlayerInput to Loading map: {ex.Message}");
                playerInputMapSwitched = false;
            }
        }

        private void RestorePlayerInputMap()
        {
            if (!handOffPlayerInputMap || InputReader.playerInput == null)
            {
                previousActionMap = string.Empty;
                playerInputMapSwitched = false;
                return;
            }

            string fallback = string.IsNullOrEmpty(previousActionMap) ? "Gameplay" : previousActionMap;
            bool switched = false;

            if (playerInputMapSwitched)
            {
                switched = TrySwitchActionMap(fallback);
            }

            if (!switched && !string.Equals(fallback, "Gameplay", StringComparison.OrdinalIgnoreCase))
            {
                switched = TrySwitchActionMap("Gameplay");
            }

            if (!switched)
            {
                Debug.LogWarning("[LoadingScreen] Unable to restore previous PlayerInput map; forcing Gameplay via InputReader rebind.");
            }

            InputReader.Instance?.RebindTo(InputReader.playerInput, switchToGameplay: true);
            previousActionMap = string.Empty;
            playerInputMapSwitched = false;

            bool TrySwitchActionMap(string mapName)
            {
                try
                {
                    InputReader.playerInput.SwitchCurrentActionMap(mapName);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LoadingScreen] Failed to switch PlayerInput map to '{mapName}': {ex.Message}");
                    return false;
                }
            }
        }


    }
}
