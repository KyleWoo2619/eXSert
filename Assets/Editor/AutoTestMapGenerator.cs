using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using System;

// Editor utility to generate an automated test map for EnemyBehavior pathfinding.
// Creates an AutoTestMap root, ground with specified transform, static obstacles, ramps,
// moving platforms/obstacles, and bakes a NavMeshSurface if available.

public class AutoTestMapGenerator : EditorWindow
{
    [MenuItem("Tools/EnemyBehavior/Generate Test Map")]
    public static void ShowWindow() => GetWindow<AutoTestMapGenerator>("Auto Test Map Generator");

    private int staticObstacles = 12;
    private int ramps = 4; // unused currently, kept for UI
    private int movingPlatforms = 3;
    private int movingObstacles = 3;
    private int agents = 6;
    private int seed = 12345;
    private float obstacleDensity = 1.0f; // multiplier
    private int corridorCount = 2;
    private float corridorWidth = 2f;
    private float platformLinger = 2f;

    void OnGUI()
    {
        GUILayout.Label("Auto Test Map Generator", EditorStyles.boldLabel);
        staticObstacles = EditorGUILayout.IntField("Static Obstacles", staticObstacles);
        ramps = EditorGUILayout.IntField("Ramps", ramps);
        movingPlatforms = EditorGUILayout.IntField("Moving Platforms", movingPlatforms);
        movingObstacles = EditorGUILayout.IntField("Moving Obstacles", movingObstacles);
        agents = EditorGUILayout.IntField("Agents", agents);
        seed = EditorGUILayout.IntField("Random Seed", seed);
        obstacleDensity = EditorGUILayout.Slider("Obstacle Density", obstacleDensity, 0.1f, 4f);
        corridorCount = EditorGUILayout.IntField("Corridor Count", corridorCount);
        corridorWidth = EditorGUILayout.FloatField("Corridor Width", corridorWidth);
        platformLinger = EditorGUILayout.FloatField("Platform Linger (s)", platformLinger);

        if (GUILayout.Button("Generate Map"))
        {
            if (EditorUtility.DisplayDialog("Confirm", "This will create/replace a GameObject named AutoTestMap in the scene. Continue?", "Yes", "No"))
                Generate();
        }

        if (GUILayout.Button("Run Tests"))
        {
            if (EditorUtility.DisplayDialog("Run Tests", "This will start the automated pathfinding tests in Play Mode. Start now?", "Yes", "No"))
            {
                if (GameObject.Find("AutoTestRunTrigger") == null) new GameObject("AutoTestRunTrigger");
                EditorApplication.EnterPlaymode();
            }
        }
    }

    private void SafeSetMaterial(GameObject go, Material mat)
    {
        if (go == null) return;
        var ren = go.GetComponent<Renderer>();
        if (ren == null) return;
        if (mat != null) ren.sharedMaterial = mat;
        else
        {
            Shader fallback = Shader.Find("Sprites/Default");
            if (fallback != null) ren.sharedMaterial = new Material(fallback);
        }
    }

