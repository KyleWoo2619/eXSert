using UnityEngine;
using UnityEngine.SceneManagement;
using EnemyBehavior.Boss;

public class BossDropAndLoadOnDeath : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private BossHealth bossHealth;

    [Header("Drop")]
    [SerializeField] private GameObject cardToEnable;

    [Header("Scene")]
    [SerializeField] private bool loadSceneOnDrop = true;
    [SerializeField] private string sceneName = "Conservatory";

    private bool triggered;

    private void Update()
    {
        if (triggered || bossHealth == null)
            return;

        if (bossHealth.currentHP > 0f)
            return;

        triggered = true;
        SpawnCard();

        if (loadSceneOnDrop)
            LoadScene();
    }

    private void SpawnCard()
    {
        if (cardToEnable == null)
            return;

        cardToEnable.SetActive(true);
    }

    private void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAdditive(sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }
}
