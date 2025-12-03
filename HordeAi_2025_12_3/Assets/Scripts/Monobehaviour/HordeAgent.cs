using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class HordeAgent : Agent
{
    public HordeBridge Bridge;
    public int AgentMaxStep = 5000;

    private int _expectedObservationSize;

    private float _prevDistance;

    public override void Initialize()
    {
        if (Bridge == null) Bridge = GetComponent<HordeBridge>();
        this.MaxStep = AgentMaxStep;

        var bp = GetComponent<BehaviorParameters>();
        if (bp != null)
        {
            _expectedObservationSize = bp.BrainParameters.VectorObservationSize;
        }
    }

    public override void OnEpisodeBegin()
    {
        Bridge.ResetEpisode();

        if (Bridge.IsReady && Bridge.MemberCount > 0)
        {
            Bridge.GetMemberState(0, out Vector3 pos, out Quaternion _, out Vector3 _, out Vector3 _);
            _prevDistance = Vector3.Distance(pos, Bridge.TargetPosition);
        }
        else
        {
            _prevDistance = 0f;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        int valuesAdded = 0;

        if (Bridge.IsReady)
        {
            int memberCount = Bridge.MemberCount;
            float[] allRays = Bridge.GetAllSensorReadings();
            int raysPerUnit = (allRays != null && memberCount > 0) ? allRays.Length / memberCount : 0;

            for (int i = 0; i < memberCount; i++)
            {
                Bridge.GetMemberState(i, out Vector3 pos, out Quaternion rot, out Vector3 linVel, out Vector3 angVel);

                // 1. Relative Position (3)
                sensor.AddObservation(Bridge.TargetPosition - pos);
                valuesAdded += 3;

                // 2. Linear Velocity (3)
                sensor.AddObservation(linVel);
                valuesAdded += 3;

                // 3. Rays (X)
                if (allRays != null)
                {
                    for (int r = 0; r < raysPerUnit; r++)
                    {
                        sensor.AddObservation(allRays[(i * raysPerUnit) + r]);
                        valuesAdded += 1;
                    }
                }
            }
        }

        int remaining = _expectedObservationSize - valuesAdded;
        if (remaining > 0)
        {
            for (int i = 0; i < remaining; i++) sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // If not ready, do nothing (wait for entities to spawn)
        if (!Bridge.IsReady) return;

        float totalReward = 0;
        int finishedCount = 0;
        int fellCount = 0;

        int count = Bridge.MemberCount;
        var contActions = actions.ContinuousActions;
        int actionsPerAgent = 2;

        for (int i = 0; i < count; i++) 
        {
            int idx = i * actionsPerAgent;
            if (idx + 1 >= contActions.Length) break;

            float moveX = contActions[idx + 0];
            float moveZ = contActions[idx + 1];

            Bridge.SetCommand(i, new Vector3(moveX, 0, moveZ));

            Bridge.GetMemberState(i, out Vector3 pos, out Quaternion _, out Vector3 _, out Vector3 _);
            float currentDist = Vector3.Distance(pos, Bridge.TargetPosition);


            if (_prevDistance == 0f) _prevDistance = currentDist;


            float distDelta = _prevDistance - currentDist;
            totalReward += distDelta * 1.0f;
            _prevDistance = currentDist;

            if (currentDist < 1.5f)
            {
                finishedCount++;
                totalReward += 1.0f;
            }
            if (pos.y < -2.0f)
            {
                fellCount++;
                totalReward -= 1.0f;
            }
        }

        AddReward(totalReward);
        AddReward(-0.0005f);

        if (finishedCount > 0)
        {
            EndEpisode();
        }
        else if (fellCount > 0)
        {
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var outActions = actionsOut.ContinuousActions;
        outActions[0] = Input.GetAxis("Horizontal");
        outActions[1] = Input.GetAxis("Vertical");
    }
}