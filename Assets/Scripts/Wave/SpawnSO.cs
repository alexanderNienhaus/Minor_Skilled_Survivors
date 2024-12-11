using UnityEngine;

public enum UnitType
{
    Boid,
    Drone,
    Tank
}

[CreateAssetMenu(menuName = "ScriptableObjects/SpawnSO")]
public class SpawnSO : ScriptableObject
{
    public GameObject prefab;
    public UnitType unitType;
    public float unitSize;
    public int amountToSpawn;
    public Vector3 spawnPosition;
    public float spawnRadiusMax;
    public float spawnRadiusMin;
    public bool isSphericalSpawn;
    public float whenToSpawn;
    public float spawningDuration;
}

