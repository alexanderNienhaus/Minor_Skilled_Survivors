using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/AttackingSO")]
public class AttackingSO : ScriptableObject
{
    [Header("Attacking")]
    public float dmg;
    public float range;
    public float attackSpeed;
    public List<AttackableUnitType> possibleAttackTargets;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed;
    public Vector3 projectileSpawnOffset;
    public float projectileSize;
}
