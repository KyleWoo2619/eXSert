using UnityEngine;
using UnityEngine.SceneManagement;

/*
Written by Kyle Woo
Keeps the HUD GameObject persistent across scene loads.
Place this on the top-level HUD object (with a Canvas component).
*/

[RequireComponent(typeof(Canvas))]
public class HUDPersistence : MonoBehaviour
{
    private static HUDPersistence instance;
    private Canvas canvas;

    void Awake()
    {
        if (instance && instance != this) { Destroy(gameObject); return; }
        instance = this;

        canvas = GetComponent<Canvas>();
        DontDestroyOnLoad(gameObject);

        RebindCamera(); // bind once for the current scene
    }

    void OnEnable()  => SceneManager.activeSceneChanged += OnSceneChanged;
    void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;

    private void OnSceneChanged(Scene prev, Scene next) => RebindCamera();

    private void RebindCamera()
    {
        if (!canvas) return;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            canvas.worldCamera = null; // overlay ignores camera
            return;
        }

        var cam = Camera.main; // requires your active camera to have the "MainCamera" tag
        if (cam) canvas.worldCamera = cam;
        else Debug.LogWarning("[HUDPersistence] No Camera.main found to bind HUD canvas.");
    }
}