    private void Generate()
    {
        try
        {
            UnityEngine.Random.InitState(seed);

            // Remove existing
            var existing = GameObject.Find("AutoTestMap");
            if (existing != null) DestroyImmediate(existing);

            GameObject root = new GameObject("AutoTestMap");

            // Ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.parent = root.transform;
            ground.transform.position = new Vector3(-1.2516f, 1.38655f, 0.42583f);
            ground.transform.localScale = new Vector3(48.82838f, 1f, 78.42213f);

            // Material fallback (prefer URP Lit -> Standard -> Sprites/Default)
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader std = Shader.Find("Standard");
            Shader fallbackShader = Shader.Find("Sprites/Default");
            Material defaultMat = null;
            try { if (urpLit != null) defaultMat = new Material(urpLit); else if (std != null) defaultMat = new Material(std); else if (fallbackShader != null) defaultMat = new Material(fallbackShader); }
            catch { defaultMat = null; }
            SafeSetMaterial(ground, defaultMat);
            ground.isStatic = true;

            // Try NavMeshSurface
            Type navMeshSurfaceType = Type.GetType("UnityEngine.AI.NavMeshSurface, Unity.AI.Navigation");
            Component surfaceComp = null;
            if (navMeshSurfaceType != null)
            {
                surfaceComp = root.AddComponent(navMeshSurfaceType);
                try
                {
                    var collProp = navMeshSurfaceType.GetProperty("collectObjects");
                    if (collProp != null) collProp.SetValue(surfaceComp, Enum.Parse(collProp.PropertyType, "Children"));
                }
                catch { }
            }

            float areaRadius = 20f;
            int statCount = Mathf.RoundToInt(staticObstacles * obstacleDensity);

            // Static obstacles
            for (int i = 0; i < statCount; i++)
            {
                Vector3 pos = ground.transform.position + new Vector3(UnityEngine.Random.Range(-areaRadius, areaRadius), 0.5f, UnityEngine.Random.Range(-areaRadius, areaRadius));
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "StaticObstacle_" + i;
                box.transform.parent = root.transform;
                box.transform.position = pos;
                box.transform.localScale = new Vector3(UnityEngine.Random.Range(1f, 4f), UnityEngine.Random.Range(1f, 4f), UnityEngine.Random.Range(1f, 4f));
                SafeSetMaterial(box, defaultMat);
                box.isStatic = true;
            }

            // Corridors
            for (int c = 0; c < corridorCount; c++)
            {
                float length = 20f;
                Vector3 center = ground.transform.position + new Vector3(UnityEngine.Random.Range(-areaRadius / 2, areaRadius / 2), 0.5f, UnityEngine.Random.Range(-areaRadius / 2, areaRadius / 2));
                GameObject wallA = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallA.name = "Corridor_" + c + "_A";
                wallA.transform.parent = root.transform;
                wallA.transform.position = center + new Vector3(0, 0, corridorWidth);
                wallA.transform.localScale = new Vector3(length, 2f, 0.5f);
                SafeSetMaterial(wallA, defaultMat);
                wallA.isStatic = true;

                GameObject wallB = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallB.name = "Corridor_" + c + "_B";
                wallB.transform.parent = root.transform;
                wallB.transform.position = center + new Vector3(0, 0, -corridorWidth);
                wallB.transform.localScale = new Vector3(length, 2f, 0.5f);
                SafeSetMaterial(wallB, defaultMat);
                wallB.isStatic = true;
            }

            // Elevated L in corner
            Vector3 corner = ground.transform.position + new Vector3(-ground.transform.localScale.x * 0.45f, 0, -ground.transform.localScale.z * 0.45f);
            float elevatedHeight = 1.5f;
            GameObject elevatedRoot = new GameObject("ElevatedL"); elevatedRoot.transform.parent = root.transform; elevatedRoot.transform.position = corner + Vector3.up * elevatedHeight;

            GameObject leg1 = GameObject.CreatePrimitive(PrimitiveType.Cube); leg1.name = "Leg1"; leg1.transform.parent = elevatedRoot.transform; leg1.transform.localScale = new Vector3(10f, 1f, 4f); leg1.transform.localPosition = new Vector3(5f, 0, 0); SafeSetMaterial(leg1, defaultMat); leg1.isStatic = true;
            GameObject leg2 = GameObject.CreatePrimitive(PrimitiveType.Cube); leg2.name = "Leg2"; leg2.transform.parent = elevatedRoot.transform; leg2.transform.localScale = new Vector3(4f, 1f, 10f); leg2.transform.localPosition = new Vector3(0, 0, 5f); SafeSetMaterial(leg2, defaultMat); leg2.isStatic = true;

            float groundTop = ground.transform.position.y + ground.transform.localScale.y * 0.5f;

            // Ramps: compute from ground to leg tops, oriented along primary axis and facing ground center
            CreateRamp(root, "RampToLeg1", groundTop, leg1.transform.position, leg1.transform.localScale, defaultMat, ground.transform.position);
            CreateRamp(root, "RampToLeg2", groundTop, leg2.transform.position, leg2.transform.localScale, defaultMat, ground.transform.position);

            // Moving platforms: make them flush with ground top and create a hidden pit NavMeshObstacle toggled by platform
            for (int i = 0; i < movingPlatforms; i++)
            {
                float platSize = 4f;
                float platHeight = 0.5f;
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.name = "MovingPlatform_" + i;
                platform.transform.parent = root.transform;
                platform.transform.localScale = new Vector3(platSize, platHeight, platSize);
                Vector3 center = ground.transform.position + new Vector3(UnityEngine.Random.Range(-areaRadius, areaRadius), 0f, UnityEngine.Random.Range(-areaRadius, areaRadius));
                // place platform so its top is flush with ground top
                float topY = groundTop;
                platform.transform.position = new Vector3(center.x, topY - (platHeight * 0.5f), center.z);
                SafeSetMaterial(platform, defaultMat);

                // Create a hidden pit obstacle (no renderer) centered slightly below ground so pit appears as hole
                GameObject pit = new GameObject(platform.name + "_Pit");
                pit.transform.parent = root.transform;
                pit.transform.position = new Vector3(center.x, topY -0.08f, center.z);
                var pitOb = pit.AddComponent<NavMeshObstacle>();
                pitOb.shape = NavMeshObstacleShape.Box;
                pitOb.carving = false; // start disabled; platform will toggle at runtime
                pitOb.center = Vector3.zero;
                pitOb.size = new Vector3(10f,4f,10f);
                // hide visuals: don't add renderer

                // add GeneratedMovingPlatform and assign pit obstacle so it can toggle carving
                var gmp = platform.AddComponent<GeneratedMovingPlatform>();
                gmp.speed = UnityEngine.Random.Range(0.5f, 2f);
                gmp.lingerSeconds = platformLinger;
                gmp.pitObstacle = pitOb;

                // If NavMeshSurface is available, add one to the platform so it has a local NavMesh when stationary
                if (navMeshSurfaceType != null)
                {
                    try
                    {
                        var platSurf = platform.AddComponent(navMeshSurfaceType);
                        var collProp = navMeshSurfaceType.GetProperty("collectObjects");
                        if (collProp != null) collProp.SetValue(platSurf, Enum.Parse(collProp.PropertyType, "Children"));
                        var buildMethod = navMeshSurfaceType.GetMethod("BuildNavMesh");
                        if (buildMethod != null) buildMethod.Invoke(platSurf, null);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("Could not add/build NavMeshSurface on platform: " + ex.Message);
                    }
                }

                // waypoints
                var wp0 = new GameObject(platform.name + "_wp0"); wp0.transform.parent = root.transform; wp0.transform.position = platform.transform.position + new Vector3(-5f, 0, 0);
                var wp1 = new GameObject(platform.name + "_wp1"); wp1.transform.parent = root.transform; wp1.transform.position = platform.transform.position + new Vector3(5f, 0, 0);
                gmp.pathPoints = new Transform[] { wp0.transform, wp1.transform };

                // Ensure platform's NavMeshObstacle exists but does not carve (so agents can stand on it)
                var platOb = platform.GetComponent<NavMeshObstacle>();
                if (platOb == null) platOb = platform.AddComponent<NavMeshObstacle>();
                platOb.carving = false;
                // ensure obstacle size matches platform so NavMesh recognizes the platform if required
                platOb.shape = NavMeshObstacleShape.Box;
                platOb.size = new Vector3(platSize, platHeight, platSize);
                platOb.center = Vector3.zero;
            }

            // Moving obstacles
            for (int i = 0; i < movingObstacles; i++)
            {
                GameObject mob = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mob.name = "MovingObstacle_" + i;
                mob.transform.parent = root.transform;
                mob.transform.localScale = new Vector3(3f, 3f, 1f);
                Vector3 center = ground.transform.position + new Vector3(UnityEngine.Random.Range(-areaRadius, areaRadius), 1f, UnityEngine.Random.Range(-areaRadius, areaRadius));
                mob.transform.position = center;
                SafeSetMaterial(mob, defaultMat);
                var mobOb = mob.AddComponent<NavMeshObstacle>();
                mobOb.carving = true;
                // ensure carveOnlyStationary = false if property exists
                var prop = mobOb.GetType().GetProperty("carveOnlyStationary");
                if (prop != null && prop.CanWrite) prop.SetValue(mobOb, false);

                var mo = mob.AddComponent(typeof(MovingObstacle));
                var a = new GameObject(mob.name + "_A"); a.transform.parent = mob.transform; a.transform.position = center + new Vector3(-4f, 0, 0);
                var b = new GameObject(mob.name + "_B"); b.transform.parent = mob.transform; b.transform.position = center + new Vector3(4f, 0, 0);
                try
                {
                    var moType = mo.GetType();
                    var pA = moType.GetField("pointA");
                    var pB = moType.GetField("pointB");
                    var speedField = moType.GetField("speed");
                    if (pA != null) pA.SetValue(mo, a.transform);
                    if (pB != null) pB.SetValue(mo, b.transform);
                    if (speedField != null) speedField.SetValue(mo, UnityEngine.Random.Range(0.5f, 2f));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Could not configure MovingObstacle fields: " + ex.Message);
                }
            }

            // Agents
            for (int i = 0; i < agents; i++)
            {
                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cap.name = "TestAgent_" + i;
                cap.transform.parent = root.transform;
                cap.transform.position = ground.transform.position + new Vector3(UnityEngine.Random.Range(-areaRadius, areaRadius), 2f, UnityEngine.Random.Range(-areaRadius, areaRadius));
                var agent = cap.AddComponent<NavMeshAgent>();
                agent.areaMask = -1;
                // add simple wanderer so agents move for testing
                cap.AddComponent<AutoAgentWander>();
            }

            // Bake NavMesh
            if (surfaceComp != null)
            {
                try
                {
                    var buildMethod = surfaceComp.GetType().GetMethod("BuildNavMesh");
                    if (buildMethod != null) buildMethod.Invoke(surfaceComp, null);
                    else UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                }
                catch (Exception ex) { Debug.LogWarning("Failed to call NavMeshSurface.BuildNavMesh via reflection: " + ex.Message); UnityEditor.AI.NavMeshBuilder.BuildNavMesh(); }
            }
            else UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("AutoTestMap generated under root GameObject 'AutoTestMap'. NavMesh baked.");
        }
        catch (Exception ex)
        {
            Debug.LogError("AutoTestMapGenerator.Generate failed: " + ex + "\n" + ex.StackTrace);
        }
    }

