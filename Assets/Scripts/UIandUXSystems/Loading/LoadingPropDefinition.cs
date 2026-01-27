using UnityEngine;

namespace UI.Loading
{
    /// <summary>
    /// Data container that drives the prop showcase on the loading screen.
    /// </summary>
    [CreateAssetMenu(menuName = "Loading/Prop Definition", fileName = "NewLoadingProp" )]
    public sealed class LoadingPropDefinition : ScriptableObject
    {
        [Header("Presentation")]
        [Tooltip("Optional friendly name shown above the description.")]
        public string displayName;

        [Tooltip("Lore or flavor text that will be displayed next to the prop render.")]
        [TextArea(2, 4)]
        public string description;

        [Header("Prefab")]
        [Tooltip("Prefab that will be instantiated inside the loading scene showcase.")]
        public GameObject propPrefab;

        [Header("Spawn Offsets")]
        [Tooltip("Local position offset applied after the prefab is instantiated.")]
        public Vector3 localPositionOffset;

        [Tooltip("Optional local rotation override.")]
        public Vector3 localEulerOffset;

        [Tooltip("Scale multiplier applied to the instantiated prefab.")]
        public float scaleMultiplier = 1f;

        [Header("Camera")]
        [Tooltip("Preferred zoom distance for this prop. Used as the starting camera offset.")]
        public float preferredZoom = 3f;
    }
}
