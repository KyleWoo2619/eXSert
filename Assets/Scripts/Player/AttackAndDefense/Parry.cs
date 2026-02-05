using System.Collections;
using UnityEngine;
using Utilities.Combat;
using UnityEngine.VFX;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Parry : MonoBehaviour
{
    [SerializeField] private AudioClip parrySoundEffect;
    [SerializeField] private VisualEffectAsset parryEffect;

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
        }
    }
}
