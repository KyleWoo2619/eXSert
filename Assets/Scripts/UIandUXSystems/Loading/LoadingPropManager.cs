using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.Loading
{
    /// <summary>
    /// Handles spawning and manipulating the showcase prop during the loading screen.
    /// </summary>
    public sealed class LoadingPropManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField]
        private List<LoadingPropDefinition> propDefinitions = new();

        [SerializeField]
        [Tooltip("Where instantiated props will be parented.")]
        private Transform propParent;

        [SerializeField]
        [Tooltip("Optional transform used for spinning the prop. Defaults to propParent.")]
        private Transform rotationPivot;

        [SerializeField]
        [Tooltip("Camera rig that will dolly along the Z axis when zoom input is received.")]
        private Transform cameraRig;

        [SerializeField]
        private Vector2 zoomRange = new(1.5f, 6f);

        [SerializeField]
        private float zoomSpeed = 3f;

        [SerializeField]
        private float rotationSpeed = 120f;

        [SerializeField]
        [Tooltip("How quickly zoom changes apply to the rig (units per second).")]
        private float zoomStepSize = 3f;

        [SerializeField]
        private TMP_Text descriptionLabel;

        private GameObject activeProp;
        private LoadingPropDefinition activeDefinition;
        private Vector2 lookInput;
        private float zoomInput;
        private float targetZoom;
        private float currentZoom;
        private Vector3 initialCameraLocalPos;

        private Transform RotationPivot => rotationPivot != null ? rotationPivot : propParent;

        private void Awake()
        {
            if (cameraRig != null)
            {
                initialCameraLocalPos = cameraRig.localPosition;
                currentZoom = targetZoom = Mathf.Abs(cameraRig.localPosition.z);
            }
        }

        private void OnDisable()
        {
            ClearProp();
            lookInput = Vector2.zero;
            zoomInput = 0f;
        }

        private void LateUpdate()
        {
            float delta = Time.unscaledDeltaTime;

            if (RotationPivot != null && activeProp != null)
            {
                if (lookInput.sqrMagnitude > 0.0001f)
                {
                    RotationPivot.Rotate(
                        Vector3.up,
                        lookInput.x * rotationSpeed * delta,
                        Space.World
                    );
                    RotationPivot.Rotate(
                        Vector3.right,
                        -lookInput.y * rotationSpeed * delta,
                        Space.Self
                    );
                }
            }

            if (cameraRig != null)
            {
                if (Mathf.Abs(zoomInput) > 0.001f)
                {
                    targetZoom = Mathf.Clamp(
                        targetZoom + zoomInput * zoomStepSize * delta,
                        zoomRange.x,
                        zoomRange.y
                    );
                }

                currentZoom = Mathf.MoveTowards(currentZoom, targetZoom, zoomSpeed * delta);
                Vector3 localPos = initialCameraLocalPos;
                localPos.z = -currentZoom;
                cameraRig.localPosition = localPos;
            }
        }

        public void SetLookInput(Vector2 value)
        {
            lookInput = value;
        }

        public void SetZoomInput(float value)
        {
            zoomInput = value;
        }

        public void ShowRandomProp()
        {
            if (propDefinitions == null || propDefinitions.Count == 0)
            {
                ClearProp();
                return;
            }

            int index = Random.Range(0, propDefinitions.Count);
            ShowProp(propDefinitions[index]);
        }

        public void ShowProp(LoadingPropDefinition definition)
        {
            ClearProp();
            activeDefinition = definition;
            if (definition == null || definition.propPrefab == null)
            {
                if (descriptionLabel != null)
                    descriptionLabel.text = string.Empty;
                return;
            }

            Transform parent = propParent != null ? propParent : transform;
            activeProp = Instantiate(definition.propPrefab, parent);
            activeProp.transform.localPosition = definition.localPositionOffset;
            activeProp.transform.localRotation = Quaternion.Euler(definition.localEulerOffset);
            activeProp.transform.localScale =
                Vector3.one * Mathf.Max(0.01f, definition.scaleMultiplier);

            if (RotationPivot != null && RotationPivot != parent)
            {
                RotationPivot.localRotation = Quaternion.identity;
            }

            if (descriptionLabel != null)
            {
                if (string.IsNullOrEmpty(definition.displayName))
                    descriptionLabel.text = definition.description;
                else
                    descriptionLabel.text =
                        $"<b>{definition.displayName}</b>\n{definition.description}";
            }

            if (cameraRig != null)
            {
                targetZoom = currentZoom = Mathf.Clamp(
                    definition.preferredZoom,
                    zoomRange.x,
                    zoomRange.y
                );
            }
        }

        public void ClearProp()
        {
            if (activeProp != null)
            {
                Destroy(activeProp);
                activeProp = null;
            }
            activeDefinition = null;
            if (descriptionLabel != null)
                descriptionLabel.text = string.Empty;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Adds inspector buttons so designers can spawn/clear props while testing in Play Mode.
    /// </summary>
    [CustomEditor(typeof(LoadingPropManager))]
    public sealed class LoadingPropManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Spawn Random Prop"))
                {
                    foreach (Object targetObj in targets)
                    {
                        if (targetObj is LoadingPropManager manager)
                            manager.ShowRandomProp();
                    }
                }

                if (GUILayout.Button("Clear Prop"))
                {
                    foreach (Object targetObj in targets)
                    {
                        if (targetObj is LoadingPropManager manager)
                            manager.ClearProp();
                    }
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to use the debug buttons.",
                    MessageType.Info
                );
            }
        }
    }
#endif
}
