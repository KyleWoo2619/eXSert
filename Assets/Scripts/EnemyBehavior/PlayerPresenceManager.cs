using UnityEngine;

public class PlayerPresenceManager : MonoBehaviour
{
    public static PlayerPresenceManager Instance { get; private set; }

    public static bool IsPlayerPresent => Instance != null && Instance._playerTransform != null
        && Instance._playerTransform.gameObject.activeInHierarchy;

    public static Transform PlayerTransform => Instance != null ? Instance._playerTransform : null;

    [SerializeField]
    private Transform _playerTransform;

    [SerializeField]
    private float recheckInterval = 2f;

    private float _lastCheckTime;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        FindPlayer();
    }

    private void Update()
    {
        if (Time.time - _lastCheckTime > recheckInterval)
        {
            _lastCheckTime = Time.time;
            if (_playerTransform == null || !_playerTransform.gameObject.activeInHierarchy)
            {
                FindPlayer();
            }
        }
    }

    private void FindPlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        _playerTransform = playerObj != null ? playerObj.transform : null;
    }

    public void RegisterPlayer(Transform player)
    {
        _playerTransform = player;
    }

    public void UnregisterPlayer()
    {
        _playerTransform = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}