    private void CreateRamp(GameObject root, string name, float groundTop, Vector3 legWorldPos, Vector3 legScale, Material mat, Vector3 groundCenter)
    {
        // Top point on elevated leg
        float legTop = legWorldPos.y + (legScale.y *0.5f);
        Vector3 top = new Vector3(legWorldPos.x, legTop, legWorldPos.z);

        // Create an axis-aligned ramp. Choose the primary axis of the elevated leg (X or Z)
        // and place the ramp to approach the leg along that axis from the ground center side.
        float legExtentX = legScale.x;
        float legExtentZ = legScale.z;
        bool useX = legExtentX >= legExtentZ;

        float offsetFromLeg = (Mathf.Max(legExtentX, legExtentZ) *0.5f) +3f;
        Vector3 start;
        if (useX)
        {
            // approach along X axis; choose sign based on ground center relative to leg
            float dirSign = (groundCenter.x >= legWorldPos.x) ?1f : -1f;
            start = new Vector3(legWorldPos.x + dirSign * offsetFromLeg, groundTop, legWorldPos.z);
        }
        else
        {
            // approach along Z axis
            float dirSign = (groundCenter.z >= legWorldPos.z) ?1f : -1f;
            start = new Vector3(legWorldPos.x, groundTop, legWorldPos.z + dirSign * offsetFromLeg);
        }

        Vector3 end = top;
        // Adjust end so ramp peak is at the outer edge of the elevated leg (not the leg center)
        if (useX)
        {
            // end.x should be at the leg edge facing the start side
            float dirSignEdge = (groundCenter.x >= legWorldPos.x) ?1f : -1f;
            end = new Vector3(legWorldPos.x + dirSignEdge * (legExtentX *0.5f), legTop, legWorldPos.z);
        }
        else
        {
            float dirSignEdge = (groundCenter.z >= legWorldPos.z) ?1f : -1f;
            end = new Vector3(legWorldPos.x, legTop, legWorldPos.z + dirSignEdge * (legExtentZ *0.5f));
        }

        Vector3 dirVec = end - start;
        float length = Mathf.Max(0.1f, dirVec.magnitude);

        Vector3 mid = (start + end) *0.5f;
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = name;
        ramp.transform.parent = root.transform;
        ramp.transform.position = mid;
        float rampWidth =4f;
        float rampThickness =0.5f;
        // scale Z to match run length; cube's forward (+Z) will point along dirVec
        ramp.transform.localScale = new Vector3(rampWidth, rampThickness, length);
        // rotate so +Z faces from start -> end
        if (dirVec.sqrMagnitude >0.0001f)
        ramp.transform.rotation = Quaternion.LookRotation(dirVec.normalized, Vector3.up);
        SafeSetMaterial(ramp, mat);
        ramp.isStatic = true;
    }
}