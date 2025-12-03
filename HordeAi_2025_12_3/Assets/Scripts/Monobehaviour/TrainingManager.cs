using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TrainingManager : MonoBehaviour
{
    [Header("Environment Settings")]
    public int EnvironmentCount = 1;
    public float EnvironmentSpacing = 60f;
    public GameObject EnvironmentGeometryPrefab;

    [Header("Agent Settings")]
    public GameObject HordeControllerPrefab;
    public Unity.MLAgents.Policies.BehaviorParameters BrainTemplate;

    [Header("ECS Entity Prefabs")]
    public GameObject UnitPrefab;
    public GameObject TargetPrefab;

    [Header("Squad Settings")]
    public int MembersPerSquad = 1;
    public float SpawnRadius = 5f;

    [Header("Movement Settings")]
    public float MoveSpeed = 8f;

    [Header("Horizontal Vision")]
    public int RayCount = 5;
    public float FovDegrees = 120f;

    [Header("Vertical Vision")]
    public int VerticalRayCount = 3;
    public float VerticalFovDegrees = 45f;

    [Header("General Vision")]
    public float RayLength = 20f;
    public LayerMask VisionMask;

    [Header("RL Settings")]
    public int AgentMaxStep = 5000;

    [ContextMenu("1. Generate World")]
    public void GenerateWorld()
    {
        DestroyImmediateIfFound("ML_Agents_Container");
        DestroyImmediateIfFound("ECS_Content_DragMe");

        Transform mlContainer = new GameObject("ML_Agents_Container").transform;
        Transform ecsContainer = new GameObject("ECS_Content_DragMe").transform;

        mlContainer.parent = transform;
        ecsContainer.position = Vector3.zero;


        if (BrainTemplate != null)
        {
            int safeH = Mathf.Max(1, RayCount);
            int safeV = Mathf.Max(1, VerticalRayCount);
            int totalRays = safeH * safeV;


            int perMemberSize = 3 + 3 + totalRays;

            int totalObsSize = perMemberSize * MembersPerSquad;

            if (BrainTemplate.BrainParameters.VectorObservationSize != totalObsSize)
            {
                BrainTemplate.BrainParameters.VectorObservationSize = totalObsSize;
            }

            int totalActions = MembersPerSquad * 2;
            BrainTemplate.BrainParameters.ActionSpec = new Unity.MLAgents.Actuators.ActionSpec
            {
                NumContinuousActions = totalActions,
                BranchSizes = new int[0]
            };

#if UNITY_EDITOR
            EditorUtility.SetDirty(BrainTemplate);
#endif
            Debug.Log($"TrainingManager: Brain Updated. Total Rays: {totalRays}. Total Obs: {totalObsSize}.");
        }


        GameObject globalSettingsGO = new GameObject("Global_Horde_Settings");
        globalSettingsGO.transform.parent = ecsContainer;

        var settingsAuth = globalSettingsGO.AddComponent<GlobalSettingsAuthoring>();
        settingsAuth.MemberPrefab = UnitPrefab;
        settingsAuth.TargetPrefab = TargetPrefab;
        settingsAuth.MembersPerSquad = MembersPerSquad;
        settingsAuth.SpawnRadius = SpawnRadius;
        settingsAuth.MoveSpeed = MoveSpeed;

        settingsAuth.RayCount = RayCount;
        settingsAuth.AngleDegrees = FovDegrees;


        settingsAuth.VerticalRayCount = VerticalRayCount;
        settingsAuth.VerticalAngleDegrees = VerticalFovDegrees;

        settingsAuth.RayLength = RayLength;
        settingsAuth.VisionMask = VisionMask;


        int x = 0, z = 0;
        int dx = 0, dz = -1;

        for (int i = 0; i < EnvironmentCount; i++)
        {
            Vector3 center = new Vector3(x * EnvironmentSpacing, 0, z * EnvironmentSpacing);
            SpawnEnvironment(i, center, mlContainer, ecsContainer);

            if (x == z || (x < 0 && x == -z) || (x > 0 && x == 1 - z))
            {
                int temp = dx;
                dx = -dz;
                dz = temp;
            }
            x += dx;
            z += dz;
        }

        Debug.Log("<color=green>GENERATION COMPLETE:</color> Drag 'ECS_Content_DragMe' to SubScene.");
    }

    private void SpawnEnvironment(int envID, Vector3 center, Transform mlRoot, Transform ecsRoot)
    {
        if (EnvironmentGeometryPrefab != null)
        {
            GameObject geom = Instantiate(EnvironmentGeometryPrefab, center, Quaternion.identity, ecsRoot);
            geom.name = $"Env_Geom_{envID}";
        }

        if (HordeControllerPrefab != null)
        {
            GameObject controllerGO = Instantiate(HordeControllerPrefab, center, Quaternion.identity, mlRoot);
            controllerGO.name = $"HordeController_{envID}";

            var agent = controllerGO.GetComponent<HordeAgent>();
            if (agent) agent.AgentMaxStep = AgentMaxStep;

            var bridge = controllerGO.GetComponent<HordeBridge>();
            if (bridge)
            {
                bridge.EnvironmentID = envID;
                bridge.EnvironmentCenter = center;
            }
        }

        GameObject marker = new GameObject($"Env_Marker_{envID}");
        marker.transform.position = center;
        marker.transform.parent = ecsRoot;
        var auth = marker.AddComponent<EnvironmentAuthoring>();
        auth.EnvID = envID;
    }

    private void DestroyImmediateIfFound(string name)
    {
        Transform t = transform.Find(name);
        if (t != null) DestroyImmediate(t.gameObject);
        GameObject go = GameObject.Find(name);
        if (go != null) DestroyImmediate(go);
    }
}