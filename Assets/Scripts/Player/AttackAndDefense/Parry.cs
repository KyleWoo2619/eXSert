using System.Collections;
using UnityEngine;
using Utilities.Combat;
using UnityEngine.VFX;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Parry : MonoBehaviour
{
    [Header("Parry Effects")]
    [SerializeField] private AudioClip parrySoundEffect;
    [SerializeField] private VisualEffectAsset parryEffect;
    [SerializeField, Range(.5f, 2f)] private float parryEffectDuration = 1f;

    [Space(10)]
    [Header("Time Pause Settings")]
    [SerializeField] private bool pauseTimeOnParry = true;
    [SerializeField, Range(.01f, 1f)] private float parryPauseDuration = 0.05f;
    [SerializeField, Range(0f, 1f)] private float howSlowTimeScales = 0.5f;

    private void Awake()
    {
        CombatManager.OnSuccessfulParry += HandleSuccessfulParry;
    }

    private void HandleSuccessfulParry(BaseEnemy<EnemyState, EnemyTrigger> enemy)
    {
        if (parrySoundEffect != null)
            AudioSource.PlayClipAtPoint(parrySoundEffect, transform.position);

        if (parryEffect != null)
        {
            GameObject vfxInstance = new GameObject("ParryEffect");
            vfxInstance.transform.position = transform.position;

            VisualEffect visualEffect = vfxInstance.AddComponent<VisualEffect>();
            visualEffect.visualEffectAsset = parryEffect;
            visualEffect.Play();
            DestroyVFX(vfxInstance, parryEffectDuration);
        }

        if (pauseTimeOnParry)
        {
            StartCoroutine(PauseTimeOnParry(parryPauseDuration));
        }

        
    }

    private void DestroyVFX(GameObject vfxInstance, float delay)
    {
        Destroy(vfxInstance, delay);
    }

    private IEnumerator PauseTimeOnParry(float duration)
    {
        Time.timeScale = howSlowTimeScales;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
