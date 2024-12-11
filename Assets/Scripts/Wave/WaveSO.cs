using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/WaveSO")]
public class WaveSO : ScriptableObject
{
    public List<SpawnSO> spawns;
}
