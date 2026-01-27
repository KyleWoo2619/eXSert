/*
 * Written by Kyle W
 * 
 * Scrolls tread material Y offset to simulate movement.
 * Creates material instance per GameObject to avoid affecting shared materials.
 * Can use fixed speed or sync with NavMeshAgent velocity.
 */

using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TredMove : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField, Tooltip("Speed of the tread scroll. Positive = forward, Negative = backward")]
    private float scrollSpeed = 0.5f;
    
    [SerializeField, Tooltip("Material index to scroll (if renderer has multiple materials)")]
    private int materialIndex = 0;
    
    [Header("Optional: Link to NavMeshAgent")]
    [SerializeField, Tooltip("If assigned, scroll speed will be based on agent's velocity")]
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    
    [SerializeField, Tooltip("Multiplier for converting agent velocity to scroll speed")]
    private float velocityToScrollMultiplier = 0.1f;
    
    [SerializeField, Tooltip("Use NavMeshAgent velocity instead of fixed scroll speed")]
    private bool useAgentVelocity = false;
    
    private Material materialInstance;
    private Renderer targetRenderer;
    private float currentOffset = 0f;
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        
        if (targetRenderer != null && targetRenderer.materials.Length > materialIndex)
        {
            materialInstance = targetRenderer.materials[materialIndex];
            Debug.Log($"[TredMove] Created material instance for {gameObject.name}");
        }
        else
        {
            Debug.LogError($"[TredMove] Renderer or material index {materialIndex} not found on {gameObject.name}");
            enabled = false;
            return;
        }
        
        if (useAgentVelocity && navMeshAgent == null)
        {
            navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
            }
        }
    }

    private void Update()
    {
        if (materialInstance == null) return;

        float effectiveSpeed = scrollSpeed;

        if (useAgentVelocity && navMeshAgent != null)
        {
            effectiveSpeed = navMeshAgent.velocity.magnitude * velocityToScrollMultiplier;
        }

        currentOffset += effectiveSpeed * Time.deltaTime;

        if (currentOffset >= 1f)
        {
            currentOffset -= 1f;
        }
        else if (currentOffset < 0f)
        {
            currentOffset += 1f;
        }

        Vector2 offset = materialInstance.GetTextureOffset(MainTex);
        offset.y = currentOffset;
        materialInstance.SetTextureOffset(MainTex, offset);
    }

    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }

    public float GetScrollSpeed()
    {
        return useAgentVelocity && navMeshAgent != null 
            ? navMeshAgent.velocity.magnitude * velocityToScrollMultiplier 
            : scrollSpeed;
    }

    public void SetUseAgentVelocity(bool useVelocity)
    {
        useAgentVelocity = useVelocity;
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (useAgentVelocity && navMeshAgent == null)
        {
            navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
            }
        }
    }
#endif
}
