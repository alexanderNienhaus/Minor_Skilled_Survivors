using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class RegisterMapLayoutAuthoring : MonoBehaviour
{
    [SerializeField] private int type;
    [SerializeField] private float halfBoundsX;
    [SerializeField] private float halfBoundsZ;

    private class Baker : Baker<RegisterMapLayoutAuthoring>
    {
        public override void Bake(RegisterMapLayoutAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RegisterMapLayout
            {
                type = pAuthoring.type,
                rotationAngle = pAuthoring.transform.rotation.eulerAngles.y,
                halfBoundsX = pAuthoring.halfBoundsX,
                halfBoundsZ = pAuthoring.halfBoundsZ
            });
        }
    }
}

[BurstCompile]
public struct RegisterMapLayout : IComponentData
{
    public int type;
    public float rotationAngle;
    public float halfBoundsX;
    public float halfBoundsZ;
}