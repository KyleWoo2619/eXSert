using UnityEngine;

public static class ProjectileHierarchy
{
    private static Transform activeEnemyProjectiles;

    // Scene-level parent for all active enemy projectiles (for cleaner hierarchy)
    public static Transform GetActiveEnemyProjectilesParent()
    {
        if (activeEnemyProjectiles == null)
        {
            var go = GameObject.Find("ActiveEnemyProjectiles");
            if (go == null)
            {
                go = new GameObject("ActiveEnemyProjectiles");
            }
            activeEnemyProjectiles = go.transform;
        }
        return activeEnemyProjectiles;
    }
}