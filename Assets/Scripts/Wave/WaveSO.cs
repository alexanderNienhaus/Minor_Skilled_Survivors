using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/WaveSO")]
public class WaveSO : ScriptableObject
{
    public GameObject prefab;
    public int amount;
    public Vector3 position;
    public float radius;
    public float time;
}

