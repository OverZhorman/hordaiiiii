using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Physics;

public class HordeBridge : MonoBehaviour
{
    public int EnvironmentID;
    public Vector3 EnvironmentCenter;

    private EntityManager _em;
    private NativeList<Entity> _mySquadEntities;
    private Entity _myTargetEntity;


    private EntityQuery _physicsQuery;

    public bool IsReady => _mySquadEntities.IsCreated && _mySquadEntities.Length > 0 && _em.Exists(_myTargetEntity);

    public int MemberCount => _mySquadEntities.IsCreated ? _mySquadEntities.Length : 0;
    public Vector3 TargetPosition { get; private set; }

    private float[] _sensorCache;

    void Start()
    {
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        _mySquadEntities = new NativeList<Entity>(Allocator.Persistent);


        _physicsQuery = _em.CreateEntityQuery(typeof(PhysicsWorldSingleton));
    }

    private void OnDestroy()
    {
        if (_mySquadEntities.IsCreated) _mySquadEntities.Dispose();
    }

    void Update()
    {
        if (!_mySquadEntities.IsCreated || _mySquadEntities.Length == 0)
        {
            FindMyEntities();
            return;
        }

        if (_em.Exists(_myTargetEntity))
        {
            TargetPosition = _em.GetComponentData<LocalTransform>(_myTargetEntity).Position;
        }
    }


    public void ResetEpisode()
    {
        if (!IsReady) return;

        CollisionWorld? collisionWorld = null;
        if (_physicsQuery.HasSingleton<PhysicsWorldSingleton>())
        {
            collisionWorld = _physicsQuery.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        }


        if (_em.Exists(_myTargetEntity))
        {
            float3 tOffset = new float3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10));
            float3 targetXZ = (float3)EnvironmentCenter + tOffset;

            // Snap to ground
            float targetY = GetGroundHeight(collisionWorld, targetXZ, 0.5f);

            _em.SetComponentData(_myTargetEntity, LocalTransform.FromPosition(new float3(targetXZ.x, targetY, targetXZ.z)));
        }

        // 3. Reset Squad with Raycast
        float angleStep = math.PI2 / _mySquadEntities.Length;
        for (int i = 0; i < _mySquadEntities.Length; i++)
        {
            Entity e = _mySquadEntities[i];
            if (!_em.Exists(e)) continue;

            float currentAngle = i * angleStep;
            float3 offset = new float3(math.cos(currentAngle), 0, math.sin(currentAngle)) * 2.0f;
            float3 memberXZ = (float3)EnvironmentCenter + offset;

            // Snap to ground
            float memberY = GetGroundHeight(collisionWorld, memberXZ, 0.5f);

            var t = _em.GetComponentData<LocalTransform>(e);
            t.Position = new float3(memberXZ.x, memberY, memberXZ.z);
            t.Rotation = quaternion.identity;
            _em.SetComponentData(e, t);

            if (_em.HasComponent<PhysicsVelocity>(e))
            {
                var vel = _em.GetComponentData<PhysicsVelocity>(e);
                vel.Linear = float3.zero;
                vel.Angular = float3.zero;
                _em.SetComponentData(e, vel);
            }
        }
    }


    private float GetGroundHeight(CollisionWorld? collisionWorld, float3 xzPos, float verticalOffset)
    {

        if (collisionWorld == null) return verticalOffset;

        float3 start = new float3(xzPos.x, 50f, xzPos.z);
        float3 end = new float3(xzPos.x, -50f, xzPos.z);

        RaycastInput input = new RaycastInput
        {
            Start = start,
            End = end,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u,
                GroupIndex = 0
            }
        };

        if (collisionWorld.Value.CastRay(input, out Unity.Physics.RaycastHit hit))
        {
            return hit.Position.y + verticalOffset;
        }

        return verticalOffset;
    }

    public void GetMemberState(int index, out Vector3 pos, out Quaternion rot, out Vector3 linVel, out Vector3 angVel)
    {
        pos = Vector3.zero; rot = Quaternion.identity; linVel = Vector3.zero; angVel = Vector3.zero;
        if (!IsReady || index < 0 || index >= _mySquadEntities.Length) return;
        Entity e = _mySquadEntities[index];
        if (!_em.Exists(e)) return;

        var transform = _em.GetComponentData<LocalTransform>(e);
        pos = transform.Position;
        rot = transform.Rotation;

        if (_em.HasComponent<PhysicsVelocity>(e))
        {
            var velData = _em.GetComponentData<PhysicsVelocity>(e);
            linVel = velData.Linear;
            angVel = velData.Angular;
        }
    }

    public float[] GetAllSensorReadings()
    {
        if (!IsReady) return null;
        int members = _mySquadEntities.Length;
        int raysPerUnit = 0;
        if (_em.Exists(_mySquadEntities[0]) && _em.HasBuffer<RaycastResult>(_mySquadEntities[0]))
            raysPerUnit = _em.GetBuffer<RaycastResult>(_mySquadEntities[0]).Length;

        int totalSize = members * raysPerUnit;
        if (_sensorCache == null || _sensorCache.Length != totalSize) _sensorCache = new float[totalSize];

        int offset = 0;
        for (int i = 0; i < members; i++)
        {
            Entity e = _mySquadEntities[i];
            if (!_em.Exists(e)) continue;

            if (_em.HasBuffer<RaycastResult>(e))
            {
                var buffer = _em.GetBuffer<RaycastResult>(e);
                for (int r = 0; r < buffer.Length; r++)
                {
                    if (offset + r < _sensorCache.Length)
                        _sensorCache[offset + r] = buffer[r].DistanceNormalized;
                }
            }
            offset += raysPerUnit;
        }
        return _sensorCache;
    }

    public void SetCommand(int memberIndex, Vector3 torqueWorld)
    {
        if (!IsReady || memberIndex >= _mySquadEntities.Length) return;
        Entity e = _mySquadEntities[memberIndex];
        if (!_em.Exists(e)) return;
        var cmd = _em.GetComponentData<AgentCommand>(e);
        cmd.MoveVector = new float3(torqueWorld.x, torqueWorld.y, torqueWorld.z);
        _em.SetComponentData(e, cmd);
    }

    private void FindMyEntities()
    {
        _mySquadEntities.Clear();
        var query = _em.CreateEntityQuery(typeof(SquadMember), typeof(LocalTransform));
        var entities = query.ToEntityArray(Allocator.Temp);
        var members = query.ToComponentDataArray<SquadMember>(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            if (members[i].EnvID == EnvironmentID) _mySquadEntities.Add(entities[i]);
        }
        entities.Dispose();
        members.Dispose();

        var tQuery = _em.CreateEntityQuery(typeof(TargetTag), typeof(LocalTransform));
        var tEntities = tQuery.ToEntityArray(Allocator.Temp);
        var tags = tQuery.ToComponentDataArray<TargetTag>(Allocator.Temp);

        for (int i = 0; i < tEntities.Length; i++)
        {
            if (tags[i].EnvID == EnvironmentID)
            {
                _myTargetEntity = tEntities[i];
                break;
            }
        }
        tEntities.Dispose();
        tags.Dispose();
    }
}