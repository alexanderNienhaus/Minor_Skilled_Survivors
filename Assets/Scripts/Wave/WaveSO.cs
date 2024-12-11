using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/WaveSO")]
public class WaveSO : ScriptableObject
{
    [Header("Spawns")]
    public List<SpawnSO> spawns;
}
