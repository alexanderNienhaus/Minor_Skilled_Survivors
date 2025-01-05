using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/BoidSettingsSO")]
public class BoidSettingsSO : ScriptableObject
{
    [Header("Combat")]
    public int dmg = 100;

    [Header("Movement")]
    public float minSpeed = 5;
    public float maxSpeed = 8;
    public float perceptionRadius = 2.5f;
    public float avoidanceRadius = 1;
    public float maxSteerForce = 8;

    [Header("Weights")]
    public float alignWeight = 2;
    public float cohesionWeight = 1;
    public float seperateWeight = 2.5f;
    public float targetWeight = 2;

    [Header("Collisions")]
    public LayerMask obstacleMask;
    public float boundsRadius = .27f;
    public float avoidCollisionWeight = 20;
    public float collisionAvoidDst = 5;
}
