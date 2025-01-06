using UnityEngine;

public enum AttackableUnitType
{
    Boid,
    Drone,
    Tank,
    AATurret,
    RadioStation,
    Base
}

[CreateAssetMenu(menuName = "ScriptableObjects/SpawnSO")]
public class SpawnSO : ScriptableObject
{
    [Header("Unit")]
    public GameObject prefab;
    public AttackableUnitType unitType;
    public float unitSize;

    [Header("Spawn")]
    public int amountToSpawn;
    public Vector3 spawnPosition;
    public float spawnRadiusMax;
    public float spawnRadiusMin;
    public bool isSphericalSpawn;

    [Header("Timing")]
    public float whenToSpawn;
}

