using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-stop health bar widget for world-space enemy HP displays.
/// Finds the enemy's IHealthSystem, keeps a slider in sync, and billboards toward the active camera.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private Slider slider;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField, Tooltip("Attempt to bind to the closest IHealthSystem in the parent hierarchy on Awake.")]
    private bool autoBindParentHealth = true;

    [Header("Behavior")]
    [SerializeField, Tooltip("Hide the bar when the enemy is at full health to cut down on clutter.")]
    private bool hideWhenFull = true;
    [SerializeField, Tooltip("How quickly the slider value interpolates toward the real HP value.")]
    private float sliderLerpSpeed = 12f;

    private IHealthSystem health;
    private Transform enemyTransform;
    private Transform fallbackCameraTransform;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponentInChildren<Slider>(true);

        if (autoBindParentHealth)
        {
            BindToHealthSystem(GetComponentInParent<IHealthSystem>());
        }
    }

    private void LateUpdate()
    {
        if (health == null || enemyTransform == null || slider == null)
            return;

        UpdateSlider();
        AlignAboveEnemy();
        FaceActiveCamera();
    }

    /// <summary>
    /// Public hook so spawners can manually provide the enemy's health interface.
    /// </summary>
    public void BindToHealthSystem(IHealthSystem system)
    {
        health = system;
        enemyTransform = (system as Component)?.transform;

        if (health == null || enemyTransform == null)
        {
            Debug.LogWarning($"[EnemyHealthBar] Failed to bind IHealthSystem on {name}.", this);
            enabled = false;
            return;
        }

        slider.maxValue = health.maxHP;
        slider.value = health.currentHP;
    }

    private void UpdateSlider()
    {
        slider.maxValue = Mathf.Max(0.01f, health.maxHP);
        float target = Mathf.Clamp(health.currentHP, 0f, slider.maxValue);
        slider.value = Mathf.MoveTowards(slider.value, target, sliderLerpSpeed * Time.deltaTime * slider.maxValue);

        if (hideWhenFull && Mathf.Approximately(target, slider.maxValue))
        {
            if (slider.gameObject.activeSelf)
                slider.gameObject.SetActive(false);
        }
        else if (!slider.gameObject.activeSelf)
        {
            slider.gameObject.SetActive(true);
        }
    }

    private void AlignAboveEnemy()
    {
        transform.position = enemyTransform.position + worldOffset;
    }

    private void FaceActiveCamera()
    {
        Transform camTransform = ResolveCameraTransform();
        if (camTransform == null)
            return;

        Vector3 toCamera = camTransform.position - transform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
    }

    private Transform ResolveCameraTransform()
    {
        if (CameraManager.Instance != null)
        {
            CinemachineCamera cineCam = CameraManager.Instance.GetActiveCamera();
            if (cineCam != null)
                return cineCam.transform;
        }

        if (fallbackCameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                fallbackCameraTransform = mainCam.transform;
        }

        return fallbackCameraTransform;
    }
}
