using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/AttackableSO")]
public class AttackableSO : ScriptableObject
{
    [Header("Attackable")]
    public AttackableUnitType attackableUnitType;
    public float startHp;
    public int ressourceCost;
    public float boundsRadius;
}